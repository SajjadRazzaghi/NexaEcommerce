using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public class ProductAttribute : BaseEntity
{
    public Guid ProductId { get; private set; }

    /*
     * New architecture:
     *
     * ProductAttribute points back to the catalog definition.
     *
     * Null is temporarily allowed for existing legacy data.
     */
    public Guid? CatalogAttributeId { get; private set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    /*
     * Final role used by the product.
     *
     * This can override the category default.
     */
    public AttributeRole Role { get; private set; }

    public bool IsRequired { get; private set; }

    public int DisplayOrder { get; private set; }

    public Product Product { get; private set; } = null!;

    public CatalogAttribute? CatalogAttribute { get; private set; }

    public ICollection<AttributeValue> Values
    {
        get;
        private set;
    } = new List<AttributeValue>();

    private ProductAttribute()
    {
    }

    public ProductAttribute(
        Guid productId,
        string name,
        string code,
        Guid? catalogAttributeId = null,
        AttributeRole role = AttributeRole.Descriptive,
        bool isRequired = false,
        int displayOrder = 0)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product id is required.",
                nameof(productId));
        }

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

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));
        }

        if (role == AttributeRole.None)
        {
            throw new ArgumentException(
                "Attribute role is required.",
                nameof(role));
        }

        ProductId = productId;

        CatalogAttributeId = catalogAttributeId;

        Name = name.Trim();

        Code = code.Trim();

        Role = role;

        IsRequired = isRequired;

        DisplayOrder = displayOrder;
    }

    /*
     * Legacy constructor.
     *
     * Existing ProductService code can continue compiling.
     */
    public ProductAttribute(
        Guid productId,
        string name,
        string code)
        : this(
            productId,
            name,
            code,
            null,
            AttributeRole.Descriptive,
            false,
            0)
    {
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

        var normalizedName =
            name.Trim();

        var normalizedCode =
            code.Trim();

        if (string.Equals(
                Name,
                normalizedName,
                StringComparison.Ordinal) &&
            string.Equals(
                Code,
                normalizedCode,
                StringComparison.Ordinal))
        {
            return;
        }

        Name = normalizedName;

        Code = normalizedCode;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCatalogAttribute(
        Guid catalogAttributeId)
    {
        if (catalogAttributeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Catalog attribute id is required.",
                nameof(catalogAttributeId));
        }

        CatalogAttributeId = catalogAttributeId;

        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearCatalogAttribute()
    {
        CatalogAttributeId = null;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRole(
        AttributeRole role)
    {
        if (role == AttributeRole.None)
        {
            throw new ArgumentException(
                "Attribute role is required.",
                nameof(role));
        }

        Role = role;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRequired(
        bool isRequired)
    {
        IsRequired = isRequired;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDisplayOrder(
        int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));
        }

        DisplayOrder = displayOrder;

        UpdatedAt = DateTime.UtcNow;
    }

    public AttributeValue AddValue(
        string value,
        string? displayValue = null,
        string? colorHex = null,
        Guid? catalogAttributeValueId = null)
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
                colorHex,
                catalogAttributeValueId);

        Values.Add(attributeValue);

        return attributeValue;
    }

    public void ClearValues()
    {
        Values.Clear();
    }
}