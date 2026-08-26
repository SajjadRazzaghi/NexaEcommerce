using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public class CatalogAttributeValue : BaseEntity
{
    public Guid CatalogAttributeId { get; private set; }

    public string Value { get; private set; } = null!;

    public string? DisplayValue { get; private set; }

    public string? ColorHex { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public CatalogAttribute CatalogAttribute { get; private set; } = null!;

    private CatalogAttributeValue()
    {
    }

    public CatalogAttributeValue(
        Guid catalogAttributeId,
        string value,
        string? displayValue = null,
        string? colorHex = null,
        int displayOrder = 0,
        bool isActive = true)
    {
        CatalogAttributeId = catalogAttributeId;
        Value = value;
        DisplayValue = displayValue;
        ColorHex = colorHex;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public void Update(
        string value,
        string? displayValue,
        string? colorHex,
        int displayOrder,
        bool isActive)
    {
        Value = value;
        DisplayValue = displayValue;
        ColorHex = colorHex;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}