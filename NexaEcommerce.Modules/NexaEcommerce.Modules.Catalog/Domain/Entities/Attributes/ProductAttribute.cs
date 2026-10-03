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
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Attribute name is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Attribute code is required.",
                nameof(code));
        }

        ProductId = productId;
        Name = name.Trim();
        Code = code.Trim();
    }

    public void Update(
        string name,
        string code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Attribute name is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Attribute code is required.",
                nameof(code));
        }

        Name = name.Trim();
        Code = code.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    public AttributeValue AddValue(
        string value,
        string? displayValue = null,
        string? colorHex = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Attribute value is required.",
                nameof(value));
        }

        var attributeValue =
            new AttributeValue(
                this,
                value.Trim(),
                displayValue,
                colorHex);

        Values.Add(
            attributeValue);

        return attributeValue;
    }

   public void ClearValues()
{
    Values.Clear();
}
}