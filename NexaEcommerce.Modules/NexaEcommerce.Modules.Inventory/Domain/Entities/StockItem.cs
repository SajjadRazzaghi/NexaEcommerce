using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public sealed class StockItem : AggregateRoot
{
    private StockItem()
    {
    }

    private StockItem(
        string tenantId,
        Guid productVariantId,
        int availableQuantity)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException(nameof(tenantId));

        if (productVariantId == Guid.Empty)
            throw new ArgumentException(
                nameof(productVariantId));

        if (availableQuantity < 0)
            throw new ArgumentOutOfRangeException(
                nameof(availableQuantity));

        TenantId = tenantId.Trim();
        ProductVariantId = productVariantId;
        AvailableQuantity = availableQuantity;
        ReservedQuantity = 0;
        Version = 1;
    }

    public string TenantId { get; private set; } = null!;

    public Guid ProductVariantId { get; private set; }

    public int AvailableQuantity { get; private set; }

    public int ReservedQuantity { get; private set; }

    public int Version { get; private set; }

    public int TotalQuantity =>
        AvailableQuantity + ReservedQuantity;

    public static StockItem Create(
        string tenantId,
        Guid productVariantId,
        int availableQuantity = 0)
    {
        return new StockItem(
            tenantId,
            productVariantId,
            availableQuantity);
    }

    public void Add(int quantity)
    {
        ValidatePositive(quantity);

        checked
        {
            AvailableQuantity += quantity;
        }

        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Remove(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > AvailableQuantity)
            throw new InvalidOperationException(
                "Insufficient available stock.");

        AvailableQuantity -= quantity;

        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reserve(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException(
                "Insufficient available stock.");
        }

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;

        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Cannot release more than reserved stock.");
        }

        ReservedQuantity -= quantity;
        AvailableQuantity += quantity;

        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Commit(int quantity)
    {
        ValidatePositive(quantity);

        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Cannot commit more than reserved stock.");
        }

        ReservedQuantity -= quantity;

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
}