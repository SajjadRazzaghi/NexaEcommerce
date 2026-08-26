using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Application.Brands.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Pagination;

namespace NexaEcommerce.Modules.Catalog.Infrastructure.Repositories;

public sealed class BrandRepository : IBrandRepository
{
    private readonly CatalogDbContext _context;

    public BrandRepository(
        CatalogDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // Get By Id
    // =========================================================

    public async Task<Brand?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    // =========================================================
    // Get By Slug
    // =========================================================

    public async Task<Brand?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        slug = slug.Trim();

        return await _context.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Slug == slug,
                cancellationToken);
    }

    // =========================================================
    // Get Deleted
    // =========================================================

    public async Task<Brand?> GetDeletedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Brands
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.IsDeleted,
                cancellationToken);
    }

    // =========================================================
    // Exists By Id
    // =========================================================

    public async Task<bool> ExistsByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return false;

        return await _context.Brands
            .AnyAsync(
                x => x.Id == id,
                cancellationToken);
    }

    // =========================================================
    // Exists By Name
    // =========================================================

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        name = name.Trim();

        var query = _context.Brands
            .AsNoTracking()
            .Where(x => x.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(
                x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(
            cancellationToken);
    }

    // =========================================================
    // Exists By Slug
    // =========================================================

    public async Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        slug = slug.Trim();

        var query = _context.Brands
            .AsNoTracking()
            .Where(x => x.Slug == slug);

        if (excludeId.HasValue)
        {
            query = query.Where(
                x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(
            cancellationToken);
    }

    // =========================================================
    // Lookup
    // =========================================================

    public async Task<IReadOnlyList<Brand>> GetLookupAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Brands
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    // =========================================================
    // Paged
    // =========================================================

    public async Task<PagedResult<Brand>> GetPagedAsync(
        BrandFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var page =
            Math.Max(1, filter.Page);

        var pageSize =
            Math.Clamp(
                filter.PageSize,
                1,
                200);

        IQueryable<Brand> query =
            _context.Brands
                .AsNoTracking();

        // -----------------------------------------------------
        // Search
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search =
                filter.Search.Trim();

            query = query.Where(x =>
                EF.Functions.Like(
                    x.Name,
                    $"%{search}%")

                || EF.Functions.Like(
                    x.Slug,
                    $"%{search}%")

                || (
                    x.Description != null &&
                    EF.Functions.Like(
                        x.Description,
                        $"%{search}%"))
            );
        }

        // -----------------------------------------------------
        // Active
        // -----------------------------------------------------

        if (filter.IsActive.HasValue)
        {
            query = query.Where(
                x =>
                    x.IsActive ==
                    filter.IsActive.Value);
        }

        // -----------------------------------------------------
        // Published
        // -----------------------------------------------------

        if (filter.IsPublished.HasValue)
        {
            query = query.Where(
                x =>
                    x.IsPublished ==
                    filter.IsPublished.Value);
        }

        // -----------------------------------------------------
        // Featured
        // -----------------------------------------------------

        if (filter.IsFeatured.HasValue)
        {
            query = query.Where(
                x =>
                    x.IsFeatured ==
                    filter.IsFeatured.Value);
        }

        // -----------------------------------------------------
        // Count BEFORE paging
        // -----------------------------------------------------

        var totalItems =
            await query.CountAsync(
                cancellationToken);

        // -----------------------------------------------------
        // Stable Sorting
        // -----------------------------------------------------

        query =
            ApplySorting(
                query,
                filter);

        // -----------------------------------------------------
        // Paging
        // -----------------------------------------------------

        var items =
            await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(
                    cancellationToken);

        // -----------------------------------------------------
        // Result
        // -----------------------------------------------------

        return PagedResult<Brand>.Create(
            items,
            page,
            pageSize,
            totalItems);
    }

    // =========================================================
    // Sorting
    // =========================================================

    private static IQueryable<Brand> ApplySorting(
        IQueryable<Brand> query,
        BrandFilterDto filter)
    {
        var sort =
            filter.SortBy?
                .Trim()
                .ToLowerInvariant();

        return sort switch
        {
            "name" =>
                filter.Desc
                    ? query
                        .OrderByDescending(x => x.Name)
                        .ThenBy(x => x.Id)
                    : query
                        .OrderBy(x => x.Name)
                        .ThenBy(x => x.Id),

            "slug" =>
                filter.Desc
                    ? query
                        .OrderByDescending(x => x.Slug)
                        .ThenBy(x => x.Id)
                    : query
                        .OrderBy(x => x.Slug)
                        .ThenBy(x => x.Id),

            "createdat" =>
                filter.Desc
                    ? query
                        .OrderByDescending(x => x.CreatedAt)
                        .ThenBy(x => x.Id)
                    : query
                        .OrderBy(x => x.CreatedAt)
                        .ThenBy(x => x.Id),

            "updatedat" =>
                filter.Desc
                    ? query
                        .OrderByDescending(x => x.UpdatedAt)
                        .ThenBy(x => x.Id)
                    : query
                        .OrderBy(x => x.UpdatedAt)
                        .ThenBy(x => x.Id),

            "displayorder" =>
                filter.Desc
                    ? query
                        .OrderByDescending(x => x.DisplayOrder)
                        .ThenBy(x => x.Name)
                        .ThenBy(x => x.Id)
                    : query
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.Name)
                        .ThenBy(x => x.Id),

            _ =>
                query
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Id)
        };
    }

    // =========================================================
    // Add
    // =========================================================

    public async Task AddAsync(
        Brand brand,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(brand);

        await _context.Brands.AddAsync(
            brand,
            cancellationToken);
    }

    // =========================================================
    // Update
    // =========================================================

    public void Update(
        Brand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);

        _context.Brands.Update(brand);
    }

    // =========================================================
    // Delete
    // =========================================================

    public void Delete(
        Brand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);

        brand.Delete();

        _context.Brands.Update(brand);
    }
}