using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class Shipment : BaseEntity
{
    private Shipment()
    {
    }

    private Shipment(
        Guid orderId,
        string tenantId,
        string shippingMethod,
        string carrier,
        string? trackingNumber)
    {
        OrderId = orderId;
        TenantId = tenantId.Trim();
        ShippingMethod = shippingMethod.Trim();
        Carrier = carrier.Trim();
        TrackingNumber =
            string.IsNullOrWhiteSpace(trackingNumber)
                ? null
                : trackingNumber.Trim();

        Status =
            ShipmentStatus.Pending;

        CreatedAt =
            DateTime.UtcNow;
    }

    public Guid OrderId { get; private set; }

    public string TenantId { get; private set; } = null!;

    public string ShippingMethod { get; private set; } = null!;

    public string Carrier { get; private set; } = null!;

    public string? TrackingNumber { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public DateTime? ShippedAt { get; private set; }

    public DateTime? DeliveredAt { get; private set; }

    public static Shipment Create(
        Guid orderId,
        string tenantId,
        string shippingMethod,
        string carrier,
        string? trackingNumber = null)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        ValidateText(
            tenantId,
            nameof(tenantId),
            64);

        ValidateText(
            shippingMethod,
            nameof(shippingMethod),
            100);

        ValidateText(
            carrier,
            nameof(carrier),
            100);

        if (!string.IsNullOrWhiteSpace(
                trackingNumber) &&
            trackingNumber.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Tracking number cannot exceed 200 characters.",
                nameof(trackingNumber));
        }

        return new Shipment(
            orderId,
            tenantId,
            shippingMethod,
            carrier,
            trackingNumber);
    }

    public void SetTrackingNumber(
        string trackingNumber)
    {
        if (Status !=
            ShipmentStatus.Pending)
        {
            throw new InvalidOperationException(
                "Tracking number can only be changed before shipment.");
        }

        ValidateText(
            trackingNumber,
            nameof(trackingNumber),
            200);

        TrackingNumber =
            trackingNumber.Trim();

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void MarkShipped()
    {
        if (Status ==
            ShipmentStatus.Shipped)
        {
            return;
        }

        if (Status !=
            ShipmentStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending shipments can be shipped.");
        }

        if (string.IsNullOrWhiteSpace(
                TrackingNumber))
        {
            throw new InvalidOperationException(
                "A tracking number is required before shipping.");
        }

        Status =
            ShipmentStatus.Shipped;

        ShippedAt =
            DateTime.UtcNow;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        if (Status ==
            ShipmentStatus.Delivered)
        {
            return;
        }

        if (Status !=
            ShipmentStatus.Shipped)
        {
            throw new InvalidOperationException(
                "Only shipped shipments can be delivered.");
        }

        Status =
            ShipmentStatus.Delivered;

        DeliveredAt =
            DateTime.UtcNow;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is
            ShipmentStatus.Shipped or
            ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException(
                "Shipped or delivered shipments cannot be cancelled.");
        }

        Status =
            ShipmentStatus.Cancelled;

        UpdatedAt =
            DateTime.UtcNow;
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

public enum ShipmentStatus
{
    Pending = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}