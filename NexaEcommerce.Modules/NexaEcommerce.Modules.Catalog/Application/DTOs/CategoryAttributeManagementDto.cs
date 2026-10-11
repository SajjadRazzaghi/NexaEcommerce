
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed record CategoryAttributeConfigurationDto(
    Guid Id,
    Guid CategoryId,
    Guid CatalogAttributeId,
    string Name,
    string Code,
    CatalogAttributeDataType DataType,
    string? DisplayType,
    AttributeRole Role,
    bool IsRequired,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyCollection<CategoryAttributeValueOptionDto> Values
);

public sealed record CategoryAttributeValueOptionDto(
    Guid Id,
    string Value,
    string? DisplayValue,
    string? ColorHex,
    int DisplayOrder,
    bool IsActive
);

public sealed record ReplaceCategoryAttributesDto(
    List<CategoryAttributeAssignmentInputDto> Attributes
);

public sealed record CategoryAttributeAssignmentInputDto(
    Guid CatalogAttributeId,
    AttributeRole Role,
    bool IsRequired,
    int DisplayOrder,
    bool IsActive = true
);
