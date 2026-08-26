using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Application.Manufacturers.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Infrastructure.Repositories;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class ManufacturerService : IManufacturerService
{
    private readonly ManufacturerRepository _repository;

    public ManufacturerService(
        ManufacturerRepository repository)
    {
        _repository = repository;
    }

    public async Task<object> GetPagedAsync(
        ManufacturerFilterDto filter,
        CancellationToken ct)
    {
        var query = _repository
            .Query()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.Slug.Contains(search));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(
                x => x.IsActive == filter.IsActive.Value);

        if (filter.IsPublished.HasValue)
            query = query.Where(
                x => x.IsPublished == filter.IsPublished.Value);

        if (filter.IsFeatured.HasValue)
            query = query.Where(
                x => x.IsFeatured == filter.IsFeatured.Value);

        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "name" => filter.Desc
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name),

            "displayorder" => filter.Desc
                ? query.OrderByDescending(x => x.DisplayOrder)
                : query.OrderBy(x => x.DisplayOrder),

            _ => query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new ManufacturerListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                LogoUrl = x.LogoUrl,
                DisplayOrder = x.DisplayOrder,
                IsActive = x.IsActive,
                IsPublished = x.IsPublished,
                IsFeatured = x.IsFeatured,
                ProductCount = x.Products.Count
            })
            .ToListAsync(ct);

        return new
        {
            items,
            total,
            page = filter.Page,
            pageSize = filter.PageSize,
            totalPages =
                (int)Math.Ceiling(
                    total / (double)filter.PageSize)
        };
    }

    public async Task<List<ManufacturerLookupDto>>
        GetLookupAsync(
            CancellationToken ct)
    {
        return await _repository
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.IsPublished)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ManufacturerLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug
            })
            .ToListAsync(ct);
    }

    public async Task<ManufacturerDetailsDto?>
        GetByIdAsync(
            Guid id,
            CancellationToken ct)
    {
        return await _repository
            .Query()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManufacturerDetailsDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Description = x.Description,
                Website = x.Website,
                LogoUrl = x.LogoUrl,
                CoverImageUrl = x.CoverImageUrl,
                SeoTitle = x.SeoTitle,
                SeoDescription = x.SeoDescription,
                SeoKeywords = x.SeoKeywords,
                DisplayOrder = x.DisplayOrder,
                IsActive = x.IsActive,
                IsPublished = x.IsPublished,
                IsFeatured = x.IsFeatured,
                ProductCount = x.Products.Count
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ManufacturerDetailsDto?>
        GetBySlugAsync(
            string slug,
            CancellationToken ct)
    {
        return await _repository
            .Query()
            .AsNoTracking()
            .Where(x => x.Slug == slug)
            .Select(x => new ManufacturerDetailsDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Description = x.Description,
                Website = x.Website,
                LogoUrl = x.LogoUrl,
                CoverImageUrl = x.CoverImageUrl,
                SeoTitle = x.SeoTitle,
                SeoDescription = x.SeoDescription,
                SeoKeywords = x.SeoKeywords,
                DisplayOrder = x.DisplayOrder,
                IsActive = x.IsActive,
                IsPublished = x.IsPublished,
                IsFeatured = x.IsFeatured,
                ProductCount = x.Products.Count
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(
        CreateManufacturerDto request,
        CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (await _repository.ExistsByNameAsync(
                name,
                null,
                ct))
        {
            throw new InvalidOperationException(
                "A manufacturer with this name already exists.");
        }

        var slug = GenerateSlug(name);

        var originalSlug = slug;
        var counter = 2;

        while (await _repository.ExistsBySlugAsync(
                   slug,
                   null,
                   ct))
        {
            slug = $"{originalSlug}-{counter++}";
        }

        var manufacturer = new Manufacturer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = request.Description?.Trim(),
            Website = request.Website?.Trim(),
            LogoUrl = request.LogoUrl?.Trim(),
            CoverImageUrl = request.CoverImageUrl?.Trim(),
            SeoTitle = request.SeoTitle?.Trim(),
            SeoDescription = request.SeoDescription?.Trim(),
            SeoKeywords = request.SeoKeywords?.Trim(),
            DisplayOrder = 0,
            IsActive = true,
            IsPublished = false,
            IsFeatured = false,
            IsDeleted = false
        };

        await _repository.AddAsync(
            manufacturer,
            ct);

        await _repository.SaveChangesAsync(ct);

        return manufacturer.Id;
    }

    public async Task<ManufacturerDetailsDto>
        UpdateAsync(
            Guid id,
            UpdateManufacturerDto request,
            CancellationToken ct)
    {
        var manufacturer =
            await _repository.GetByIdAsync(id, ct);

        if (manufacturer is null)
            throw new KeyNotFoundException(
                "Manufacturer not found.");

        var name = request.Name.Trim();

        if (await _repository.ExistsByNameAsync(
                name,
                id,
                ct))
        {
            throw new InvalidOperationException(
                "A manufacturer with this name already exists.");
        }

        manufacturer.Name = name;

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var slug = request.Slug.Trim();

            if (await _repository.ExistsBySlugAsync(
                    slug,
                    id,
                    ct))
            {
                throw new InvalidOperationException(
                    "A manufacturer with this slug already exists.");
            }

            manufacturer.Slug = slug;
        }

        manufacturer.Description =
            request.Description?.Trim();

        manufacturer.Website =
            request.Website?.Trim();

        manufacturer.LogoUrl =
            request.LogoUrl?.Trim();

        manufacturer.CoverImageUrl =
            request.CoverImageUrl?.Trim();

        manufacturer.SeoTitle =
            request.SeoTitle?.Trim();

        manufacturer.SeoDescription =
            request.SeoDescription?.Trim();

        manufacturer.SeoKeywords =
            request.SeoKeywords?.Trim();

        manufacturer.IsActive =
            request.IsActive;

        manufacturer.IsPublished =
            request.IsPublished;

        manufacturer.IsFeatured =
            request.IsFeatured;

        manufacturer.DisplayOrder =
            Math.Max(0, request.DisplayOrder);

        await _repository.SaveChangesAsync(ct);

        return (await GetByIdAsync(id, ct))!;
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken ct)
    {
        var manufacturer =
            await _repository.GetByIdAsync(id, ct);

        if (manufacturer is null)
            throw new KeyNotFoundException(
                "Manufacturer not found.");

        manufacturer.IsDeleted = true;

        await _repository.SaveChangesAsync(ct);
    }

    public async Task RestoreAsync(
        Guid id,
        CancellationToken ct)
    {
        var manufacturer =
            await _repository
                .Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    ct);

        if (manufacturer is null)
            throw new KeyNotFoundException(
                "Manufacturer not found.");

        manufacturer.IsDeleted = false;

        await _repository.SaveChangesAsync(ct);
    }

    public Task ActivateAsync(
        Guid id,
        CancellationToken ct)
    {
        return SetStatusAsync(
            id,
            true,
            ct);
    }

    public Task DeactivateAsync(
        Guid id,
        CancellationToken ct)
    {
        return SetStatusAsync(
            id,
            false,
            ct);
    }

    private async Task SetStatusAsync(
        Guid id,
        bool active,
        CancellationToken ct)
    {
        var manufacturer =
            await _repository.GetByIdAsync(id, ct);

        if (manufacturer is null)
            throw new KeyNotFoundException(
                "Manufacturer not found.");

        manufacturer.IsActive = active;

        await _repository.SaveChangesAsync(ct);
    }

    public Task PublishAsync(
        Guid id,
        CancellationToken ct)
    {
        return SetPublishedAsync(
            id,
            true,
            ct);
    }

    public Task UnPublishAsync(
        Guid id,
        CancellationToken ct)
    {
        return SetPublishedAsync(
            id,
            false,
            ct);
    }

    private async Task SetPublishedAsync(
        Guid id,
        bool published,
        CancellationToken ct)
    {
        var manufacturer =
            await _repository.GetByIdAsync(id, ct);

        if (manufacturer is null)
            throw new KeyNotFoundException(
                "Manufacturer not found.");

        manufacturer.IsPublished = published;

        await _repository.SaveChangesAsync(ct);
    }

    public Task FeatureAsync(
        Guid id,
        CancellationToken ct)
    {
        return SetFeaturedAsync(
            id,
            true,
            ct);
    }

    public Task UnFeatureAsync(
        Guid id,
        CancellationToken ct)
    {
        return SetFeaturedAsync(
            id,
            false,
            ct);
    }

    private async Task SetFeaturedAsync(
        Guid id,
        bool featured,
        CancellationToken ct)
    {
        var manufacturer =
            await _repository.GetByIdAsync(id, ct);

        if (manufacturer is null)
            throw new KeyNotFoundException(
                "Manufacturer not found.");

        manufacturer.IsFeatured = featured;

        await _repository.SaveChangesAsync(ct);
    }

    private static string GenerateSlug(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(c =>
                char.IsLetterOrDigit(c)
                    ? c
                    : '-')
            .ToArray();

        return new string(chars)
            .Trim('-');
    }
}