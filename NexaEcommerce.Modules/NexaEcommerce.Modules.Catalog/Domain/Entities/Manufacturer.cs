namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public sealed class Manufacturer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? Website { get; set; }

    public string? LogoUrl { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string? SeoKeywords { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsDeleted { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<Product> Products { get; set; }
        = new List<Product>();
}