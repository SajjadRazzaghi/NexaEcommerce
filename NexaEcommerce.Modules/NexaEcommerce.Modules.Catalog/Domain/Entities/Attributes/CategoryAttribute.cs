using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public sealed class CategoryAttribute : BaseEntity
{
    public Guid CategoryId { get; private set; }

    public Guid CatalogAttributeId { get; private set; }

    public AttributeRole Role { get; private set; }

    public bool IsRequired { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public Category Category { get; private set; } = null!;

    public CatalogAttribute CatalogAttribute { get; private set; } = null!;

    private CategoryAttribute()
    {
    }

    public CategoryAttribute(
        Guid categoryId,
        Guid catalogAttributeId,
        AttributeRole role,
        bool isRequired = false,
        int displayOrder = 0,
        bool isActive = true)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category id is required.",
                nameof(categoryId));
        }

        if (catalogAttributeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Catalog attribute id is required.",
                nameof(catalogAttributeId));
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

        CategoryId = categoryId;
        CatalogAttributeId = catalogAttributeId;
        Role = role;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public void Update(
        AttributeRole role,
        bool isRequired,
        int displayOrder,
        bool isActive)
    {
        if (role == AttributeRole.None)
        {
            throw new ArgumentException(
                "Attribute role is required.",
                nameof(role));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));
        }

        Role = role;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
        IsActive = isActive;

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

    public void SetActive(
        bool isActive)
    {
        IsActive = isActive;

        UpdatedAt = DateTime.UtcNow;
    }
}