namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class OrderInventoryReservation
{
    private OrderInventoryReservation()
    {
    }

    private OrderInventoryReservation(
        Guid orderId,
        string tenantId,
        string reservationKey,
        Guid productVariantId,
        int quantity,
        DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();

        OrderId = orderId;
        TenantId = tenantId;
        ReservationKey = reservationKey;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        Status = InventoryReservationStatus.Reserved;
        ExpiresAt = expiresAt;

        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string TenantId { get; private set; } = null!;

    public string ReservationKey { get; private set; } = null!;

    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    public InventoryReservationStatus Status { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static OrderInventoryReservation Create(
        Guid orderId,
        string tenantId,
        string reservationKey,
        Guid productVariantId,
        int quantity,
        DateTimeOffset expiresAt)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                nameof(orderId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(reservationKey))
        {
            throw new ArgumentException(
                nameof(reservationKey));
        }

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                nameof(productVariantId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(
                "Reservation expiration must be in the future.",
                nameof(expiresAt));
        }

        return new OrderInventoryReservation(
            orderId,
            tenantId.Trim(),
            reservationKey.Trim(),
            productVariantId,
            quantity,
            expiresAt);
    }

    public void MarkCommitted()
    {
        if (Status == InventoryReservationStatus.Committed)
        {
            return;
        }

        if (Status != InventoryReservationStatus.Reserved)
        {
            throw new InvalidOperationException(
                "Only reserved inventory can be committed.");
        }

        Status =
            InventoryReservationStatus.Committed;

        CompletedAt =
            DateTimeOffset.UtcNow;
    }

    public void MarkReleased()
    {
        if (Status == InventoryReservationStatus.Released)
        {
            return;
        }

        if (Status == InventoryReservationStatus.Committed)
        {
            throw new InvalidOperationException(
                "Committed inventory cannot be released.");
        }

        Status =
            InventoryReservationStatus.Released;

        CompletedAt =
            DateTimeOffset.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status != InventoryReservationStatus.Reserved)
        {
            return;
        }

        Status =
            InventoryReservationStatus.Expired;

        CompletedAt =
            DateTimeOffset.UtcNow;
    }
}

public enum InventoryReservationStatus
{
    Reserved = 1,
    Committed = 2,
    Released = 3,
    Expired = 4
}