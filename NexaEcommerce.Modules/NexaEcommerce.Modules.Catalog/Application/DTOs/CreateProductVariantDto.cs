namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class CreateProductVariantDto
{
    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public decimal? PriceOverride { get; set; }

    public decimal? ComparePrice { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Guid> AttributeValueIds { get; set; } = new();

    public List<string> Images { get; set; } = new();

    public int StockQuantity { get; set; }
}