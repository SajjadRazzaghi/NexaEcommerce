namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public sealed class WarehouseStockReservation
{
    private WarehouseStockReservation()
    {
    }

    private WarehouseStockReservation(
        Guid id,
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        Guid orderId,
        int quantity,
        DateTimeOffset expiresAt)
    {
        Id = id;
        TenantId = tenantId;
        WarehouseId = warehouseId;
        LocationId = locationId;
        ProductVariantId = productVariantId;
        OrderId = orderId;
        Quantity = quantity;
        Status = WarehouseStockReservationStatus.Pending;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string TenantId { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }

    public Guid LocationId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public Guid OrderId { get; private set; }

    public int Quantity { get; private set; }

    public WarehouseStockReservationStatus Status { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public DateTimeOffset? CommittedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static WarehouseStockReservation Create(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        Guid orderId,
        int quantity,
        TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse id is required.",
                nameof(warehouseId));
        }

        if (locationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Location id is required.",
                nameof(locationId));
        }

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration));
        }

        return new WarehouseStockReservation(
            Guid.NewGuid(),
            tenantId.Trim(),
            warehouseId,
            locationId,
            productVariantId,
            orderId,
            quantity,
            DateTimeOffset.UtcNow.Add(duration));
    }

    public bool IsExpired(
        DateTimeOffset utcNow)
    {
        return Status ==
                   WarehouseStockReservationStatus.Pending &&
               ExpiresAt <= utcNow;
    }

    public void MarkReleased()
    {
        EnsurePending();

        Status =
            WarehouseStockReservationStatus.Released;

        ReleasedAt =
            DateTimeOffset.UtcNow;

        UpdatedAt =
            DateTimeOffset.UtcNow;
    }

    public void MarkExpired()
    {
        EnsurePending();

        Status =
            WarehouseStockReservationStatus.Expired;

        ReleasedAt =
            DateTimeOffset.UtcNow;

        UpdatedAt =
            DateTimeOffset.UtcNow;
    }

    public void MarkCommitted()
    {
        EnsurePending();

        Status =
            WarehouseStockReservationStatus.Committed;

        CommittedAt =
            DateTimeOffset.UtcNow;

        UpdatedAt =
            DateTimeOffset.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status !=
            WarehouseStockReservationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Reservation is already {Status}.");
        }
    }
}