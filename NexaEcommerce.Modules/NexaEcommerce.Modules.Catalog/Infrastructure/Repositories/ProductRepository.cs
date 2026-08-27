using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;

namespace NexaEcommerce.Modules.Catalog.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;

    public ProductRepository(
        CatalogDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // Get By Id
    // ============================================================

    public async Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products

            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)

            .Include(p => p.Images)

            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
                    .ThenInclude(av => av.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)

            .Include(p => p.Attributes)
                .ThenInclude(a => a.Values)

            .Include(p => p.Reviews)

            .Include(p => p.Brand)

            .Include(p => p.Manufacturer)

            .FirstOrDefaultAsync(
                p =>
                    p.Id == id &&
                    !p.IsDeleted,
                cancellationToken);
    }

    // ============================================================
    // Get By Slug
    // ============================================================

    public async Task<Product?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var normalizedSlug =
            slug.Trim().ToLowerInvariant();

        return await _context.Products
            .AsNoTracking()

            .Where(p =>
                p.Slug == normalizedSlug &&
                !p.IsDeleted &&
                p.IsActive &&
                p.IsPublished)

            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)

            .Include(p => p.Images)

            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
                    .ThenInclude(av => av.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)

            .Include(p => p.Attributes)
                .ThenInclude(a => a.Values)

            .Include(p => p.Reviews)

            .Include(p => p.Brand)

            .Include(p => p.Manufacturer)

            .FirstOrDefaultAsync(
                cancellationToken);
    }

    // ============================================================
    // SKU Exists
    // ============================================================

    public async Task<bool> ExistsBySkuAsync(
        string sku,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return false;

        var normalizedSku =
            sku.Trim();

        return await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                p =>
                    p.Sku == normalizedSku &&
                    (!excludeId.HasValue ||
                     p.Id != excludeId.Value),
                cancellationToken);
    }

    // ============================================================
    // Slug Exists
    // ============================================================

    public async Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        var normalizedSlug =
            slug.Trim().ToLowerInvariant();

        return await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                p =>
                    p.Slug == normalizedSlug &&
                    (!excludeId.HasValue ||
                     p.Id != excludeId.Value),
                cancellationToken);
    }

    // ============================================================
    // Get All - Storefront
    // ============================================================

    public async Task<IEnumerable<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p =>
                !p.IsDeleted &&
                p.IsActive &&
                p.IsPublished)

            .Include(p => p.Images)

            .Include(p => p.Brand)

            .Include(p => p.Manufacturer)

            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)

            .Include(p => p.Variants)

            .Include(p => p.Reviews)

            .OrderByDescending(
                p => p.CreatedAt)

            .ToListAsync(
                cancellationToken);
    }

    // ============================================================
    // Get By Category
    // ============================================================

    public async Task<IEnumerable<Product>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p =>
                !p.IsDeleted &&
                p.IsActive &&
                p.IsPublished &&
                p.ProductCategories.Any(
                    pc => pc.CategoryId == categoryId))

            .Include(p => p.Images)

            .Include(p => p.Brand)

            .Include(p => p.Manufacturer)

            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)

            .Include(p => p.Variants)

            .Include(p => p.Reviews)

            .OrderByDescending(
                p => p.CreatedAt)

            .ToListAsync(
                cancellationToken);
    }

    // ============================================================
    // Search
    // ============================================================

    public async Task<IEnumerable<Product>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync(
                cancellationToken);

        var term =
            searchTerm.Trim();

        return await _context.Products
            .AsNoTracking()
            .Where(p =>
                !p.IsDeleted &&
                p.IsActive &&
                p.IsPublished)

            .Where(p =>
                EF.Functions.Like(
                    p.Name,
                    $"%{term}%") ||

                EF.Functions.Like(
                    p.Description ?? string.Empty,
                    $"%{term}%") ||

                EF.Functions.Like(
                    p.Sku,
                    $"%{term}%"))

            .Include(p => p.Images)

            .Include(p => p.Brand)

            .Include(p => p.Manufacturer)

            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)

            .Include(p => p.Variants)

            .Include(p => p.Reviews)

            .OrderByDescending(
                p => p.CreatedAt)

            .Take(50)

            .ToListAsync(
                cancellationToken);
    }

    // ============================================================
    // Featured
    // ============================================================

    public async Task<IEnumerable<Product>> GetFeaturedAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        count =
            Math.Clamp(
                count,
                1,
                50);

        return await _context.Products
            .AsNoTracking()
            .Where(p =>
                !p.IsDeleted &&
                p.IsActive &&
                p.IsPublished &&
                p.IsFeatured)

            .Include(p => p.Images)

            .Include(p => p.Brand)

            .Include(p => p.Manufacturer)

            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)

            .Include(p => p.Variants)

            .Include(p => p.Reviews)

            .OrderByDescending(
                p => p.CreatedAt)

            .Take(count)

            .ToListAsync(
                cancellationToken);
    }

    // ============================================================
    // Paged
    // ============================================================

    public async Task<(
        IReadOnlyList<Product> Items,
        int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? categoryId = null,
        Guid? brandId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isFeatured = null,
        bool? isInStock = null,
        bool? isActive = null,
        bool? isPublished = null,
        bool includeInactive = false,
        bool includeUnpublished = false,
        string? sortBy = null,
        bool desc = false,
        CancellationToken cancellationToken = default)
    {
        page =
            Math.Max(
                page,
                1);

        pageSize =
            Math.Clamp(
                pageSize,
                1,
                100);

        var query =
            _context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

        // --------------------------------------------------------
        // Visibility
        // --------------------------------------------------------

        if (!includeInactive)
            query =
                query.Where(
                    p => p.IsActive);

        if (!includeUnpublished)
            query =
                query.Where(
                    p => p.IsPublished);

        // --------------------------------------------------------
        // Status
        // --------------------------------------------------------

        if (isActive.HasValue)
        {
            query =
                query.Where(
                    p => p.IsActive ==
                         isActive.Value);
        }

        if (isPublished.HasValue)
        {
            query =
                query.Where(
                    p => p.IsPublished ==
                         isPublished.Value);
        }

        // --------------------------------------------------------
        // Category
        // --------------------------------------------------------

        if (categoryId.HasValue)
        {
            query =
                query.Where(
                    p => p.ProductCategories.Any(
                        pc =>
                            pc.CategoryId ==
                            categoryId.Value));
        }

        // --------------------------------------------------------
        // Brand
        // --------------------------------------------------------

        if (brandId.HasValue)
        {
            query =
                query.Where(
                    p => p.BrandId ==
                         brandId.Value);
        }

        // --------------------------------------------------------
        // Price
        // --------------------------------------------------------

        if (minPrice.HasValue)
        {
            query =
                query.Where(
                    p => p.Price >=
                         minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query =
                query.Where(
                    p => p.Price <=
                         maxPrice.Value);
        }

        // --------------------------------------------------------
        // Featured
        // --------------------------------------------------------

        if (isFeatured.HasValue)
        {
            query =
                query.Where(
                    p => p.IsFeatured ==
                         isFeatured.Value);
        }

        // --------------------------------------------------------
        // Stock
        // --------------------------------------------------------

        if (isInStock.HasValue)
        {
            query =
                isInStock.Value
                    ? query.Where(
                        p => p.Variants.Any(
                            v =>
                                v.IsActive &&
                                v.StockQuantity > 0))

                    : query.Where(
                        p => !p.Variants.Any(
                            v =>
                                v.IsActive &&
                                v.StockQuantity > 0));
        }

        // --------------------------------------------------------
        // Search
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term =
                search.Trim();

            query =
                query.Where(
                    p =>
                        EF.Functions.Like(
                            p.Name,
                            $"%{term}%") ||

                        EF.Functions.Like(
                            p.Description ??
                            string.Empty,
                            $"%{term}%") ||

                        EF.Functions.Like(
                            p.Sku,
                            $"%{term}%"));
        }

        // --------------------------------------------------------
        // Sort
        // --------------------------------------------------------

        query =
            sortBy?
                .Trim()
                .ToLowerInvariant() switch
            {
                "price_asc" =>
                    desc
                        ? query.OrderByDescending(
                            p => p.Price)
                        : query.OrderBy(
                            p => p.Price),

                "price_desc" =>
                    desc
                        ? query.OrderBy(
                            p => p.Price)
                        : query.OrderByDescending(
                            p => p.Price),

                "name" =>
                    desc
                        ? query.OrderByDescending(
                            p => p.Name)
                        : query.OrderBy(
                            p => p.Name),

                "popular" =>
                    desc
                        ? query.OrderBy(
                            p =>
                                p.Reviews.Count(
                                    r => r.IsApproved))
                        : query.OrderByDescending(
                            p =>
                                p.Reviews.Count(
                                    r => r.IsApproved)),

                _ =>
                    desc
                        ? query.OrderBy(
                            p => p.CreatedAt)
                        : query.OrderByDescending(
                            p => p.CreatedAt)
            };

        // --------------------------------------------------------
        // Total
        // --------------------------------------------------------

        var totalItems =
            await query.CountAsync(
                cancellationToken);

        // --------------------------------------------------------
        // Page
        // --------------------------------------------------------

        var items =
            await query

                .Include(p => p.Images)

                .Include(p => p.Brand)

                .Include(p => p.Manufacturer)

                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)

                .Include(p => p.Variants)

                .Include(p => p.Reviews)

                .Skip(
                    (page - 1) *
                    pageSize)

                .Take(pageSize)

                .ToListAsync(
                    cancellationToken);

        return (
            items,
            totalItems);
    }

    // ============================================================
    // Add
    // ============================================================

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(
            product,
            cancellationToken);
    }

    // ============================================================
    // Update
    // ============================================================

    public void Update(
        Product product)
    {
        _context.Products.Update(
            product);
    }

    // ============================================================
    // Delete - Soft Delete
    // ============================================================

    public void Delete(
        Product product)
    {
        product.IsDeleted = true;
        product.DeletedAt =
            DateTime.UtcNow;

        _context.Products.Update(
            product);
    }
}