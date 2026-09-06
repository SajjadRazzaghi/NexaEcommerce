namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class UpdateProductVariantDto
{
    /// <summary>
    /// Existing variant id.
    /// Null/empty means create a new variant.
    /// </summary>
    public Guid? Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    // Backward-compatible fields.
    public string? Color { get; set; }

    public string? Size { get; set; }

    public decimal? PriceOverride { get; set; }

    public decimal? ComparePrice { get; set; }

    /// <summary>
    /// Used only when creating a new variant.
    /// Existing variant stock must be changed through Inventory.
    /// </summary>
    public int? StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// IDs of catalog-level attribute values.
    /// </summary>
    public List<Guid> AttributeValueIds { get; set; } = new();
}