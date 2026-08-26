using System.ComponentModel.DataAnnotations;

namespace NexaEcommerce.Modules.Catalog.Application.Brands.DTOs;

public sealed record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    bool IsActive);

public sealed record BrandListDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    bool IsActive,
    bool IsPublished,
    bool IsFeatured,
    int DisplayOrder);

public sealed record BrandDetailsDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Website,
    string? LogoUrl,
    string? CoverImageUrl,
    string? SeoTitle,
    string? SeoDescription,
    string? SeoKeywords,
    bool IsActive,
    bool IsPublished,
    bool IsFeatured,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateBrandDto(
    [property: Required]
    [property: StringLength(150, MinimumLength = 2)]
    string Name,

    [property: StringLength(5000)]
    string? Description = null,

    [property: Url]
    [property: StringLength(1000)]
    string? Website = null,

    [property: Url]
    [property: StringLength(1000)]
    string? LogoUrl = null,

    [property: Url]
    [property: StringLength(1000)]
    string? CoverImageUrl = null,

    [property: StringLength(200)]
    string? SeoTitle = null,

    [property: StringLength(500)]
    string? SeoDescription = null,

    [property: StringLength(1000)]
    string? SeoKeywords = null);

public sealed record UpdateBrandDto(
    [property: Required]
    [property: StringLength(150, MinimumLength = 2)]
    string Name,

    [property: StringLength(200)]
    string? Slug = null,

    [property: StringLength(5000)]
    string? Description = null,

    [property: Url]
    [property: StringLength(1000)]
    string? Website = null,

    [property: Url]
    [property: StringLength(1000)]
    string? LogoUrl = null,

    [property: Url]
    [property: StringLength(1000)]
    string? CoverImageUrl = null,

    [property: StringLength(200)]
    string? SeoTitle = null,

    [property: StringLength(500)]
    string? SeoDescription = null,

    [property: StringLength(1000)]
    string? SeoKeywords = null,

    bool IsActive = true,

    bool IsPublished = false,

    bool IsFeatured = false,

    [property: Range(0, int.MaxValue)]
    int DisplayOrder = 0);

public sealed record BrandLookupDto(
    Guid Id,
    string Name);

public sealed record BrandFilterDto
{
    public string? Search { get; init; }

    public bool? IsActive { get; init; }

    public bool? IsPublished { get; init; }

    public bool? IsFeatured { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? SortBy { get; init; }

    public bool Desc { get; init; }
}