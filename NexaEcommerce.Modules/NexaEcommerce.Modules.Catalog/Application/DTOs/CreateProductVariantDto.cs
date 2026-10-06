namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class CreateProductVariantDto
{
    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public decimal? PriceOverride { get; set; }

    public decimal? ComparePrice { get; set; }

    /*
     * IDs belong to ProductAttributeValue records.
     *
     * Only values whose ProductAttribute.Role contains
     * VariantDefining are allowed here.
     */
    public List<Guid> AttributeValueIds { get; set; } = new();

    /*
     * Optional variant-specific media.
     */
    public List<string> Images { get; set; } = new();/*
     * Legacy compatibility only.
     *
     * Inventory is the source of truth for stock.
     */
    public int StockQuantity { get; set; }
}
