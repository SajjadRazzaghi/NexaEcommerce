using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public class CatalogAttribute : BaseEntity
{
    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? DisplayType { get; private set; }

    public bool IsRequired { get; private set; }

    public bool IsFilterable { get; private set; }

    public bool IsVariantAttribute { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public ICollection<CatalogAttributeValue> Values { get; private set; }
        = new List<CatalogAttributeValue>();

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
        int displayOrder = 0)
    {
        Name = name;
        Code = code;
        Description = description;
        DisplayType = displayType;
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
        int displayOrder)
    {
        Name = name;
        Code = code;
        Description = description;
        DisplayType = displayType;
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        IsVariantAttribute = isVariantAttribute;
        IsActive = isActive;
        DisplayOrder = displayOrder;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public CatalogAttributeValue AddValue(
        string value,
        string? displayValue = null,
        string? colorHex = null,
        int displayOrder = 0,
        bool isActive = true)
    {
        var item = new CatalogAttributeValue(
            Id,
            value,
            displayValue,
            colorHex,
            displayOrder,
            isActive);

        Values.Add(item);

        return item;
    }
}