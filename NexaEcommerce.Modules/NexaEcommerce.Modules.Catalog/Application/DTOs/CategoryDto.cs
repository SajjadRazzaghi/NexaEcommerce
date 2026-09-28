namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class CategoryDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string? SeoKeywords { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public string? ParentCategoryName { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public int ProductCount { get; set; }

    public List<CategoryDto> SubCategories { get; set; }
        = new();
}

public sealed class CreateCategoryDto
{
    public string Name { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string? SeoKeywords { get; set; }

    public Guid? ParentCategoryId { get; set; }

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

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string? SeoKeywords { get; set; }

    public Guid? ParentCategoryId { get; set; }

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

    public List<CategoryHierarchyDto> Children { get; set; } = new();
}