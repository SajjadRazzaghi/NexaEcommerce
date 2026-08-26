namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class CreateProductVariantDto
{
    public string Sku { get; set; } = string.Empty;

    public string? Color { get; set; }

    public string? Size { get; set; }

    public decimal? PriceOverride { get; set; }

    public int StockQuantity { get; set; }
}