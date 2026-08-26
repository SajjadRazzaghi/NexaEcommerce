namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? Currency { get; set; }

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public decimal? ComparePrice { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public Guid? BrandId { get; set; }

    public List<Guid> CategoryIds { get; set; } = new();

    public bool IsActive { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsPublished { get; set; }
}
