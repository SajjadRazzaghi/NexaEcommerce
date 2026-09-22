using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class WarehouseReservationOrchestrator(
    IOrderRepository orderRepository,
    IFulfillmentRepository fulfillmentRepository,
    IWarehouseRepository warehouseRepository,
    IWarehouseStockRepository warehouseStockRepository,
    IWarehouseStockReservationRepository reservationRepository,
    IInventoryUnitOfWork inventoryUnitOfWork)
    : IWarehouseReservationOrchestrator
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<WarehouseReservationResultDto> ReserveAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        var normalizedTenantId = tenantId.Trim();

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

        if (order.Status is
            not OrderStatus.PendingPayment and
            not OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "Warehouse stock can only be reserved for pending-payment or processing orders.");
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

        if (!fulfillment.WarehouseId.HasValue)
        {
            throw new InvalidOperationException(
                "Warehouse must be allocated before warehouse stock can be reserved.");
        }

        ValidateOrderReservations(order);

        var existing =
            await reservationRepository.GetByOrderAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        if (existing.Count > 0)
        {
            EnsureExistingReservationsMatchOrder(
                order,
                existing);

            return MapResult(
                order,
                fulfillment,
                existing,
                alreadyReserved: true);
        }

        var warehouseId =
            fulfillment.WarehouseId.Value;

        var warehouse =
            await warehouseRepository.GetWarehouseAsync(
                normalizedTenantId,
                warehouseId,
                cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException(
                "Allocated warehouse was not found.");
        }

        if (!warehouse.IsActive)
        {
            throw new InvalidOperationException(
                "Allocated warehouse is inactive.");
        }

        var locations =
            await warehouseRepository.GetLocationsAsync(
                normalizedTenantId,
                warehouseId,
                includeInactive: false,
                cancellationToken);

        if (locations.Count == 0)
        {
            throw new InvalidOperationException(
                "Allocated warehouse has no active locations.");
        }

        var locationIds =
            locations
                .Select(x => x.Id)
                .ToHashSet();

        var stockSnapshot =
            await warehouseStockRepository.GetByWarehouseAsync(
                normalizedTenantId,
                warehouseId,
                includeZeroStock: true,
                cancellationToken: cancellationToken);

        var availableStock =
            stockSnapshot
                .Where(
                    x =>
                        locationIds.Contains(
                            x.LocationId))
                .ToList();

        var required =
            order.InventoryReservations
                .Where(
                    x =>
                        x.Status is
                            InventoryReservationStatus.Reserved or
                            InventoryReservationStatus.Committed)
                .GroupBy(
                    x =>
                        x.ProductVariantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(
                        x => x.Quantity));

        foreach (var requirement in required)
        {
            var available =
                availableStock
                    .Where(
                        x =>
                            x.ProductVariantId ==
                            requirement.Key)
                    .Sum(
                        x =>
                            x.AvailableQuantity);

            if (available < requirement.Value)
            {
                throw new InvalidOperationException(
                    $"Insufficient warehouse stock for product variant '{requirement.Key}'.");
            }
        }

        var plannedChunks =
            BuildPlan(
                order,
                availableStock);

        for (var attempt = 1;
             attempt <= MaxConcurrencyRetries;
             attempt++)
        {
            try
            {
                var created =
                    await inventoryUnitOfWork
                        .ExecuteInTransactionAsync(
                            async ct =>
                            {
                                var createdReservations =
                                    new List<WarehouseStockReservation>(
                                        plannedChunks.Count);

                                var grouped =
                                    plannedChunks
                                        .GroupBy(
                                            x =>
                                                new
                                                {
                                                    x.LocationId,
                                                    x.ProductVariantId
                                                })
                                        .ToList();

                                foreach (var group in grouped)
                                {
                                    ct.ThrowIfCancellationRequested();

                                    var stock =
                                        await warehouseStockRepository.GetAsync(
                                            normalizedTenantId,
                                            warehouseId,
                                            group.Key.LocationId,
                                            group.Key.ProductVariantId,
                                            ct);

                                    if (stock is null)
                                    {
                                        throw new InvalidOperationException(
                                            "Warehouse stock changed while reserving order inventory.");
                                    }

                                    var totalQuantity =
                                        group.Sum(
                                            x => x.Quantity);

                                    stock.Reserve(
                                        totalQuantity);
                                }

                                foreach (var chunk in plannedChunks)
                                {
                                    ct.ThrowIfCancellationRequested();

                                    var reservation =
                                        WarehouseStockReservation.Create(
                                            normalizedTenantId,
                                            order.Id,
                                            fulfillment.Id,
                                            chunk.OrderInventoryReservationId,
                                            chunk.ReservationKey,
                                            warehouseId,
                                            chunk.LocationId,
                                            chunk.ProductVariantId,
                                            chunk.Quantity);

                                    await reservationRepository.AddAsync(
                                        reservation,
                                        ct);

                                    createdReservations.Add(
                                        reservation);
                                }

                                return createdReservations;
                            },
                            cancellationToken);

                return MapResult(
                    order,
                    fulfillment,
                    created,
                    alreadyReserved: false);
            }
            catch (DbUpdateConcurrencyException)
                when (attempt < MaxConcurrencyRetries)
            {
                await Task.Yield();
            }
        }

        throw new InvalidOperationException(
            "Warehouse stock changed concurrently. Please retry the warehouse reservation.");
    }

    public async Task<WarehouseReleaseResultDto> ReleaseAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        var normalizedTenantId =
            tenantId.Trim();

        var activeReservations =
            await reservationRepository.GetActiveByOrderAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        if (activeReservations.Count == 0)
        {
            return new WarehouseReleaseResultDto(
                orderId,
                0,
                false);
        }

        await inventoryUnitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var grouped =
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

                    foreach (var group in grouped)
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
                                "Warehouse stock was not found while releasing the reservation.");
                        }

                        stock.Release(
                            group.Sum(
                                x => x.Quantity));
                    }

                    foreach (var reservation in activeReservations)
                    {
                        reservation.Release();
                    }

                    return true;
                },
                cancellationToken);

        return new WarehouseReleaseResultDto(
            orderId,
            activeReservations.Sum(
                x => x.Quantity),
            true);
    }

    private static List<WarehouseReservationPlan> BuildPlan(
        Order order,
        IReadOnlyList<WarehouseStock> stocks)
    {
        var plans =
            new List<WarehouseReservationPlan>();

        var reservations =
            order.InventoryReservations
                .Where(
                    x =>
                        x.Status is
                            InventoryReservationStatus.Reserved or
                            InventoryReservationStatus.Committed)
                .OrderBy(
                    x =>
                        x.ReservationKey)
                .ToList();

        foreach (var reservation in reservations)
        {
            var remaining =
                reservation.Quantity;

            var candidates =
                stocks
                    .Where(
                        x =>
                            x.ProductVariantId ==
                            reservation.ProductVariantId &&
                            x.AvailableQuantity > 0)
                    .OrderByDescending(
                        x =>
                            x.AvailableQuantity)
                    .ThenBy(
                        x =>
                            x.LocationId)
                    .ToList();

            foreach (var stock in candidates)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var quantity =
                    Math.Min(
                        remaining,
                        stock.AvailableQuantity);

                if (quantity <= 0)
                {
                    continue;
                }

                plans.Add(
                    new WarehouseReservationPlan(
                        reservation.Id,
                        reservation.ReservationKey,
                        reservation.ProductVariantId,
                        stock.LocationId,
                        quantity));

                remaining -= quantity;
            }

            if (remaining > 0)
            {
                throw new InvalidOperationException(
                    $"Insufficient warehouse stock for reservation '{reservation.ReservationKey}'.");
            }
        }

        return plans;
    }

    private static void ValidateOrderReservations(
        Order order)
    {
        if (order.InventoryReservations.Count == 0)
        {
            throw new InvalidOperationException(
                "Order has no inventory reservations.");
        }

        var itemRequirements =
            order.Items
                .GroupBy(
                    x =>
                        x.ProductVariantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(
                        x => x.Quantity));

        var reservationRequirements =
            order.InventoryReservations
                .Where(
                    x =>
                        x.Status is
                            InventoryReservationStatus.Reserved or
                            InventoryReservationStatus.Committed)
                .GroupBy(
                    x =>
                        x.ProductVariantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(
                        x => x.Quantity));

        foreach (var item in itemRequirements)
        {
            if (!reservationRequirements.TryGetValue(
                    item.Key,
                    out var reservedQuantity) ||
                reservedQuantity != item.Value)
            {
                throw new InvalidOperationException(
                    $"Inventory reservations do not match order quantity for product variant '{item.Key}'.");
            }
        }

        if (reservationRequirements.Count !=
            itemRequirements.Count)
        {
            throw new InvalidOperationException(
                "Inventory reservations do not match order items.");
        }
    }

    private static void EnsureExistingReservationsMatchOrder(
        Order order,
        IReadOnlyList<WarehouseStockReservation> existing)
    {
        var expected =
            order.InventoryReservations
                .Where(
                    x =>
                        x.Status is
                            InventoryReservationStatus.Reserved or
                            InventoryReservationStatus.Committed)
                .GroupBy(
                    x =>
                        x.ProductVariantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(
                        x => x.Quantity));

        var actual =
            existing
                .Where(
                    x =>
                        x.Status ==
                        WarehouseStockReservationStatus.Reserved)
                .GroupBy(
                    x =>
                        x.ProductVariantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(
                        x => x.Quantity));

        if (expected.Count !=
            actual.Count)
        {
            throw new InvalidOperationException(
                "Existing warehouse reservations do not match the order.");
        }

        foreach (var pair in expected)
        {
            if (!actual.TryGetValue(
                    pair.Key,
                    out var quantity) ||
                quantity != pair.Value)
            {
                throw new InvalidOperationException(
                    "Existing warehouse reservations do not match the order.");
            }
        }
    }

    private static WarehouseReservationResultDto MapResult(
        Order order,
        Fulfillment fulfillment,
        IReadOnlyList<WarehouseStockReservation> reservations,
        bool alreadyReserved)
    {
        return new WarehouseReservationResultDto(
            order.Id,
            order.OrderNumber,
            fulfillment.Id,
            fulfillment.WarehouseId!.Value,
            alreadyReserved,
            reservations
                .OrderBy(
                    x =>
                        x.ProductVariantId)
                .ThenBy(
                    x =>
                        x.LocationId)
                .Select(
                    x =>
                        new WarehouseReservationLineDto(
                            x.Id,
                            x.OrderInventoryReservationId,
                            x.ReservationKey,
                            x.ProductVariantId,
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

    private sealed record WarehouseReservationPlan(
        Guid OrderInventoryReservationId,
        string ReservationKey,
        Guid ProductVariantId,
        Guid LocationId,
        int Quantity);
}

public sealed record WarehouseReservationLineDto(
    Guid Id,
    Guid OrderInventoryReservationId,
    string ReservationKey,
    Guid ProductVariantId,
    Guid LocationId,
    int Quantity,
    string Status);

public sealed record WarehouseReservationResultDto(
    Guid OrderId,
    string OrderNumber,
    Guid FulfillmentId,
    Guid WarehouseId,
    bool AlreadyReserved,
    IReadOnlyList<WarehouseReservationLineDto> Lines);

public sealed record WarehouseReleaseResultDto(
    Guid OrderId,
    int ReleasedQuantity,
    bool Released);