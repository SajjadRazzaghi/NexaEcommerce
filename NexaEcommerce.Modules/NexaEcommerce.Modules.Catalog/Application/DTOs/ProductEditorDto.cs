using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;


namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class ProductEditorDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ShortDescription { get; init; }

    public decimal Price { get; init; }

    public decimal? ComparePrice { get; init; }

    public decimal? DiscountPercentage { get; init; }

    public string Currency { get; init; } = "IRR";

    public bool IsActive { get; init; }

    public bool IsFeatured { get; init; }

    public bool IsPublished { get; init; }

    public Guid? BrandId { get; init; }

    public Guid? ManufacturerId { get; init; }

    public List<Guid> CategoryIds { get; init; } = [];

    public List<ProductEditorAttributeDto> Attributes { get; init; } = [];

    public List<ProductEditorVariantDto> Variants { get; init; } = [];

    public List<ProductEditorImageDto> Images { get; init; } = [];

    public int StockQuantity { get; init; }
}

public sealed class ProductEditorAttributeDto
{
    public Guid Id { get; init; }

    public Guid? CatalogAttributeId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public AttributeRole Role { get; init; }

    public bool IsRequired { get; init; }

    public int DisplayOrder { get; init; }

    public List<ProductEditorAttributeValueDto> Values { get; init; } = [];
}

public sealed class ProductEditorAttributeValueDto
{
    public Guid Id { get; init; }

    public Guid? CatalogAttributeValueId { get; init; }

    public string Value { get; init; } = string.Empty;

    public string? DisplayValue { get; init; }

    public string? ColorHex { get; init; }

    public int DisplayOrder { get; init; }
}

public sealed class ProductEditorVariantDto
{
    public Guid Id { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string? Barcode { get; init; }

    public decimal PriceOverride { get; init; }

    public decimal? ComparePrice { get; init; }

    public string CombinationKey { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public int StockQuantity { get; init; }

    public List<ProductEditorVariantAttributeDto> Attributes { get; init; } = [];

    public List<ProductEditorImageDto> Images { get; init; } = [];
}

public sealed class ProductEditorVariantAttributeDto
{
    public Guid AttributeValueId { get; init; }

    public Guid ProductAttributeId { get; init; }

    public Guid? CatalogAttributeId { get; init; }

    public string AttributeCode { get; init; } = string.Empty;

    public string AttributeName { get; init; } = string.Empty;

    public AttributeRole Role { get; init; }

    public string Value { get; init; } = string.Empty;

    public string? DisplayValue { get; init; }

    public string? ColorHex { get; init; }
}

public sealed class ProductEditorImageDto
{
    public Guid Id { get; init; }

    public string ImageUrl { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsPrimary { get; init; }
}