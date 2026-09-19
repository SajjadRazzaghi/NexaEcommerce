using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class WarehousePickingOrchestrator(
    IOrderRepository orderRepository,
    IFulfillmentRepository fulfillmentRepository,
    IWarehouseStockRepository warehouseStockRepository,
    IWarehouseStockReservationRepository warehouseReservationRepository,
    IInventoryUnitOfWork inventoryUnitOfWork,
    IOrderUnitOfWork orderUnitOfWork)
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<FulfillmentPickingResultDto>
        StartAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        ValidateOrderId(orderId);

        var normalizedTenantId =
            tenantId.Trim();

        var order =
            await orderRepository.GetByIdAsync(
                normalizedTenantId,
                orderId,
                null,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        if (order.Status !=
            OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "Picking can only start for a processing order.");
        }

        var fulfillment =
            await fulfillmentRepository.GetByOrderAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        if (fulfillment is null)
        {
            throw new KeyNotFoundException(
                "Fulfillment was not found for this order.");
        }

        if (fulfillment.Status ==
            FulfillmentStatus.Picked)
        {
            var completedReservations =
                await warehouseReservationRepository.GetByOrderAsync(
                    normalizedTenantId,
                    orderId,
                    cancellationToken);

            return Map(
                order,
                fulfillment,
                completedReservations,
                alreadyProcessed: true);
        }

        if (fulfillment.Status ==
            FulfillmentStatus.Picking)
        {
            var activeReservations =
                await warehouseReservationRepository.GetActiveByOrderAsync(
                    normalizedTenantId,
                    orderId,
                    cancellationToken);

            EnsureReservationsMatchOrder(
                order,
                fulfillment,
                activeReservations);

            return Map(
                order,
                fulfillment,
                activeReservations,
                alreadyProcessed: true);
        }

        if (fulfillment.Status !=
            FulfillmentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Fulfillment cannot start picking from '{fulfillment.Status}' status.");
        }

        if (!fulfillment.WarehouseId.HasValue)
        {
            throw new InvalidOperationException(
                "Warehouse must be allocated before picking.");
        }

        var active =
            await warehouseReservationRepository.GetActiveByOrderAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        if (active.Count == 0)
        {
            throw new InvalidOperationException(
                "Warehouse stock must be reserved before picking.");
        }

        EnsureReservationsMatchOrder(
            order,
            fulfillment,
            active);

        fulfillment.StartPicking();

        await orderUnitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(
            order,
            fulfillment,
            active,
            alreadyProcessed: false);
    }

    public async Task<FulfillmentPickingResultDto>
        CompleteAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        ValidateOrderId(orderId);

        var normalizedTenantId =
            tenantId.Trim();

        for (var attempt = 1;
             attempt <= MaxConcurrencyRetries;
             attempt++)
        {
            var order =
                await orderRepository.GetByIdAsync(
                    normalizedTenantId,
                    orderId,
                    null,
                    cancellationToken);

            if (order is null)
            {
                throw new KeyNotFoundException(
                    "Order was not found.");
            }

            if (order.Status !=
                OrderStatus.Processing)
            {
                throw new InvalidOperationException(
                    "Picking can only be completed for a processing order.");
            }

            var fulfillment =
                await fulfillmentRepository.GetByOrderAsync(
                    normalizedTenantId,
                    orderId,
                    cancellationToken);

            if (fulfillment is null)
            {
                throw new KeyNotFoundException(
                    "Fulfillment was not found for this order.");
            }

            var allReservations =
                await warehouseReservationRepository.GetByOrderAsync(
                    normalizedTenantId,
                    orderId,
                    cancellationToken);

            if (fulfillment.Status ==
                FulfillmentStatus.Picked)
            {
                EnsureReservationsMatchOrder(
                    order,
                    fulfillment,
                    allReservations);

                return Map(
                    order,
                    fulfillment,
                    allReservations,
                    alreadyProcessed: true);
            }

            if (fulfillment.Status !=
                FulfillmentStatus.Picking)
            {
                throw new InvalidOperationException(
                    "Fulfillment must be in Picking status before it can be completed.");
            }

            EnsureReservationsMatchOrder(
                order,
                fulfillment,
                allReservations);

            var activeReservations =
                allReservations
                    .Where(
                        x =>
                            x.Status ==
                            WarehouseStockReservationStatus.Reserved)
                    .ToList();

            var consumedReservations =
                allReservations
                    .Where(
                        x =>
                            x.Status ==
                            WarehouseStockReservationStatus.Consumed)
                    .ToList();

            if (activeReservations.Count == 0)
            {
                if (allReservations.Count > 0 &&
                    consumedReservations.Count ==
                        allReservations.Count)
                {
                    fulfillment.MarkPicked();

                    await orderUnitOfWork.SaveChangesAsync(
                        cancellationToken);

                    return Map(
                        order,
                        fulfillment,
                        allReservations,
                        alreadyProcessed: true);
                }

                throw new InvalidOperationException(
                    "Warehouse reservations are not in a valid picking state.");
            }

            if (allReservations.Any(
                    x =>
                        x.Status ==
                        WarehouseStockReservationStatus.Released))
            {
                throw new InvalidOperationException(
                    "Released warehouse reservations cannot be picked.");
            }

            try
            {
                await inventoryUnitOfWork
                    .ExecuteInTransactionAsync(
                        async ct =>
                        {
                            var groups =
                                activeReservations
                                    .GroupBy(
                                        x =>
                                            new
                                            {
                                                x.WarehouseId,
                                                x.LocationId,
                                                x.ProductVariantId
                                            })
                                    .ToList();

                            foreach (var group in groups)
                            {
                                ct.ThrowIfCancellationRequested();

                                var stock =
                                    await warehouseStockRepository.GetAsync(
                                        normalizedTenantId,
                                        group.Key.WarehouseId,
                                        group.Key.LocationId,
                                        group.Key.ProductVariantId,
                                        ct);

                                if (stock is null)
                                {
                                    throw new InvalidOperationException(
                                        "Warehouse stock was not found while completing picking.");
                                }

                                var quantity =
                                    group.Sum(
                                        x => x.Quantity);

                                stock.ConsumeReserved(
                                    quantity);

                                var movement =
                                    WarehouseStockMovement.Create(
                                        normalizedTenantId,
                                        group.Key.WarehouseId,
                                        group.Key.LocationId,
                                        group.Key.ProductVariantId,
                                        WarehouseStockMovementType.Picking,
                                        -quantity,
                                        stock.OnHandQuantity,
                                        "WarehousePicking",
                                        orderId.ToString(),
                                        "Warehouse stock consumed during picking.");

                                await warehouseStockRepository.AddMovementAsync(
                                    movement,
                                    ct);
                            }

                            foreach (var reservation in activeReservations)
                            {
                                reservation.Consume();
                            }

                            return true;
                        },
                        cancellationToken);

                /*
                 * Inventory transaction has succeeded.
                 *
                 * The operation is intentionally idempotent: if the
                 * order-side update is interrupted, the next request
                 * sees consumed warehouse reservations and only finishes
                 * the fulfillment transition to Picked.
                 */
                fulfillment.MarkPicked();

                await orderUnitOfWork.SaveChangesAsync(
                    cancellationToken);

                var finalReservations =
                    await warehouseReservationRepository.GetByOrderAsync(
                        normalizedTenantId,
                        orderId,
                        cancellationToken);

                return Map(
                    order,
                    fulfillment,
                    finalReservations,
                    alreadyProcessed: false);
            }
            catch (DbUpdateConcurrencyException)
                when (attempt < MaxConcurrencyRetries)
            {
                await Task.Yield();
            }
        }

        throw new InvalidOperationException(
            "Warehouse stock changed concurrently. Please retry picking.");
    }

    private static void EnsureReservationsMatchOrder(
        Order order,
        Fulfillment fulfillment,
        IReadOnlyList<WarehouseStockReservation> reservations)
    {
        if (!fulfillment.WarehouseId.HasValue)
        {
            throw new InvalidOperationException(
                "Fulfillment has no allocated warehouse.");
        }

        var expected =
            order.InventoryReservations
                .Where(
                    x =>
                        x.Status ==
                        InventoryReservationStatus.Committed)
                .GroupBy(
                    x => x.Id)
                .ToDictionary(
                    x => x.Key,
                    x => new
                    {
                        ProductVariantId =
                            x.First().ProductVariantId,
                        Quantity =
                            x.Sum(
                                r => r.Quantity)
                    });

        if (expected.Count == 0)
        {
            throw new InvalidOperationException(
                "Order has no committed inventory reservations.");
        }

        var actual =
            reservations
                .GroupBy(
                    x =>
                        x.OrderInventoryReservationId)
                .ToDictionary(
                    x => x.Key,
                    x => new
                    {
                        ProductVariantId =
                            x.First().ProductVariantId,
                        Quantity =
                            x.Sum(
                                r => r.Quantity)
                    });

        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException(
                "Warehouse reservations do not match order reservations.");
        }

        foreach (var expectedPair in expected)
        {
            if (!actual.TryGetValue(
                    expectedPair.Key,
                    out var actualValue))
            {
                throw new InvalidOperationException(
                    "Warehouse reservations do not match order reservations.");
            }

            if (actualValue.ProductVariantId !=
                expectedPair.Value.ProductVariantId ||
                actualValue.Quantity !=
                expectedPair.Value.Quantity)
            {
                throw new InvalidOperationException(
                    "Warehouse reservation quantities do not match the order.");
            }
        }

        foreach (var reservation in reservations)
        {
            if (reservation.WarehouseId !=
                fulfillment.WarehouseId.Value)
            {
                throw new InvalidOperationException(
                    "Warehouse reservation belongs to a different warehouse.");
            }

            if (reservation.Status ==
                WarehouseStockReservationStatus.Released)
            {
                throw new InvalidOperationException(
                    "Released warehouse reservation cannot participate in picking.");
            }
        }
    }

    private static FulfillmentPickingResultDto Map(
        Order order,
        Fulfillment fulfillment,
        IReadOnlyList<WarehouseStockReservation> reservations,
        bool alreadyProcessed)
    {
        return new FulfillmentPickingResultDto(
            order.Id,
            order.OrderNumber,
            fulfillment.Id,
            fulfillment.Status.ToString(),
            alreadyProcessed,
            reservations
                .OrderBy(
                    x => x.ProductVariantId)
                .ThenBy(
                    x => x.LocationId)
                .ThenBy(
                    x => x.Id)
                .Select(
                    x =>
                        new WarehousePickLineDto(
                            x.Id,
                            x.OrderInventoryReservationId,
                            x.ReservationKey,
                            x.ProductVariantId,
                            x.WarehouseId,
                            x.LocationId,
                            x.Quantity,
                            x.Status.ToString()))
                .ToList());
    }

    private static void ValidateTenant(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }

    private static void ValidateOrderId(
        Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }
    }
}

public sealed record WarehousePickLineDto(
    Guid Id,
    Guid OrderInventoryReservationId,
    string ReservationKey,
    Guid ProductVariantId,
    Guid WarehouseId,
    Guid LocationId,
    int Quantity,
    string Status);

public sealed record FulfillmentPickingResultDto(
    Guid OrderId,
    string OrderNumber,
    Guid FulfillmentId,
    string Status,
    bool AlreadyProcessed,
    IReadOnlyList<WarehousePickLineDto> Lines);