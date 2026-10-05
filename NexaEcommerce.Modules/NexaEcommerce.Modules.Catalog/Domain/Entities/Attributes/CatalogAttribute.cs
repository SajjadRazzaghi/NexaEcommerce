using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public class CatalogAttribute : BaseEntity
{
    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? DisplayType { get; private set; }

    public CatalogAttributeDataType DataType { get; private set; }

    /*
     * Legacy compatibility flag.
     *
     * Product/category-level role is now determined by
     * CategoryAttribute / ProductAttribute.Role.
     *
     * This field remains temporarily so the current application
     * continues to compile during the migration phases.
     */
    public bool IsVariantAttribute { get; private set; }

    public bool IsRequired { get; private set; }

    public bool IsFilterable { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public ICollection<CatalogAttributeValue> Values
    {
        get;
        private set;
    } = new List<CatalogAttributeValue>();

    public ICollection<CategoryAttribute> CategoryAttributes
    {
        get;
        private set;
    } = new List<CategoryAttribute>();

    private CatalogAttribute()
    {
    }

    public CatalogAttribute(
        string name,
        string code,
        string? description = null,
        string? displayType = null,
        bool isRequired = false,
        bool isFilterable = false,
        bool isVariantAttribute = false,
        bool isActive = true,
        int displayOrder = 0,
        CatalogAttributeDataType dataType =
            CatalogAttributeDataType.SingleSelect)
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

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));
        }

        Name = name.Trim();

        Code = NormalizeCode(code);

        Description = NormalizeNullable(description);

        DisplayType = NormalizeNullable(displayType);

        DataType = dataType;

        IsRequired = isRequired;

        IsFilterable = isFilterable;

        IsVariantAttribute = isVariantAttribute;

        IsActive = isActive;

        DisplayOrder = displayOrder;
    }

    public void Update(
        string name,
        string code,
        string? description,
        string? displayType,
        bool isRequired,
        bool isFilterable,
        bool isVariantAttribute,
        bool isActive,
        int displayOrder,
        CatalogAttributeDataType dataType =
            CatalogAttributeDataType.SingleSelect)
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

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));
        }

        Name = name.Trim();

        Code = NormalizeCode(code);

        Description = NormalizeNullable(description);

        DisplayType = NormalizeNullable(displayType);

        IsRequired = isRequired;

        IsFilterable = isFilterable;

        /*
         * Temporary legacy support.
         *
         * The field will no longer be consulted by Product behavior
         * once the migration reaches the Product/Service phase.
         */
        IsVariantAttribute = isVariantAttribute;

        IsActive = isActive;

        DisplayOrder = displayOrder;

        DataType = dataType;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(
        bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDataType(
        CatalogAttributeDataType dataType)
    {
        DataType = dataType;
        UpdatedAt = DateTime.UtcNow;
    }

    public CatalogAttributeValue AddValue(
        string value,
        string? displayValue = null,
        string? colorHex = null,
        int displayOrder = 0,
        bool isActive = true)
    {
        var item =
            new CatalogAttributeValue(
                Id,
                value,
                displayValue,
                colorHex,
                displayOrder,
                isActive);

        Values.Add(item);

        return item;
    }

    private static string NormalizeCode(
        string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-");
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

public enum CatalogAttributeDataType
{
    Text = 1,
    Integer = 2,
    Decimal = 3,
    Boolean = 4,
    Date = 5,
    DateTime = 6,
    SingleSelect = 7,
    MultiSelect = 8
}