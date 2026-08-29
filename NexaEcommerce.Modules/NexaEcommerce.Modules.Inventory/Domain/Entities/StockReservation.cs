using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public sealed class StockReservation : BaseEntity
{
    private StockReservation()
    {
    }

    private StockReservation(
        string tenantId,
        string reservationKey,
        Guid productVariantId,
        Guid stockItemId,
        int quantity,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException(nameof(tenantId));

        if (string.IsNullOrWhiteSpace(reservationKey))
            throw new ArgumentException(
                nameof(reservationKey));

        if (productVariantId == Guid.Empty)
            throw new ArgumentException(
                nameof(productVariantId));

        if (stockItemId == Guid.Empty)
            throw new ArgumentException(
                nameof(stockItemId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity));

        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException(
                "Reservation expiration must be in the future.",
                nameof(expiresAt));

        TenantId = tenantId.Trim();
        ReservationKey = reservationKey.Trim();
        ProductVariantId = productVariantId;
        StockItemId = stockItemId;
        Quantity = quantity;
        ExpiresAt = expiresAt;
        Status = StockReservationStatus.Active;
    }

    public string TenantId { get; private set; } = null!;

    public string ReservationKey { get; private set; } = null!;

    public Guid ProductVariantId { get; private set; }

    public Guid StockItemId { get; private set; }

    public int Quantity { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public StockReservationStatus Status { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public StockItem StockItem { get; private set; } = null!;

    public static StockReservation Create(
        string tenantId,
        string reservationKey,
        Guid productVariantId,
        Guid stockItemId,
        int quantity,
        DateTimeOffset expiresAt)
    {
        return new StockReservation(
            tenantId,
            reservationKey,
            productVariantId,
            stockItemId,
            quantity,
            expiresAt);
    }

    public bool IsActive =>
        Status == StockReservationStatus.Active;

    public bool IsExpired =>
        IsActive &&
        ExpiresAt <= DateTimeOffset.UtcNow;

    public void MarkReleased()
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "Only active reservations can be released.");

        Status = StockReservationStatus.Released;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCommitted()
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "Only active reservations can be committed.");

        Status = StockReservationStatus.Committed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkExpired()
    {
        if (!IsActive)
            return;

        Status = StockReservationStatus.Expired;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}

public enum StockReservationStatus
{
    Active = 1,
    Released = 2,
    Committed = 3,
    Expired = 4
}