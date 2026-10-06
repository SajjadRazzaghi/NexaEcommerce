namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class ProductAttributeDto
{
    public Guid Id { get; set; }

    public Guid? CatalogAttributeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Role { get; set; }

    public int RoleValue { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    public List<ProductAttributeValueDto> Values { get; set; } = new();
}

public sealed class ProductAttributeValueDto
{
    public Guid Id { get; set; }

    public Guid? CatalogAttributeValueId { get; set; }

    public string Value { get; set; } = string.Empty;

    public string? DisplayValue { get; set; }

    public string? ColorHex { get; set; }
}

public sealed class ProductAttributeInputDto
{
    public Guid CatalogAttributeId { get; set; }

    public string? Role { get; set; }

    public int? RoleValue { get; set; }

    public bool? IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    public List<ProductAttributeValueInputDto> Values { get; set; }
        = new();
}

public sealed class ProductAttributeValueInputDto
{
    public Guid? CatalogAttributeValueId { get; set; }

    public string? Value { get; set; }

    public string? DisplayValue { get; set; }

    public string? ColorHex { get; set; }

    public int DisplayOrder { get; set; }
}

public sealed class ProductVariantAttributeDto
{
    public Guid AttributeValueId { get; set; }

    public Guid ProductAttributeId { get; set; }

    public Guid? CatalogAttributeId { get; set; }

    public string AttributeCode { get; set; } = string.Empty;

    public string AttributeName { get; set; } = string.Empty;

    public string? Role { get; set; }

    public int RoleValue { get; set; }

    public string Value { get; set; } = string.Empty;

    public string? DisplayValue { get; set; }

    public string? ColorHex { get; set; }
}

public sealed class ProductVariantDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? CombinationKey { get; set; }

    public decimal PriceOverride { get; set; }

    public decimal? ComparePrice { get; set; }

    public bool IsActive { get; set; }public int StockQuantity { get; set; }

    public List<ProductVariantAttributeDto> Attributes { get; set; }
        = new();

    public List<ProductVariantImageDto> Images { get; set; }
        = new();
}

public sealed class ProductVariantImageDto
{
    public Guid Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
}

public sealed class ProductImageDto
{
    public Guid Id { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("isMain")]
    public bool IsPrimary { get; set; }
}

public sealed class ProductDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /*
     * Temporary compatibility field.
     *
     * The canonical sellable SKU lives on ProductVariant.
     */
    public string Sku { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public decimal Price { get; set; }

    public decimal? ComparePrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal FinalPrice { get; set; }

    public string Currency { get; set; } = "IRR";

    public bool IsActive { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsPublished { get; set; }

    public bool IsInStock { get; set; }

    /*
     * Temporary compatibility fields.
     */
    public int StockQuantity { get; set; }

    public Guid? BrandId { get; set; }

    public string? BrandName { get; set; }

    public Guid? ManufacturerId { get; set; }

    public string? ManufacturerName { get; set; }

    public List<ProductImageDto> Images { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public List<Guid> CategoryIds { get; set; } = new();

    public List<ProductVariantDto> Variants { get; set; } = new();

    public List<ProductAttributeDto> Attributes { get; set; } = new();

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
