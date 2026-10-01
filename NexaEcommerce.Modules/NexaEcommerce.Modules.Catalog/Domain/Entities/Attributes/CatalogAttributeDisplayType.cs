using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public sealed class CatalogAttributeDisplayType : BaseEntity
{
    public string Code { get; private set; } = null!;

    public string NameEn { get; private set; } = null!;

    public string NameFa { get; private set; } = null!;

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    private CatalogAttributeDisplayType()
    {
    }

    public CatalogAttributeDisplayType(
        string code,
        string nameEn,
        string nameFa,
        int displayOrder = 0,
        bool isActive = true)
    {
        Code = code;
        NameEn = nameEn;
        NameFa = nameFa;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public void Update(
        string code,
        string nameEn,
        string nameFa,
        int displayOrder,
        bool isActive)
    {
        Code = code;
        NameEn = nameEn;
        NameFa = nameFa;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }
}