using NexaEcommerce.Modules.Catalog.Application.Brands.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.SharedKernel.Pagination;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class BrandService : IBrandService
{
    private readonly IBrandRepository _repository;
    private readonly CatalogDbContext _context;

    public BrandService(
        IBrandRepository repository,
        CatalogDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    // =========================================================
    // Get By Id
    // =========================================================

    public async Task<BrandDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        var brand = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        return brand is null
            ? null
            : MapDetails(brand);
    }

    // =========================================================
    // Get By Slug
    // =========================================================

    public async Task<BrandDetailsDto?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var brand = await _repository.GetBySlugAsync(
            slug.Trim(),
            cancellationToken);

        return brand is null
            ? null
            : MapDetails(brand);
    }

    // =========================================================
    // Paged
    // =========================================================

    public async Task<PagedResult<BrandListDto>> GetPagedAsync(
        BrandFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var normalizedFilter = NormalizeFilter(filter);

        var result = await _repository.GetPagedAsync(
            normalizedFilter,
            cancellationToken);

        var items = result.Items
            .Select(MapList)
            .ToList();

        return PagedResult<BrandListDto>.Create(
            items,
            result.Page,
            result.PageSize,
            result.TotalItems);
    }

    // =========================================================
    // Lookup
    // =========================================================

    public async Task<IReadOnlyList<BrandLookupDto>> GetLookupAsync(
        CancellationToken cancellationToken = default)
    {
        var brands = await _repository.GetLookupAsync(
            cancellationToken);

        return brands
            .Select(MapLookup)
            .ToList();
    }

    // =========================================================
    // Create
    // =========================================================

    public async Task<Guid> CreateAsync(
        CreateBrandDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var name = NormalizeRequired(dto.Name);

        ValidateName(name);

        // -----------------------------------------------------
        // Duplicate name
        // -----------------------------------------------------

        if (await _repository.ExistsByNameAsync(
                name,
                null,
                cancellationToken))
        {
            throw new InvalidOperationException(
                $"A brand with the name '{name}' already exists.");
        }

        // -----------------------------------------------------
        // Generate unique slug
        // -----------------------------------------------------

        var slug = await GenerateUniqueSlugAsync(
            GenerateSlug(name),
            null,
            cancellationToken);

        // -----------------------------------------------------
        // Create domain entity
        // -----------------------------------------------------

        var brand = Brand.Create(
            name,
            NormalizeNullable(dto.Description),
            NormalizeNullable(dto.Website));

        brand.ChangeSlug(slug);

        brand.ChangeLogo(
            NormalizeNullable(dto.LogoUrl));

        brand.ChangeCover(
            NormalizeNullable(dto.CoverImageUrl));

        brand.ChangeSeo(
            NormalizeNullable(dto.SeoTitle),
            NormalizeNullable(dto.SeoDescription),
            NormalizeNullable(dto.SeoKeywords));

        // New Brand defaults:
        // Active = true
        // Published = false
        // Featured = false
        // DisplayOrder = 0

        await _repository.AddAsync(
            brand,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);

        return brand.Id;
    }

    // =========================================================
    // Update
    // =========================================================

    public async Task<BrandDetailsDto> UpdateAsync(
        Guid id,
        UpdateBrandDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var brand = await GetRequiredAsync(
            id,
            cancellationToken);

        var name = NormalizeRequired(dto.Name);

        ValidateName(name);

        // -----------------------------------------------------
        // Duplicate name
        // -----------------------------------------------------

        if (await _repository.ExistsByNameAsync(
                name,
                id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                $"A brand with the name '{name}' already exists.");
        }

        // -----------------------------------------------------
        // Slug
        // -----------------------------------------------------

        var requestedSlug =
            string.IsNullOrWhiteSpace(dto.Slug)
                ? GenerateSlug(name)
                : GenerateSlug(dto.Slug);

        var slug = await GenerateUniqueSlugAsync(
            requestedSlug,
            id,
            cancellationToken);

        // -----------------------------------------------------
        // Basic information
        // -----------------------------------------------------

        brand.Rename(name);

        brand.ChangeSlug(slug);

        brand.ChangeDescription(
            NormalizeNullable(dto.Description));

        brand.ChangeWebsite(
            NormalizeNullable(dto.Website));

        // -----------------------------------------------------
        // Media
        // -----------------------------------------------------

        brand.ChangeLogo(
            NormalizeNullable(dto.LogoUrl));

        brand.ChangeCover(
            NormalizeNullable(dto.CoverImageUrl));

        // -----------------------------------------------------
        // SEO
        // -----------------------------------------------------

        brand.ChangeSeo(
            NormalizeNullable(dto.SeoTitle),
            NormalizeNullable(dto.SeoDescription),
            NormalizeNullable(dto.SeoKeywords));

        // -----------------------------------------------------
        // Display order
        // -----------------------------------------------------

        brand.ChangeDisplayOrder(
            Math.Max(0, dto.DisplayOrder));

        // -----------------------------------------------------
        // Active status
        // -----------------------------------------------------

        if (dto.IsActive)
        {
            brand.Activate();
        }
        else
        {
            brand.Deactivate();
        }

        // -----------------------------------------------------
        // Published status
        // -----------------------------------------------------

        if (dto.IsPublished)
        {
            if (!brand.IsActive)
            {
                throw new InvalidOperationException(
                    "An inactive brand cannot be published.");
            }

            brand.Publish();
        }
        else
        {
            brand.UnPublish();
        }

        // -----------------------------------------------------
        // Featured status
        // -----------------------------------------------------

        if (dto.IsFeatured)
        {
            if (!brand.IsActive)
            {
                throw new InvalidOperationException(
                    "An inactive brand cannot be featured.");
            }

            brand.Feature();
        }
        else
        {
            brand.UnFeature();
        }

        _repository.Update(brand);

        await _context.SaveChangesAsync(
            cancellationToken);

        return MapDetails(brand);
    }

    // =========================================================
    // Delete
    // =========================================================

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var brand = await GetRequiredAsync(
            id,
            cancellationToken);

        _repository.Delete(brand);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // Restore
    // =========================================================

    public async Task RestoreAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var brand =
            await _repository.GetDeletedByIdAsync(
                id,
                cancellationToken);

        if (brand is null)
        {
            throw new KeyNotFoundException(
                $"Deleted brand '{id}' was not found.");
        }

        brand.Restore();

        _repository.Update(brand);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // Activate
    // =========================================================

    public async Task ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var brand = await GetRequiredAsync(
            id,
            cancellationToken);

        brand.Activate();

        _repository.Update(brand);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // Deactivate
    // =========================================================

    public async Task DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var brand = await GetRequiredAsync(
            id,
            cancellationToken);

        brand.Deactivate();

        _repository.Update(brand);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // Publish
    // =========================================================

    public async Task PublishAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var brand = await GetRequiredAsync(
            id,
            cancellationToken);

        brand.Publish();

        _repository.Update(brand);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // UnPublish
    // =========================================================

    public async Task UnPublishAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var brand = await GetRequiredAsync(
            id,
            cancellationToken);

        brand.UnPublish();

        _repository.Update(brand);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // Feature
    // =========================================================

    public async Task FeatureAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var brand = await GetRequiredAsync(
            id,
            cancellationToken);

        brand.Feature();

        _repository.Update(brand);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // UnFeature
    // =========================================================

    public async Task UnFeatureAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var brand = await GetRequiredAsync(
            id,
            cancellationToken);

        brand.UnFeature();

        _repository.Update(brand);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private async Task<Brand> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Brand id is required.",
                nameof(id));
        }

        var brand = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (brand is null)
        {
            throw new KeyNotFoundException(
                $"Brand '{id}' was not found.");
        }

        return brand;
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string slug,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var candidate = slug;
        var counter = 2;

        while (await _repository.ExistsBySlugAsync(
                   candidate,
                   excludeId,
                   cancellationToken))
        {
            candidate = $"{slug}-{counter}";
            counter++;
        }

        return candidate;
    }

    private static BrandFilterDto NormalizeFilter(
        BrandFilterDto filter)
    {
        return filter with
        {
            Page = Math.Max(
                1,
                filter.Page),

            PageSize = Math.Clamp(
                filter.PageSize,
                1,
                200),

            Search =
                NormalizeNullable(filter.Search),

            SortBy =
                NormalizeNullable(filter.SortBy)
        };
    }

    private static string NormalizeRequired(
        string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void ValidateName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Brand name is required.",
                nameof(name));
        }

        if (name.Length > 150)
        {
            throw new ArgumentException(
                "Brand name cannot exceed 150 characters.",
                nameof(name));
        }
    }

    private static string GenerateSlug(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Slug source cannot be empty.",
                nameof(value));
        }

        var slug = value
            .Trim()
            .ToLowerInvariant();

        var characters = slug
            .Select(character =>
                char.IsLetterOrDigit(character)
                    ? character
                    : '-')
            .ToArray();

        slug = new string(characters);

        while (slug.Contains("--"))
        {
            slug = slug.Replace(
                "--",
                "-");
        }

        slug = slug.Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new InvalidOperationException(
                "A valid slug could not be generated.");
        }

        if (slug.Length > 200)
        {
            slug = slug[..200].Trim('-');
        }

        return slug;
    }

    // =========================================================
    // Mapping
    // =========================================================

    private static BrandDetailsDto MapDetails(
        Brand brand)
    {
        return new BrandDetailsDto(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.Description,
            brand.Website,
            brand.LogoUrl,
            brand.CoverImageUrl,
            brand.SeoTitle,
            brand.SeoDescription,
            brand.SeoKeywords,
            brand.IsActive,
            brand.IsPublished,
            brand.IsFeatured,
            brand.DisplayOrder,
            brand.CreatedAt,
            brand.UpdatedAt);
    }

    private static BrandListDto MapList(
        Brand brand)
    {
        return new BrandListDto(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.LogoUrl,
            brand.IsActive,
            brand.IsPublished,
            brand.IsFeatured,
            brand.DisplayOrder);
    }

    private static BrandLookupDto MapLookup(
        Brand brand)
    {
        return new BrandLookupDto(
            brand.Id,
            brand.Name);
    }
}