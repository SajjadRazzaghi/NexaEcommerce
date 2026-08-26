using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public class AttributeValue : BaseEntity
{
    public Guid ProductAttributeId { get; private set; }

    public string Value { get; private set; } = null!;

    public string? DisplayValue { get; private set; }

    public string? ColorHex { get; private set; }

    public ProductAttribute ProductAttribute { get; private set; } = null!;

    public ICollection<VariantAttributeValue> VariantAttributeValues
    {
        get;
        private set;
    } = new List<VariantAttributeValue>();

    private AttributeValue()
    {
    }

    public AttributeValue(
        Guid productAttributeId,
        string value,
        string? displayValue = null,
        string? colorHex = null)
    {
        ProductAttributeId = productAttributeId;
        Value = value;
        DisplayValue = displayValue;
        ColorHex = colorHex;
    }

    public void Update(
        string value,
        string? displayValue,
        string? colorHex)
    {
        Value = value;
        DisplayValue = displayValue;
        ColorHex = colorHex;
    }
}