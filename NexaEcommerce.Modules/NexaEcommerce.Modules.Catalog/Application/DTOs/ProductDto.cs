namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class ProductDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

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

    public int StockQuantity { get; set; }

    public Guid? BrandId { get; set; }

    public string? BrandName { get; set; }

    public List<ProductImageDto> Images { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public List<Guid> CategoryIds { get; set; } = new();

    public List<ProductVariantDto> Variants { get; set; } = new();

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
