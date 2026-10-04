using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = null!;

    public decimal PriceOverride { get; private set; }

    public decimal? ComparePrice { get; private set; }

    /*
     * Legacy catalog stock field.
     *
     * Physical/sellable inventory is managed by the Inventory module.
     * This value is retained for compatibility with the existing model.
     */
    public int StockQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public Product Product { get; private set; } = null!;

    public ICollection<VariantAttributeValue> AttributeValues
    {
        get;
        private set;
    } = new List<VariantAttributeValue>();

    private ProductVariant()
    {
    }

    public ProductVariant(
        Guid productId,
        string sku,
        decimal price,
        int stockQuantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product id is required.",
                nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException(
                "SKU is required.",
                nameof(sku));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price));
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantity));
        }

        ProductId = productId;
        Sku = sku.Trim();
        PriceOverride = price;
        StockQuantity = stockQuantity;
        IsActive = true;
    }

    public void ChangeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException(
                "SKU is required.",
                nameof(sku));
        }

        Sku = sku.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price));
        }

        PriceOverride = price;
        UpdatedAt = DateTime.UtcNow;
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
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        StockQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncreaseStock(int quantity)
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

        UpdatedAt = DateTime.UtcNow;
    }

    public void DecreaseStock(int quantity)
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
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
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

        /*
         * Remove mappings which are no longer required.
         *
         * We intentionally do not Clear() the collection because
         * EF Core can interpret a required relationship being
         * severed as a conceptual-null/orphan operation.
         */
        var existingMappings =
            AttributeValues.ToList();

        foreach (
            var mapping in existingMappings)
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

        foreach (
            var attributeValue
            in desiredValues)
        {
            if (existingIds.Contains(
                    attributeValue.Id))
            {
                continue;
            }

            /*
             * Populate both FK and navigation.
             */
            AttributeValues.Add(
                new VariantAttributeValue(
                    Id,
                    attributeValue));
        }

        UpdatedAt = DateTime.UtcNow;
    }
}