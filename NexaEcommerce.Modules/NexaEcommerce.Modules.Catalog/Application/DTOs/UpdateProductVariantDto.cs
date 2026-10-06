namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class UpdateProductVariantDto
{
    /// <summary>
    /// Existing variant id.
    /// Null/empty means create a new variant.
    /// </summary>
    public Guid? Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public decimal? PriceOverride { get; set; }

    public decimal? ComparePrice { get; set; }

    /*
     * IDs belong to ProductAttributeValue records.
     *
     * Only ProductAttribute values with VariantDefining role
     * can participate in the combination.
     */
    public List<Guid> AttributeValueIds { get; set; } = new();

    public List<string> Images { get; set; } = new();

    public bool IsActive { get; set; } = true;/*
     * New variants may still receive this temporarily for backward
     * compatibility. Existing variant stock must be changed through
     * Inventory.
     */
    public int? StockQuantity { get; set; }
}
