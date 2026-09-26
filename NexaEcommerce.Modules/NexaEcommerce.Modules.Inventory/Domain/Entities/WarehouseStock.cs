using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public sealed class WarehouseStock : BaseEntity
{
    private WarehouseStock()
    {
    }

    private WarehouseStock(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        int onHandQuantity,
        int reservedQuantity,
        int incomingQuantity,
        int damagedQuantity,
        int reorderPoint)
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

        ValidateNonNegative(
            onHandQuantity,
            nameof(onHandQuantity));

        ValidateNonNegative(
            reservedQuantity,
            nameof(reservedQuantity));

        ValidateNonNegative(
            incomingQuantity,
            nameof(incomingQuantity));

        ValidateNonNegative(
            damagedQuantity,
            nameof(damagedQuantity));

        ValidateNonNegative(
            reorderPoint,
            nameof(reorderPoint));

        if (reservedQuantity > onHandQuantity)
        {
            throw new ArgumentException(
                "Reserved quantity cannot exceed on-hand quantity.",
                nameof(reservedQuantity));
        }

        TenantId = tenantId.Trim();

        WarehouseId = warehouseId;
        LocationId = locationId;
        ProductVariantId = productVariantId;

        OnHandQuantity = onHandQuantity;
        ReservedQuantity = reservedQuantity;
        IncomingQuantity = incomingQuantity;
        DamagedQuantity = damagedQuantity;
        ReorderPoint = reorderPoint;

        Version = 1;
    }

    public string TenantId { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }

    public Guid LocationId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public int OnHandQuantity { get; private set; }

    public int ReservedQuantity { get; private set; }

    public int IncomingQuantity { get; private set; }

    public int DamagedQuantity { get; private set; }

    public int ReorderPoint { get; private set; }

    public int Version { get; private set; }

    public int AvailableQuantity =>
        OnHandQuantity - ReservedQuantity;

    public bool IsLowStock =>
        AvailableQuantity <= ReorderPoint;

    public static WarehouseStock Create(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        int onHandQuantity = 0,
        int reservedQuantity = 0,
        int incomingQuantity = 0,
        int damagedQuantity = 0,
        int reorderPoint = 0)
    {
        return new WarehouseStock(
            tenantId,
            warehouseId,
            locationId,
            productVariantId,
            onHandQuantity,
            reservedQuantity,
            incomingQuantity,
            damagedQuantity,
            reorderPoint);
    }

    public void SetQuantities(
        int onHandQuantity,
        int reservedQuantity,
        int incomingQuantity,
        int damagedQuantity)
    {
        ValidateNonNegative(
            onHandQuantity,
            nameof(onHandQuantity));

        ValidateNonNegative(
            reservedQuantity,
            nameof(reservedQuantity));

        ValidateNonNegative(
            incomingQuantity,
            nameof(incomingQuantity));

        ValidateNonNegative(
            damagedQuantity,
            nameof(damagedQuantity));

        if (reservedQuantity > onHandQuantity)
        {
            throw new InvalidOperationException(
                "Reserved quantity cannot exceed on-hand quantity.");
        }

        OnHandQuantity = onHandQuantity;
        ReservedQuantity = reservedQuantity;
        IncomingQuantity = incomingQuantity;
        DamagedQuantity = damagedQuantity;

        Touch();
    }

    public void AddOnHand(int quantity)
    {
        ValidatePositive(quantity);

        checked
        {
            OnHandQuantity += quantity;
        }

        Touch();
    }

    public void RemoveOnHand(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException(
                "Insufficient available warehouse stock.");
        }

        OnHandQuantity -= quantity;

        Touch();
    }

    public void Reserve(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException(
                "Insufficient available warehouse stock.");
        }

        checked
        {
            ReservedQuantity += quantity;
        }

        Touch();
    }

    public void Release(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Cannot release more than reserved warehouse stock.");
        }

        ReservedQuantity -= quantity;

        Touch();
    }

    public void ConsumeReserved(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Cannot consume more than reserved warehouse stock.");
        }

        if (quantity > OnHandQuantity)
        {
            throw new InvalidOperationException(
                "Cannot consume more than on-hand warehouse stock.");
        }

        OnHandQuantity -= quantity;
        ReservedQuantity -= quantity;

        Touch();
    }
    public void RestoreConsumed(
    int quantity)
    {
        ValidatePositive(quantity);

        checked
        {
            OnHandQuantity += quantity;
            ReservedQuantity += quantity;
        }

        if (ReservedQuantity > OnHandQuantity)
        {
            throw new InvalidOperationException(
                "Reserved quantity cannot exceed on-hand quantity.");
        }

        Touch();
    }
    public void AddIncoming(int quantity)
    {
        ValidatePositive(quantity);

        checked
        {
            IncomingQuantity += quantity;
        }

        Touch();
    }

    public void ReceiveIncoming(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > IncomingQuantity)
        {
            throw new InvalidOperationException(
                "Cannot receive more than incoming quantity.");
        }

        checked
        {
            IncomingQuantity -= quantity;
            OnHandQuantity += quantity;
        }

        Touch();
    }

    public void AddDamage(int quantity)
    {
        ValidatePositive(quantity);

        checked
        {
            DamagedQuantity += quantity;
        }

        Touch();
    }

    public void SetReorderPoint(int quantity)
    {
        ValidateNonNegative(
            quantity,
            nameof(quantity));

        ReorderPoint = quantity;

        Touch();
    }

    private void Touch()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidatePositive(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }
    }

    private static void ValidateNonNegative(
        int quantity,
        string parameterName)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName);
        }
    }
}