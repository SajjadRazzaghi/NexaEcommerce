namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class ProductVariantDto
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = null!;

    // Backward compatibility for existing frontend code.
    public string? Color { get; set; }

    public string? Size { get; set; }

    public decimal PriceOverride { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    // Generic attribute selections of this variant.
    public List<ProductVariantAttributeDto> Attributes { get; set; } = new();
}

public sealed class ProductVariantAttributeDto
{
    public Guid AttributeValueId { get; set; }

    public string AttributeCode { get; set; } = string.Empty;

    public string AttributeName { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? DisplayValue { get; set; }

    public string? ColorHex { get; set; }
}

