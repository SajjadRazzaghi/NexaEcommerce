
namespace NexaEcommerce.Modules.Catalog.Application.Manufacturers.DTOs;
public sealed record CreateManufacturerDto
{
    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string? Website { get; init; }

    public string? LogoUrl { get; init; }

    public string? CoverImageUrl { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public string? SeoKeywords { get; init; }
}



public sealed record UpdateManufacturerDto
{
    public string Name { get; init; } = null!;

    public string? Slug { get; init; }

    public string? Description { get; init; }

    public string? Website { get; init; }

    public string? LogoUrl { get; init; }

    public string? CoverImageUrl { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public string? SeoKeywords { get; init; }

    public bool IsActive { get; init; }

    public bool IsPublished { get; init; }

    public bool IsFeatured { get; init; }

    public int DisplayOrder { get; init; }
}



public sealed record ManufacturerDetailsDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Slug { get; init; } = null!;

    public string? Description { get; init; }

    public string? Website { get; init; }

    public string? LogoUrl { get; init; }

    public string? CoverImageUrl { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public string? SeoKeywords { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; }

    public bool IsPublished { get; init; }

    public bool IsFeatured { get; init; }

    public int ProductCount { get; init; }
}



public sealed record ManufacturerListItemDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Slug { get; init; } = null!;

    public string? LogoUrl { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; }

    public bool IsPublished { get; init; }

    public bool IsFeatured { get; init; }

    public int ProductCount { get; init; }
}

public sealed record ManufacturerLookupDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Slug { get; init; } = null!;
}



public sealed class ManufacturerFilterDto
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }

    public string? SortBy { get; set; }

    public bool Desc { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsPublished { get; set; }

    public bool? IsFeatured { get; set; }
}