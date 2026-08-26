namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public class ProductVariantDto
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = null!;

    public string? Color { get; set; }

    public string? Size { get; set; }

    public decimal PriceOverride { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }
}