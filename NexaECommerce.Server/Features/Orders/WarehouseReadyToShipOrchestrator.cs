using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class WarehouseReadyToShipOrchestrator(
    IOrderRepository orderRepository,
    IFulfillmentRepository fulfillmentRepository,
    IPackageRepository packageRepository,
    IShipmentService shipmentService,
    IOrderUnitOfWork unitOfWork)
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
                "Order must be in Processing status before it can become ready to ship.");
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

        var packageSummaries =
            (await packageRepository.GetByOrderIdAsync(
                normalizedTenantId,
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

        if (packageSummaries.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one active package is required before the order becomes ready to ship.");
        }

        foreach (var package in packageSummaries)
        {
            if (package.Status is
                PackageStatus.Packed or
                PackageStatus.LabelPrinted)
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Package '{package.Id}' must be packed before the order becomes ready to ship.");
        }

        /*
         * Shipment is prepared from the shipping method selected
         * during checkout. This also repairs an old/partial state
         * where Fulfillment is already ReadyToShip but the shipment
         * record is missing.
         */
        await shipmentService.PrepareAsync(
            normalizedTenantId,
            orderId,
            cancellationToken);

        if (fulfillment.Status ==
            FulfillmentStatus.ReadyToShip)
        {
            return Map(
                fulfillment);
        }

        if (fulfillment.Status !=
            FulfillmentStatus.Packed)
        {
            throw new InvalidOperationException(
                "Fulfillment must be in Packed status before it can become ready to ship.");
        }

        fulfillment.MarkReadyToShip();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(
            fulfillment);
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