using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class WarehouseShipmentOrchestrator(
IOrderRepository orders,
IFulfillmentRepository fulfillments,
IPackageService packages,
IShipmentService shipments,
IOrderUnitOfWork unitOfWork)
{
    public async Task<WarehouseShipmentResultDto> ShipAsync(
    string tenantId,
    Guid orderId,
    CancellationToken cancellationToken = default)
    {
        ValidateScope(
        tenantId,
        orderId);


    var order =
        await orders.GetByIdAsync(
            tenantId,
            orderId,
            null,
            cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        var fulfillment =
            await fulfillments.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (fulfillment is null)
        {
            throw new KeyNotFoundException(
                "Fulfillment was not found.");
        }

        var activePackages =
            (await packages.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken))
            .Where(x =>
                !string.Equals(
                    x.Status,
                    PackageStatus.Cancelled.ToString(),
                    StringComparison.Ordinal))
            .ToList();

        if (activePackages.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one active package is required before shipping.");
        }

        var shipment =
            await shipments.GetByOrderAsync(
                tenantId,
                orderId,
                null,
                cancellationToken);

        if (shipment is null)
        {
            throw new KeyNotFoundException(
                "Shipment was not found.");
        }

        if (string.IsNullOrWhiteSpace(
                shipment.TrackingNumber))
        {
            throw new InvalidOperationException(
                "Tracking number must be set before shipping.");
        }

        var orderAlreadyShipped =
            order.Status == OrderStatus.Shipped;

        var fulfillmentAlreadyShipped =
            fulfillment.Status == FulfillmentStatus.Shipped;

        var shipmentAlreadyShipped =
            string.Equals(
                shipment.Status,
                ShipmentStatus.Shipped.ToString(),
                StringComparison.Ordinal);

        var packagesAlreadyShipped =
            activePackages.All(x =>
                string.Equals(
                    x.Status,
                    PackageStatus.Shipped.ToString(),
                    StringComparison.Ordinal));

        if (orderAlreadyShipped &&
            fulfillmentAlreadyShipped &&
            shipmentAlreadyShipped &&
            packagesAlreadyShipped)
        {
            return MapResult(
                order,
                fulfillment,
                activePackages,
                shipment,
                alreadyProcessed: true);
        }

        if (fulfillment.Status !=
            FulfillmentStatus.ReadyToShip)
        {
            throw new InvalidOperationException(
                "Fulfillment must be in ReadyToShip status before shipping.");
        }

        if (order.Status !=
            OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "Order must be in Processing status before shipping.");
        }

        if (activePackages.Any(x =>
                !string.Equals(
                    x.Status,
                    PackageStatus.Packed.ToString(),
                    StringComparison.Ordinal) &&
                !string.Equals(
                    x.Status,
                    PackageStatus.LabelPrinted.ToString(),
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "All active packages must be Packed or LabelPrinted before shipping.");
        }

        foreach (var package in activePackages)
        {
            if (string.IsNullOrWhiteSpace(
                    package.TrackingNumber))
            {
                await packages.SetTrackingAsync(
                    tenantId,
                    package.Id,
                    new SetPackageTrackingRequest(
                        shipment.TrackingNumber),
                    cancellationToken);
            }

            await packages.MarkShippedAsync(
                tenantId,
                package.Id,
                cancellationToken);
        }

        var shippedShipment =
            await shipments.ShipAsync(
                tenantId,
                orderId,
                cancellationToken);

        fulfillment.MarkShipped();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        var shippedPackages =
            await packages.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken);

        return MapResult(
            order,
            fulfillment,
            shippedPackages
                .Where(x =>
                    !string.Equals(
                        x.Status,
                        PackageStatus.Cancelled.ToString(),
                        StringComparison.Ordinal))
                .ToList(),
            shippedShipment,
            alreadyProcessed: false);
    }

    public async Task<WarehouseShipmentResultDto> DeliverAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(
            tenantId,
            orderId);

        var order =
            await orders.GetByIdAsync(
                tenantId,
                orderId,
                null,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        var fulfillment =
            await fulfillments.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (fulfillment is null)
        {
            throw new KeyNotFoundException(
                "Fulfillment was not found.");
        }

        var activePackages =
            (await packages.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken))
            .Where(x =>
                !string.Equals(
                    x.Status,
                    PackageStatus.Cancelled.ToString(),
                    StringComparison.Ordinal))
            .ToList();

        if (activePackages.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one active package is required before delivery.");
        }

        var shipment =
            await shipments.GetByOrderAsync(
                tenantId,
                orderId,
                null,
                cancellationToken);

        if (shipment is null)
        {
            throw new KeyNotFoundException(
                "Shipment was not found.");
        }

        var orderAlreadyDelivered =
            order.Status == OrderStatus.Delivered;

        var fulfillmentAlreadyDelivered =
            fulfillment.Status == FulfillmentStatus.Delivered;

        var shipmentAlreadyDelivered =
            string.Equals(
                shipment.Status,
                ShipmentStatus.Delivered.ToString(),
                StringComparison.Ordinal);

        var packagesAlreadyDelivered =
            activePackages.All(x =>
                string.Equals(
                    x.Status,
                    PackageStatus.Delivered.ToString(),
                    StringComparison.Ordinal));

        if (orderAlreadyDelivered &&
            fulfillmentAlreadyDelivered &&
            shipmentAlreadyDelivered &&
            packagesAlreadyDelivered)
        {
            return MapResult(
                order,
                fulfillment,
                activePackages,
                shipment,
                alreadyProcessed: true);
        }

        if (fulfillment.Status !=
            FulfillmentStatus.Shipped)
        {
            throw new InvalidOperationException(
                "Fulfillment must be in Shipped status before delivery.");
        }

        if (!string.Equals(
                shipment.Status,
                ShipmentStatus.Shipped.ToString(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Shipment must be in Shipped status before delivery.");
        }

        if (order.Status !=
            OrderStatus.Shipped)
        {
            throw new InvalidOperationException(
                "Order must be in Shipped status before delivery.");
        }

        if (string.IsNullOrWhiteSpace(
                shipment.TrackingNumber))
        {
            throw new InvalidOperationException(
                "Tracking number must be set before delivery.");
        }

        /*
         * A Package should normally already be Shipped at this point.
         *
         * However, an existing order may have been persisted with the
         * aggregate shipment state as Shipped while the package itself
         * remained Packed/LabelPrinted. In that case we repair the
         * package state using the shipment tracking number and continue
         * to delivery.
         */
        foreach (var package in activePackages)
        {
            var isDelivered =
                string.Equals(
                    package.Status,
                    PackageStatus.Delivered.ToString(),
                    StringComparison.Ordinal);

            if (isDelivered)
            {
                continue;
            }

            var isShipped =
                string.Equals(
                    package.Status,
                    PackageStatus.Shipped.ToString(),
                    StringComparison.Ordinal);

            var isPacked =
                string.Equals(
                    package.Status,
                    PackageStatus.Packed.ToString(),
                    StringComparison.Ordinal);

            var isLabelPrinted =
                string.Equals(
                    package.Status,
                    PackageStatus.LabelPrinted.ToString(),
                    StringComparison.Ordinal);

            if (isPacked ||
                isLabelPrinted)
            {
                if (string.IsNullOrWhiteSpace(
                        package.TrackingNumber))
                {
                    await packages.SetTrackingAsync(
                        tenantId,
                        package.Id,
                        new SetPackageTrackingRequest(
                            shipment.TrackingNumber),
                        cancellationToken);
                }

                await packages.MarkShippedAsync(
                    tenantId,
                    package.Id,
                    cancellationToken);

                continue;
            }

            if (!isShipped)
            {
                throw new InvalidOperationException(
                    "All active packages must be Shipped or Delivered before delivery.");
            }
        }

        foreach (var package in
                 await packages.GetByOrderAsync(
                     tenantId,
                     orderId,
                     cancellationToken))
        {
            if (string.Equals(
                    package.Status,
                    PackageStatus.Cancelled.ToString(),
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(
                    package.Status,
                    PackageStatus.Delivered.ToString(),
                    StringComparison.Ordinal))
            {
                continue;
            }

            await packages.MarkDeliveredAsync(
                tenantId,
                package.Id,
                cancellationToken);
        }

        var deliveredShipment =
            await shipments.DeliverAsync(
                tenantId,
                orderId,
                cancellationToken);

        fulfillment.MarkDelivered();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        var deliveredPackages =
            await packages.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken);

        return MapResult(
            order,
            fulfillment,
            deliveredPackages
                .Where(x =>
                    !string.Equals(
                        x.Status,
                        PackageStatus.Cancelled.ToString(),
                        StringComparison.Ordinal))
                .ToList(),
            deliveredShipment,
            alreadyProcessed: false);
    }

    private static WarehouseShipmentResultDto MapResult(
        Order order,
        Fulfillment fulfillment,
        IReadOnlyCollection<PackageDto> packages,
        ShipmentDto shipment,
        bool alreadyProcessed)
    {
        return new WarehouseShipmentResultDto(
            order.Id,
            order.OrderNumber,
            fulfillment.Id,
            fulfillment.Status.ToString(),
            packages,
            shipment,
            alreadyProcessed);
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

public sealed record WarehouseShipmentResultDto(
Guid OrderId,
string OrderNumber,
Guid FulfillmentId,
string FulfillmentStatus,
IReadOnlyCollection<PackageDto> Packages,
ShipmentDto Shipment,
bool AlreadyProcessed);
