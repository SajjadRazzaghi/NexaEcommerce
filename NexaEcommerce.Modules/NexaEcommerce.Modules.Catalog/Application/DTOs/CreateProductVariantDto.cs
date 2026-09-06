namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class CreateProductVariantDto
{
    public string Sku { get; set; } = string.Empty;

    // Backward compatible fields.
    public string? Color { get; set; }

    public string? Size { get; set; }

    public decimal? PriceOverride { get; set; }

    public int StockQuantity { get; set; }

    // Generic catalog attribute values selected for this variant.
    //
    // Example:
    // Color = Red  -> catalog attribute value id
    // Size  = XL   -> catalog attribute value id
    // Material = Cotton -> catalog attribute value id
    //
    // This allows the product system to support any future
    // variant attribute without changing this DTO again.
    public List<Guid> AttributeValueIds { get; set; } = new();
}

