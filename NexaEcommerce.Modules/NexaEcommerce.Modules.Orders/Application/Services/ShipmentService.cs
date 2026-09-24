using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class ShipmentService(
    IShipmentRepository shipments,
    IOrderRepository orders,
    IShippingMethodRepository shippingMethods,
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

        ValidateText(
            shippingMethod,
            nameof(shippingMethod),
            100);

        ValidateText(
            carrier,
            nameof(carrier),
            100);

        if (!string.IsNullOrWhiteSpace(trackingNumber) &&
            trackingNumber.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Tracking number cannot exceed 200 characters.",
                nameof(trackingNumber));
        }

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

        if (!order.ShippingMethodId.HasValue ||
            order.ShippingMethodId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The order does not have a persisted shipping method.");
        }

        var selectedShippingMethod =
            await shippingMethods.GetByIdAsync(
                tenantId,
                order.ShippingMethodId.Value,
                cancellationToken);

        if (selectedShippingMethod is null)
        {
            throw new InvalidOperationException(
                "The selected shipping method no longer exists.");
        }

        var normalizedShippingMethod =
            shippingMethod.Trim();

        var normalizedCarrier =
            carrier.Trim();

        if (!string.Equals(
                normalizedShippingMethod,
                selectedShippingMethod.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The shipment shipping method does not match the shipping method selected for the order.");
        }

        if (!string.Equals(
                normalizedCarrier,
                selectedShippingMethod.Carrier,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The shipment carrier does not match the shipping method selected for the order.");
        }

        var normalizedTrackingNumber =
            string.IsNullOrWhiteSpace(
                trackingNumber)
                ? null
                : trackingNumber.Trim();

        var existing =
            await shipments.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (existing is not null)
        {
            var sameShippingMethod =
                string.Equals(
                    existing.ShippingMethod,
                    normalizedShippingMethod,
                    StringComparison.Ordinal);

            var sameCarrier =
                string.Equals(
                    existing.Carrier,
                    normalizedCarrier,
                    StringComparison.Ordinal);

            var sameTrackingNumber =
                string.Equals(
                    existing.TrackingNumber,
                    normalizedTrackingNumber,
                    StringComparison.Ordinal);

            if (!sameShippingMethod ||
                !sameCarrier ||
                !sameTrackingNumber)
            {
                throw new InvalidOperationException(
                    "A shipment already exists for this order with different shipping details.");
            }

            return Map(existing);
        }

        var shipment =
            Shipment.Create(
                order.Id,
                tenantId,
                normalizedShippingMethod,
                normalizedCarrier,
                normalizedTrackingNumber);

        await shipments.AddAsync(
            shipment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(shipment);
    }

    public async Task<ShipmentDto> PrepareAsync(
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

        if (order.Status !=
            OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "A shipment can only be prepared for an order in Processing status.");
        }

        if (!order.ShippingMethodId.HasValue ||
            order.ShippingMethodId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The order does not have a shipping method selected.");
        }

        var selectedShippingMethod =
            await shippingMethods.GetByIdAsync(
                tenantId,
                order.ShippingMethodId.Value,
                cancellationToken);

        if (selectedShippingMethod is null)
        {
            throw new InvalidOperationException(
                "The selected shipping method no longer exists.");
        }

        if (selectedShippingMethod.Name.Trim().Length > 100)
        {
            throw new InvalidOperationException(
                "The selected shipping method name is too long for shipment preparation.");
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
                selectedShippingMethod.Name,
                selectedShippingMethod.Carrier,
                null);

        await shipments.AddAsync(
            shipment,
            cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            var raced =
                await shipments.GetByOrderIdAsync(
                    tenantId,
                    orderId,
                    cancellationToken);

            if (raced is not null)
            {
                return Map(raced);
            }

            throw;
        }

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

        if (shipment.Status ==
            ShipmentStatus.Shipped &&
            order.Status ==
            OrderStatus.Shipped)
        {
            return Map(shipment);
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

        if (shipment.Status ==
            ShipmentStatus.Delivered &&
            order.Status ==
            OrderStatus.Delivered)
        {
            return Map(shipment);
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

    private static void ValidateText(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }
    }
}