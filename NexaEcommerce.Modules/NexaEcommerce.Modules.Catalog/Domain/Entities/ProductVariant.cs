using NexaEcommerce.SharedKernel.Domain;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = null!;

    public decimal PriceOverride { get; private set; }

    public decimal? ComparePrice { get; private set; }

    public int StockQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public Product Product { get; private set; } = null!;

    public ICollection<VariantAttributeValue> AttributeValues { get; private set; }
        = new List<VariantAttributeValue>();

    private ProductVariant()
    {
    }

    public ProductVariant(
        Guid productId,
        string sku,
        decimal price,
        int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity));

        ProductId = productId;
        Sku = sku;
        PriceOverride = price;
        StockQuantity = stockQuantity;
        IsActive = true;
    }

    public void ChangeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException(
                "SKU is required.",
                nameof(sku));

        Sku = sku;
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        PriceOverride = price;
    }

    public void SetComparePrice(decimal? comparePrice)
    {
        if (comparePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(comparePrice));

        ComparePrice = comparePrice;
    }

    public void ChangeStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        StockQuantity = quantity;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        StockQuantity += quantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (quantity > StockQuantity)
            throw new InvalidOperationException(
                "Insufficient stock.");

        StockQuantity -= quantity;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void AddAttributeValue(AttributeValue attributeValue)
    {
        ArgumentNullException.ThrowIfNull(attributeValue);

        if (AttributeValues.Any(x =>
            x.AttributeValueId == attributeValue.Id))
        {
            return;
        }

        AttributeValues.Add(
            new VariantAttributeValue(
                Id,
                attributeValue.Id));
    }

    public void RemoveAttributeValue(Guid attributeValueId)
    {
        var existing = AttributeValues.FirstOrDefault(
            x => x.AttributeValueId == attributeValueId);

        if (existing is not null)
        {
            AttributeValues.Remove(existing);
        }
    }
}