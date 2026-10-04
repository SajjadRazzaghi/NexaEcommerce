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
        if (productAttributeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product attribute id is required.",
                nameof(productAttributeId));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Attribute value is required.",
                nameof(value));
        }

        ProductAttributeId =
            productAttributeId;

        Value =
            value.Trim();

        DisplayValue =
            string.IsNullOrWhiteSpace(displayValue)
                ? null
                : displayValue.Trim();

        ColorHex =
            string.IsNullOrWhiteSpace(colorHex)
                ? null
                : colorHex.Trim();
    }

    /*
     * This constructor is used when the value is created
     * through the ProductAttribute aggregate.
     *
     * Keeping the navigation populated is important because
     * variant synchronization needs to know which ProductAttribute
     * owns the value.
     */
    public AttributeValue(
        ProductAttribute productAttribute,
        string value,
        string? displayValue = null,
        string? colorHex = null)
        : this(
            productAttribute?.Id
                ?? throw new ArgumentNullException(
                    nameof(productAttribute)),
            value,
            displayValue,
            colorHex)
    {
        ProductAttribute =
            productAttribute;
    }

    public void Update(
        string value,
        string? displayValue,
        string? colorHex)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Attribute value is required.",
                nameof(value));
        }

        var normalizedValue =
            value.Trim();

        var normalizedDisplayValue =
            string.IsNullOrWhiteSpace(displayValue)
                ? null
                : displayValue.Trim();

        var normalizedColorHex =
            string.IsNullOrWhiteSpace(colorHex)
                ? null
                : colorHex.Trim();

        // Nothing really changed.
        if (string.Equals(
                Value,
                normalizedValue,
                StringComparison.Ordinal) &&
            string.Equals(
                DisplayValue,
                normalizedDisplayValue,
                StringComparison.Ordinal) &&
            string.Equals(
                ColorHex,
                normalizedColorHex,
                StringComparison.Ordinal))
        {
            return;
        }

        Value =
            normalizedValue;

        DisplayValue =
            normalizedDisplayValue;

        ColorHex =
            normalizedColorHex;

        UpdatedAt =
            DateTime.UtcNow;
    }
}