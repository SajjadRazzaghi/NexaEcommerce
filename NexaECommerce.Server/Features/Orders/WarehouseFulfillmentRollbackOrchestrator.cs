using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class WarehouseFulfillmentRollbackOrchestrator(
    IOrderRepository orderRepository,
    IFulfillmentRepository fulfillmentRepository,
    IPackageRepository packageRepository,
    IShipmentRepository shipmentRepository,
    IWarehouseStockRepository warehouseStockRepository,
    IWarehouseStockReservationRepository warehouseReservationRepository,
    IInventoryUnitOfWork inventoryUnitOfWork,
    IOrderUnitOfWork orderUnitOfWork)
{
    public async Task<FulfillmentDto> ExecuteAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(
            tenantId,
            orderId);

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
                "Only processing orders can be rolled back through the warehouse workflow.");
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

        switch (fulfillment.Status)
        {
            case FulfillmentStatus.ReadyToShip:

                await RollbackReadyToShipAsync(
                    normalizedTenantId,
                    orderId,
                    fulfillment,
                    cancellationToken);

                break;

            case FulfillmentStatus.Packed:

                await RollbackPackedAsync(
                    normalizedTenantId,
                    orderId,
                    fulfillment,
                    cancellationToken);

                break;

            case FulfillmentStatus.Packing:

                await RollbackPackingAsync(
                    normalizedTenantId,
                    orderId,
                    fulfillment,
                    cancellationToken);

                break;

            case FulfillmentStatus.Picked:

                await RollbackPickedAsync(
                    normalizedTenantId,
                    orderId,
                    fulfillment,
                    cancellationToken);

                break;

            case FulfillmentStatus.Picking:

                fulfillment.RollbackToPending();

                await orderUnitOfWork.SaveChangesAsync(
                    cancellationToken);

                break;

            case FulfillmentStatus.Pending:

                throw new InvalidOperationException(
                    "Fulfillment is already at the first warehouse stage.");

            case FulfillmentStatus.Shipped:

            case FulfillmentStatus.Delivered:

                throw new InvalidOperationException(
                    "Shipped or delivered orders cannot be rolled back through the warehouse workflow.");

            case FulfillmentStatus.Cancelled:

                throw new InvalidOperationException(
                    "Cancelled fulfillment cannot be rolled back.");
        }

        return Map(
            fulfillment);
    }

    private async Task RollbackReadyToShipAsync(
        string tenantId,
        Guid orderId,
        Fulfillment fulfillment,
        CancellationToken cancellationToken)
    {
        var packages =
            await GetActivePackagesAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (packages.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one active package is required.");
        }

        foreach (var packageSummary in packages)
        {
            if (packageSummary.Status is
                PackageStatus.Shipped or
                PackageStatus.Delivered)
            {
                throw new InvalidOperationException(
                    "A shipped or delivered package cannot be rolled back.");
            }

            if (packageSummary.Status is not
                PackageStatus.Packed and not
                PackageStatus.LabelPrinted)
            {
                throw new InvalidOperationException(
                    $"Package '{packageSummary.Id}' is not in a valid state for rollback.");
            }

            var package =
                await packageRepository.GetByIdAsync(
                    tenantId,
                    packageSummary.Id,
                    cancellationToken);

            if (package is null)
            {
                throw new KeyNotFoundException(
                    "Package was not found.");
            }

            package.RollbackToPacked(
                DateTime.UtcNow);
        }

        var shipment =
            await shipmentRepository.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (shipment is not null)
        {
            shipment.ResetForFulfillmentRollback();
        }

        fulfillment.RollbackToPacked();

        await orderUnitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task RollbackPackedAsync(
        string tenantId,
        Guid orderId,
        Fulfillment fulfillment,
        CancellationToken cancellationToken)
    {
        var packages =
            await GetActivePackagesAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (packages.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one active package is required.");
        }

        foreach (var packageSummary in packages)
        {
            if (packageSummary.Status is
                PackageStatus.Shipped or
                PackageStatus.Delivered)
            {
                throw new InvalidOperationException(
                    "A shipped or delivered package cannot be rolled back.");
            }

            if (packageSummary.Status is not
                PackageStatus.Packed and not
                PackageStatus.LabelPrinted)
            {
                throw new InvalidOperationException(
                    $"Package '{packageSummary.Id}' is not in a valid state for rollback.");
            }

            var package =
                await packageRepository.GetByIdAsync(
                    tenantId,
                    packageSummary.Id,
                    cancellationToken);

            if (package is null)
            {
                throw new KeyNotFoundException(
                    "Package was not found.");
            }

            package.RollbackToPacking(
                DateTime.UtcNow);
        }

        fulfillment.RollbackToPacking();

        await orderUnitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task RollbackPackingAsync(
        string tenantId,
        Guid orderId,
        Fulfillment fulfillment,
        CancellationToken cancellationToken)
    {
        var packages =
            await GetActivePackagesAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (packages.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one active package is required.");
        }

        foreach (var packageSummary in packages)
        {
            if (packageSummary.Status is
                PackageStatus.Shipped or
                PackageStatus.Delivered)
            {
                throw new InvalidOperationException(
                    "A shipped or delivered package cannot be rolled back.");
            }

            if (packageSummary.Status !=
                PackageStatus.Packing)
            {
                throw new InvalidOperationException(
                    $"Package '{packageSummary.Id}' must be in Packing status.");
            }

            var package =
                await packageRepository.GetByIdAsync(
                    tenantId,
                    packageSummary.Id,
                    cancellationToken);

            if (package is null)
            {
                throw new KeyNotFoundException(
                    "Package was not found.");
            }

            package.RollbackToDraft(
                DateTime.UtcNow);
        }

        fulfillment.RollbackToPicked();

        await orderUnitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task RollbackPickedAsync(
        string tenantId,
        Guid orderId,
        Fulfillment fulfillment,
        CancellationToken cancellationToken)
    {
        var reservations =
            await warehouseReservationRepository.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (reservations.Count == 0)
        {
            throw new InvalidOperationException(
                "Warehouse reservations were not found for this order.");
        }

        if (reservations.Any(
                x =>
                    x.Status ==
                    WarehouseStockReservationStatus.Released))
        {
            throw new InvalidOperationException(
                "Released warehouse reservations cannot be restored.");
        }

        var consumed =
            reservations
                .Where(
                    x =>
                        x.Status ==
                        WarehouseStockReservationStatus.Consumed)
                .ToList();

        var reserved =
            reservations
                .Where(
                    x =>
                        x.Status ==
                        WarehouseStockReservationStatus.Reserved)
                .ToList();

        /*
         * If a previous rollback attempt already restored the
         * reservations but failed before changing fulfillment,
         * simply finish the fulfillment rollback.
         */
        if (consumed.Count == 0 &&
            reserved.Count == reservations.Count)
        {
            fulfillment.RollbackToPicking();

            await orderUnitOfWork.SaveChangesAsync(
                cancellationToken);

            return;
        }

        if (consumed.Count !=
            reservations.Count)
        {
            throw new InvalidOperationException(
                "Warehouse reservations are in an inconsistent state.");
        }

        await inventoryUnitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var groups =
                    consumed
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
                            tenantId,
                            group.Key.WarehouseId,
                            group.Key.LocationId,
                            group.Key.ProductVariantId,
                            ct);

                    if (stock is null)
                    {
                        throw new InvalidOperationException(
                            "Warehouse stock was not found while rolling back picking.");
                    }

                    var quantity =
                        group.Sum(
                            x => x.Quantity);

                    stock.RestoreConsumed(
                        quantity);

                    var movement =
                        WarehouseStockMovement.Create(
                            tenantId,
                            group.Key.WarehouseId,
                            group.Key.LocationId,
                            group.Key.ProductVariantId,
                            WarehouseStockMovementType.Correction,
                            quantity,
                            stock.OnHandQuantity,
                            "WarehouseFulfillmentRollback",
                            orderId.ToString(),
                            "Warehouse stock restored because picking was rolled back.");

                    await warehouseStockRepository.AddMovementAsync(
                        movement,
                        ct);
                }

                foreach (var reservation in consumed)
                {
                    reservation.RestoreToReserved();
                }

                return true;
            },
            cancellationToken);

        fulfillment.RollbackToPicking();

        await orderUnitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<IReadOnlyList<Package>>
        GetActivePackagesAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken)
    {
        return
            (await packageRepository.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken))
            .Where(
                x =>
                    x.Status !=
                    PackageStatus.Cancelled)
            .OrderBy(
                x => x.PackageNumber)
            .ThenBy(
                x => x.Id)
            .ToList();
    }

    private static FulfillmentDto Map(
        Fulfillment fulfillment)
    {
        return new FulfillmentDto(
            fulfillment.Id,
            fulfillment.OrderId,
            fulfillment.Status.ToString(),
            fulfillment.WarehouseId,
            fulfillment.PickingLocationId,
            fulfillment.PickingStartedAt,
            fulfillment.PickedAt,
            fulfillment.PackingStartedAt,
            fulfillment.PackedAt,
            fulfillment.ReadyToShipAt,
            fulfillment.ShippedAt,
            fulfillment.DeliveredAt,
            fulfillment.CreatedAt,
            fulfillment.UpdatedAt);
    }

    private static void ValidateScope(
        string tenantId,
        Guid orderId)
    {
        if (string.IsNullOrWhiteSpace(
                tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }
    }
}