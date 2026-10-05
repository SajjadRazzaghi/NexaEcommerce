using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public class AttributeValue : BaseEntity
{
    public Guid ProductAttributeId { get; private set; }

    /*
     * Optional reference to the controlled catalog value.
     *
     * Null means product-specific/custom value.
     */
    public Guid? CatalogAttributeValueId { get; private set; }

    public string Value { get; private set; } = null!;

    public string? DisplayValue { get; private set; }

    public string? ColorHex { get; private set; }

    public ProductAttribute ProductAttribute { get; private set; } = null!;

    public CatalogAttributeValue? CatalogAttributeValue
    {
        get;
        private set;
    }

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
        string? colorHex = null,
        Guid? catalogAttributeValueId = null)
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

        if (catalogAttributeValueId.HasValue &&
            catalogAttributeValueId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Catalog attribute value id cannot be empty.",
                nameof(catalogAttributeValueId));
        }

        ProductAttributeId =
            productAttributeId;

        CatalogAttributeValueId =
            catalogAttributeValueId;

        Value =
            value.Trim();

        DisplayValue =
            NormalizeNullable(displayValue);

        ColorHex =
            NormalizeNullable(colorHex);
    }

    public AttributeValue(
        ProductAttribute productAttribute,
        string value,
        string? displayValue = null,
        string? colorHex = null,
        Guid? catalogAttributeValueId = null)
        : this(
            productAttribute?.Id
                ?? throw new ArgumentNullException(
                    nameof(productAttribute)),
            value,
            displayValue,
            colorHex,
            catalogAttributeValueId)
    {
        ProductAttribute =
            productAttribute;
    }

    public void SetCatalogValue(
        Guid catalogAttributeValueId)
    {
        if (catalogAttributeValueId == Guid.Empty)
        {
            throw new ArgumentException(
                "Catalog attribute value id is required.",
                nameof(catalogAttributeValueId));
        }

        CatalogAttributeValueId =
            catalogAttributeValueId;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void ClearCatalogValue()
    {
        CatalogAttributeValueId = null;

        UpdatedAt =
            DateTime.UtcNow;
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
            NormalizeNullable(displayValue);

        var normalizedColorHex =
            NormalizeNullable(colorHex);

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

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}