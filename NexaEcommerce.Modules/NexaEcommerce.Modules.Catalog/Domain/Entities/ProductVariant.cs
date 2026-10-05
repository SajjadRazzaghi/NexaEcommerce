using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = null!;

    public string? Barcode { get; private set; }

    /*
     * Deterministic representation of the variant-defining
     * ProductAttributeValue combination.
     *
     * Nullable during the migration phase because existing
     * variants do not have this value yet.
     */
    public string? CombinationKey { get; private set; }

    public decimal PriceOverride { get; private set; }

    public decimal? ComparePrice { get; private set; }

    /*
     * Legacy compatibility field.
     *
     * Inventory will become the only source of truth in the
     * inventory migration phase.
     */
    public int StockQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public Product Product { get; private set; } = null!;

    public ICollection<VariantAttributeValue> AttributeValues
    {
        get;
        private set;
    } = new List<VariantAttributeValue>();

    public ICollection<ProductVariantImage> Images
    {
        get;
        private set;
    } = new List<ProductVariantImage>();

    private ProductVariant()
    {
    }

    public ProductVariant(
        Guid productId,
        string sku,
        decimal price,
        int stockQuantity = 0,
        decimal? comparePrice = null)
    {
        ProductId = productId;

        Sku =
            sku?.Trim()
            ?? throw new ArgumentNullException(
                nameof(sku));

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Variant price cannot be negative.");
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantity),
                "Variant stock quantity cannot be negative.");
        }

        PriceOverride = price;

        StockQuantity =
            stockQuantity;

        ComparePrice =
            comparePrice;

        IsActive =
            true;

        AttributeValues =
            new List<VariantAttributeValue>();

        Images =
            new List<ProductVariantImage>();
    }

    public void ChangeSku(
        string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException(
                "SKU is required.",
                nameof(sku));
        }

        Sku = sku.Trim();

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void SetBarcode(
        string? barcode)
    {
        Barcode =
            NormalizeNullable(barcode);

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void SetCombinationKey(
        string? combinationKey)
    {
        CombinationKey =
            NormalizeNullable(combinationKey);

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void ChangePrice(
        decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price));
        }

        PriceOverride = price;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void SetComparePrice(
        decimal? comparePrice)
    {
        if (comparePrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(comparePrice));
        }

        ComparePrice = comparePrice;

        UpdatedAt =
            DateTime.UtcNow;
    }

    /*
     * Legacy only.
     *
     * New Inventory code must never use this method.
     */
    public void ChangeStock(
        int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        StockQuantity = quantity;

        UpdatedAt =
            DateTime.UtcNow;
    }

    /*
     * Legacy only.
     */
    public void IncreaseStock(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        checked
        {
            StockQuantity += quantity;
        }

        UpdatedAt =
            DateTime.UtcNow;
    }

    /*
     * Legacy only.
     */
    public void DecreaseStock(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        if (quantity > StockQuantity)
        {
            throw new InvalidOperationException(
                "Insufficient stock.");
        }

        StockQuantity -= quantity;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void SetActive(
        bool isActive)
    {
        IsActive = isActive;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void AddAttributeValue(
        AttributeValue attributeValue)
    {
        ArgumentNullException.ThrowIfNull(
            attributeValue);

        if (attributeValue.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "Attribute value id is required.",
                nameof(attributeValue));
        }

        if (AttributeValues.Any(
                x =>
                    x.AttributeValueId ==
                    attributeValue.Id))
        {
            return;
        }

        AttributeValues.Add(
            new VariantAttributeValue(
                Id,
                attributeValue));
    }

    public void RemoveAttributeValue(
        Guid attributeValueId)
    {
        if (attributeValueId == Guid.Empty)
        {
            return;
        }

        var existing =
            AttributeValues.FirstOrDefault(
                x =>
                    x.AttributeValueId ==
                    attributeValueId);

        if (existing is not null)
        {
            AttributeValues.Remove(
                existing);
        }
    }

    public void ReplaceAttributeValues(
        IEnumerable<AttributeValue> attributeValues)
    {
        ArgumentNullException.ThrowIfNull(
            attributeValues);

        var desiredValues =
            attributeValues
                .Where(
                    value =>
                        value is not null &&
                        value.Id != Guid.Empty)
                .GroupBy(
                    value =>
                        value.Id)
                .Select(
                    group =>
                        group.First())
                .ToList();

        var desiredIds =
            desiredValues
                .Select(
                    value =>
                        value.Id)
                .ToHashSet();

        var existingMappings =
            AttributeValues.ToList();

        foreach (var mapping in existingMappings)
        {
            if (!desiredIds.Contains(
                    mapping.AttributeValueId))
            {
                AttributeValues.Remove(
                    mapping);
            }
        }

        var existingIds =
            AttributeValues
                .Select(
                    mapping =>
                        mapping.AttributeValueId)
                .ToHashSet();

        foreach (var attributeValue in desiredValues)
        {
            if (existingIds.Contains(
                    attributeValue.Id))
            {
                continue;
            }

            AttributeValues.Add(
                new VariantAttributeValue(
                    Id,
                    attributeValue));
        }

        UpdatedAt =
            DateTime.UtcNow;
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}