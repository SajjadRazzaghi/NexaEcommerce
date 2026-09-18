using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public enum FulfillmentStatus
{
    Pending = 1,
    Picking = 2,
    Picked = 3,
    Packing = 4,
    Packed = 5,
    ReadyToShip = 6,
    Shipped = 7,
    Delivered = 8,
    Cancelled = 9
}

public sealed class Fulfillment : BaseEntity
{
    private Fulfillment()
    {
    }

    private Fulfillment(
        Guid orderId,
        string tenantId,
        FulfillmentStatus status = FulfillmentStatus.Pending)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        OrderId = orderId;
        TenantId = tenantId.Trim();
        Status = status;
    }

    public Guid OrderId { get; private set; }

    public string TenantId { get; private set; } = null!;

    public FulfillmentStatus Status { get; private set; }

    public Guid? WarehouseId { get; private set; }

    public Guid? PickingLocationId { get; private set; }

    public DateTime? PickingStartedAt { get; private set; }

    public DateTime? PickedAt { get; private set; }

    public DateTime? PackingStartedAt { get; private set; }

    public DateTime? PackedAt { get; private set; }

    public DateTime? ReadyToShipAt { get; private set; }

    public DateTime? ShippedAt { get; private set; }

    public DateTime? DeliveredAt { get; private set; }

    public static Fulfillment Create(
        Guid orderId,
        string tenantId)
    {
        return new Fulfillment(
            orderId,
            tenantId);
    }

    public void AssignWarehouse(
        Guid warehouseId,
        Guid? pickingLocationId = null)
    {
        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse id is required.",
                nameof(warehouseId));
        }

        if (PickingLocationId.HasValue &&
            PickingLocationId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Picking location id is invalid.",
                nameof(pickingLocationId));
        }

        WarehouseId = warehouseId;
        PickingLocationId = pickingLocationId;

        UpdatedAt = DateTime.UtcNow;
    }

    public void StartPicking()
    {
        EnsureStatus(
            FulfillmentStatus.Pending);

        Status = FulfillmentStatus.Picking;
        PickingStartedAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPicked()
    {
        EnsureStatus(
            FulfillmentStatus.Picking);

        Status = FulfillmentStatus.Picked;
        PickedAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
    }

    public void StartPacking()
    {
        EnsureStatus(
            FulfillmentStatus.Picked);

        Status = FulfillmentStatus.Packing;
        PackingStartedAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPacked()
    {
        EnsureStatus(
            FulfillmentStatus.Packing);

        Status = FulfillmentStatus.Packed;
        PackedAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReadyToShip()
    {
        EnsureStatus(
            FulfillmentStatus.Packed);

        Status = FulfillmentStatus.ReadyToShip;
        ReadyToShipAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkShipped()
    {
        if (Status == FulfillmentStatus.Shipped)
        {
            return;
        }

        EnsureStatus(
            FulfillmentStatus.ReadyToShip);

        Status = FulfillmentStatus.Shipped;
        ShippedAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        if (Status == FulfillmentStatus.Delivered)
        {
            return;
        }

        EnsureStatus(
            FulfillmentStatus.Shipped);

        Status = FulfillmentStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is
            FulfillmentStatus.Shipped or
            FulfillmentStatus.Delivered)
        {
            throw new InvalidOperationException(
                "Shipped or delivered fulfillment cannot be cancelled.");
        }

        Status = FulfillmentStatus.Cancelled;

        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(
        FulfillmentStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Fulfillment must be in '{expected}' status.");
        }
    }
}
