namespace NexaEcommerce.Modules.Catalog.Application.CatalogAttributes.DTOs;

public sealed record CatalogAttributeDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? DisplayType,
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAttribute,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyCollection<CatalogAttributeValueDto> Values
);

public sealed record CatalogAttributeValueDto(
    Guid Id,
    Guid CatalogAttributeId,
    string Value,
    string? DisplayValue,
    string? ColorHex,
    int DisplayOrder,
    bool IsActive
);

public sealed record CreateCatalogAttributeDto(
    string Name,
    string Code,
    string? Description,
    string? DisplayType,
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAttribute,
    bool IsActive,
    int DisplayOrder
);

public sealed record UpdateCatalogAttributeDto(
    string Name,
    string Code,
    string? Description,
    string? DisplayType,
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAttribute,
    bool IsActive,
    int DisplayOrder
);

public sealed record CreateCatalogAttributeValueDto(
    string Value,
    string? DisplayValue,
    string? ColorHex,
    int DisplayOrder,
    bool IsActive
);

public sealed record UpdateCatalogAttributeValueDto(
    string Value,
    string? DisplayValue,
    string? ColorHex,
    int DisplayOrder,
    bool IsActive
);