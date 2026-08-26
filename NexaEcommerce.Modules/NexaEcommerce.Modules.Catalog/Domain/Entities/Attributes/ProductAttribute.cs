using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public class ProductAttribute : BaseEntity
{
    public Guid ProductId { get; private set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public Product Product { get; private set; } = null!;

    public ICollection<AttributeValue> Values { get; private set; }
        = new List<AttributeValue>();

    private ProductAttribute()
    {
    }

    public ProductAttribute(
        Guid productId,
        string name,
        string code)
    {
        ProductId = productId;
        Name = name;
        Code = code;
    }

    public void Update(
        string name,
        string code)
    {
        Name = name;
        Code = code;
    }

    public AttributeValue AddValue(
        string value,
        string? displayValue = null,
        string? colorHex = null)
    {
        var attributeValue = new AttributeValue(
            Id,
            value,
            displayValue,
            colorHex);

        Values.Add(attributeValue);

        return attributeValue;
    }
}