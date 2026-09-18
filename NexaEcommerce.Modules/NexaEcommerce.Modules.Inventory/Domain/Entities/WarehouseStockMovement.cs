using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public enum WarehouseStockMovementType
{
    OpeningBalance = 1,
    Purchase = 2,
    Receive = 3,
    AdjustmentIncrease = 4,
    AdjustmentDecrease = 5,
    Damage = 6,
    TransferOut = 7,
    TransferIn = 8,
    Reservation = 9,
    ReservationRelease = 10,
    Sale = 11,
    Return = 12,
    Correction = 13
}

public sealed class WarehouseStockMovement : BaseEntity
{
    private WarehouseStockMovement()
    {
    }

    private WarehouseStockMovement(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        WarehouseStockMovementType type,
        int quantityDelta,
        int balanceAfter,
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

        if (quantityDelta == 0)
        {
            throw new ArgumentException(
                "Movement quantity cannot be zero.",
                nameof(quantityDelta));
        }

        if (balanceAfter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(balanceAfter));
        }

        TenantId = tenantId.Trim();

        WarehouseId = warehouseId;
        LocationId = locationId;
        ProductVariantId = productVariantId;

        Type = type;

        QuantityDelta = quantityDelta;
        BalanceAfter = balanceAfter;

        ReferenceType = NormalizeOptional(referenceType);
        ReferenceId = NormalizeOptional(referenceId);
        Reason = NormalizeOptional(reason);

        OccurredAt = DateTime.UtcNow;
    }

    public string TenantId { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }

    public Guid LocationId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public WarehouseStockMovementType Type { get; private set; }

    public int QuantityDelta { get; private set; }

    public int BalanceAfter { get; private set; }

    public string? ReferenceType { get; private set; }

    public string? ReferenceId { get; private set; }

    public string? Reason { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public static WarehouseStockMovement Create(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        WarehouseStockMovementType type,
        int quantityDelta,
        int balanceAfter,
        string? referenceType = null,
        string? referenceId = null,
        string? reason = null)
    {
        return new WarehouseStockMovement(
            tenantId,
            warehouseId,
            locationId,
            productVariantId,
            type,
            quantityDelta,
            balanceAfter,
            referenceType,
            referenceId,
            reason);
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
