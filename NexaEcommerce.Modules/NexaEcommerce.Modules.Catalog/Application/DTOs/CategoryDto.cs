namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class CategoryDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    // =========================================================
    // SEO
    // =========================================================

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string? SeoKeywords { get; set; }

    // =========================================================
    // Hierarchy
    // =========================================================

    public Guid? ParentCategoryId { get; set; }

    public string? ParentCategoryName { get; set; }

    // =========================================================
    // Display / Status
    // =========================================================

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    // =========================================================
    // Statistics
    // =========================================================

    public int ProductCount { get; set; }

    // =========================================================
    // Audit
    // =========================================================

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // =========================================================
    // Children
    // =========================================================

    public List<CategoryDto> SubCategories { get; set; }
        = new();
}

public sealed class CreateCategoryDto
{
    public string Name { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    // SEO
    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string? SeoKeywords { get; set; }

    // Hierarchy
    public Guid? ParentCategoryId { get; set; }

    // Display / Status
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }
}

public sealed class UpdateCategoryDto
{
    public string Name { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    // SEO
    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string? SeoKeywords { get; set; }

    // Hierarchy
    public Guid? ParentCategoryId { get; set; }

    // Display / Status
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }
}

public class CategoryHierarchyDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Slug { get; set; }

    public List<CategoryHierarchyDto> Children { get; set; }
        = new();
}