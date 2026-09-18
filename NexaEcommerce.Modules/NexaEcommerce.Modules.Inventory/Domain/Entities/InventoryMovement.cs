using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public sealed class InventoryMovement : BaseEntity
{
    private InventoryMovement()
    {
    }

    private InventoryMovement(
        string tenantId,
        Guid stockItemId,
        Guid productVariantId,
        InventoryMovementType type,
        int availableDelta,
        int reservedDelta,
        int availableBalance,
        int reservedBalance,
        string? referenceType,
        string? referenceId,
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (stockItemId == Guid.Empty)
        {
            throw new ArgumentException(
                "Stock item id is required.",
                nameof(stockItemId));
        }

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        if (availableBalance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableBalance));
        }

        if (reservedBalance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reservedBalance));
        }

        TenantId = tenantId.Trim();
        StockItemId = stockItemId;
        ProductVariantId = productVariantId;
        Type = type;
        AvailableDelta = availableDelta;
        ReservedDelta = reservedDelta;
        AvailableBalance = availableBalance;
        ReservedBalance = reservedBalance;
        ReferenceType = Normalize(referenceType);
        ReferenceId = Normalize(referenceId);
        Reason = Normalize(reason);
        OccurredAt = DateTime.UtcNow;
    }

    public string TenantId { get; private set; } = null!;

    public Guid StockItemId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public InventoryMovementType Type { get; private set; }

    public int AvailableDelta { get; private set; }

    public int ReservedDelta { get; private set; }

    public int AvailableBalance { get; private set; }

    public int ReservedBalance { get; private set; }

    public int TotalBalance =>
        AvailableBalance + ReservedBalance;

    public string? ReferenceType { get; private set; }

    public string? ReferenceId { get; private set; }

    public string? Reason { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public static InventoryMovement Create(
        string tenantId,
        Guid stockItemId,
        Guid productVariantId,
        InventoryMovementType type,
        int availableDelta,
        int reservedDelta,
        int availableBalance,
        int reservedBalance,
        string? referenceType = null,
        string? referenceId = null,
        string? reason = null)
    {
        return new InventoryMovement(
            tenantId,
            stockItemId,
            productVariantId,
            type,
            availableDelta,
            reservedDelta,
            availableBalance,
            reservedBalance,
            referenceType,
            referenceId,
            reason);
    }

    private static string? Normalize(
        string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}

public enum InventoryMovementType
{
    OpeningBalance = 1,
    Purchase = 2,
    Sale = 3,
    Reservation = 4,
    ReservationRelease = 5,
    ReservationCommit = 6,
    AdjustmentIncrease = 7,
    AdjustmentDecrease = 8,
    Return = 9,
    Damage = 10,
    Correction = 11
}
