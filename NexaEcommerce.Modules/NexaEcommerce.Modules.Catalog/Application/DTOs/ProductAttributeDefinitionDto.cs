using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;


namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class ProductAttributeDefinitionDto
{
    public Guid CatalogAttributeId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public CatalogAttributeDataType DataType { get; init; }

    public AttributeRole DefaultRole { get; init; }

    public bool IsRequired { get; init; }

    public bool IsFilterable { get; init; }

    public string? DisplayType { get; init; }

    public int DisplayOrder { get; init; }

    public List<ProductAttributeDefinitionValueDto> Values { get; init; } = [];
}

public sealed class ProductAttributeDefinitionValueDto
{
    public Guid CatalogAttributeValueId { get; init; }

    public string Value { get; init; } = string.Empty;

    public string? DisplayValue { get; init; }

    public string? ColorHex { get; init; }

    public int DisplayOrder { get; init; }
}