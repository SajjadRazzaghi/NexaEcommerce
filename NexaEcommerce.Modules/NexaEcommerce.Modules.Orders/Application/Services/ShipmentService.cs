using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class ShipmentService(
    IShipmentRepository shipments,
    IOrderRepository orders,
    IOrderUnitOfWork unitOfWork)
    : IShipmentService
{
    public async Task<ShipmentDto?> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            return null;
        }

        var order =
            await orders.GetByIdAsync(
                tenantId,
                orderId,
                userId,
                cancellationToken);

        if (order is null)
        {
            return null;
        }

        var shipment =
            await shipments.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        return shipment is null
            ? null
            : Map(shipment);
    }

    public async Task<ShipmentDto> CreateAsync(
        string tenantId,
        Guid orderId,
        string shippingMethod,
        string carrier,
        string? trackingNumber,
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

        if (order.Status !=
            OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "A shipment can only be created for an order in Processing status.");
        }

        var existing =
            await shipments.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (existing is not null)
        {
            return Map(existing);
        }

        var shipment =
            Shipment.Create(
                order.Id,
                tenantId,
                shippingMethod,
                carrier,
                trackingNumber);

        await shipments.AddAsync(
            shipment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(shipment);
    }

    public async Task<ShipmentDto> SetTrackingNumberAsync(
        string tenantId,
        Guid orderId,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(
            tenantId,
            orderId);

        if (string.IsNullOrWhiteSpace(
                trackingNumber))
        {
            throw new ArgumentException(
                "Tracking number is required.",
                nameof(trackingNumber));
        }

        var shipment =
            await shipments.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (shipment is null)
        {
            throw new KeyNotFoundException(
                "Shipment was not found.");
        }

        shipment.SetTrackingNumber(
            trackingNumber);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(shipment);
    }

    public async Task<ShipmentDto> ShipAsync(
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

        var shipment =
            await shipments.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (shipment is null)
        {
            throw new KeyNotFoundException(
                "Shipment was not found.");
        }

        shipment.MarkShipped();

        order.MarkShipped();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(shipment);
    }

    public async Task<ShipmentDto> DeliverAsync(
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

        var shipment =
            await shipments.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (shipment is null)
        {
            throw new KeyNotFoundException(
                "Shipment was not found.");
        }

        shipment.MarkDelivered();

        order.MarkDelivered();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(shipment);
    }

    private static ShipmentDto Map(
        Shipment shipment)
    {
        return new ShipmentDto(
            shipment.Id,
            shipment.OrderId,
            shipment.ShippingMethod,
            shipment.Carrier,
            shipment.TrackingNumber,
            shipment.Status.ToString(),
            shipment.CreatedAt,
            shipment.ShippedAt,
            shipment.DeliveredAt);
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