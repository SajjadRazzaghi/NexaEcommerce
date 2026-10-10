using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
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
    // ============================================================
    // Update Existing Variant
    // ============================================================
    //
    // ProductVariant does not currently use a concurrency token.
    // Existing variants are loaded as tracked entities, but updating
    // them through normal EF tracking has produced zero-row UPDATE
    // concurrency exceptions in the product aggregate.
    //
    // ExecuteUpdate performs the scalar update directly in SQL.
    // The tracked entity is then marked Unchanged so SaveChanges()
    // does not issue the same UPDATE again.
    //
    public async Task DeleteNonVariantProductAttributesAsync(
    Guid productId,
    IReadOnlyCollection<string> variantAttributeCodes,
    CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return;
        }

        var query =
            _context.ProductAttributes
                .IgnoreQueryFilters()
                .Where(
                    x =>
                        x.ProductId ==
                        productId);

        if (variantAttributeCodes.Count > 0)
        {
            var normalizedCodes =
                variantAttributeCodes
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .Select(
                        x =>
                            x.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            if (normalizedCodes.Length > 0)
            {
                query =
                    query.Where(
                        x =>
                            !normalizedCodes.Contains(
                                x.Code));
            }
        }

        await query.ExecuteDeleteAsync(
            cancellationToken);
    }
    public async Task RepairTrackedProductAttributeStatesAsync(
      CancellationToken cancellationToken = default)
    {
        _context.ChangeTracker.DetectChanges();

        // ============================================================
        // ProductAttribute
        // ============================================================

        var productAttributeEntries =
            _context.ChangeTracker
                .Entries<ProductAttribute>()
                .Where(
                    entry =>
                        entry.State != EntityState.Detached)
                .ToList();

        var productAttributeIds =
            productAttributeEntries
                .Select(
                    entry =>
                        entry.Entity.Id)
                .Where(
                    id =>
                        id != Guid.Empty)
                .Distinct()
                .ToArray();

        var existingProductAttributeIds =
            productAttributeIds.Length == 0
                ? new HashSet<Guid>()
                : (
                    await _context.ProductAttributes
                        .IgnoreQueryFilters()
                        .Where(
                            attribute =>
                                productAttributeIds.Contains(
                                    attribute.Id))
                        .Select(
                            attribute =>
                                attribute.Id)
                        .ToListAsync(
                            cancellationToken)
                  ).ToHashSet();

        foreach (var entry in productAttributeEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // --------------------------------------------------------
            // New entity: leave it Added.
            // --------------------------------------------------------

            if (entry.State == EntityState.Added)
            {
                continue;
            }

            var id =
                entry.Entity.Id;

            var existsInDatabase =
                existingProductAttributeIds.Contains(
                    id);

            // --------------------------------------------------------
            // Deleted entity which is already gone.
            // --------------------------------------------------------

            if (entry.State == EntityState.Deleted)
            {
                if (!existsInDatabase)
                {
                    entry.State =
                        EntityState.Detached;
                }

                continue;
            }

            // --------------------------------------------------------
            // Only repair Modified entities.
            // --------------------------------------------------------

            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var hasActualChanges =
                entry.Properties.Any(
                    property =>
                        !Equals(
                            property.CurrentValue,
                            property.OriginalValue));

            // --------------------------------------------------------
            // Existing database row but nothing actually changed.
            // --------------------------------------------------------

            if (!hasActualChanges)
            {
                entry.State =
                    existsInDatabase
                        ? EntityState.Unchanged
                        : EntityState.Added;

                continue;
            }

            // --------------------------------------------------------
            // Object has changes but its row does not exist.
            //
            // This is a newly-created entity which was incorrectly
            // promoted to Modified.
            // --------------------------------------------------------

            if (!existsInDatabase)
            {
                entry.State =
                    EntityState.Added;
            }
        }

        // ============================================================
        // AttributeValue
        // ============================================================

        var attributeValueEntries =
            _context.ChangeTracker
                .Entries<AttributeValue>()
                .Where(
                    entry =>
                        entry.State != EntityState.Detached)
                .ToList();

        var attributeValueIds =
            attributeValueEntries
                .Select(
                    entry =>
                        entry.Entity.Id)
                .Where(
                    id =>
                        id != Guid.Empty)
                .Distinct()
                .ToArray();

        var existingAttributeValueIds =
            attributeValueIds.Length == 0
                ? new HashSet<Guid>()
                : (
                    await _context.AttributeValues
                        .IgnoreQueryFilters()
                        .Where(
                            value =>
                                attributeValueIds.Contains(
                                    value.Id))
                        .Select(
                            value =>
                                value.Id)
                        .ToListAsync(
                            cancellationToken)
                  ).ToHashSet();

        foreach (var entry in attributeValueEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // --------------------------------------------------------
            // New entity: leave it Added.
            // --------------------------------------------------------

            if (entry.State == EntityState.Added)
            {
                continue;
            }

            var id =
                entry.Entity.Id;

            var existsInDatabase =
                existingAttributeValueIds.Contains(
                    id);

            // --------------------------------------------------------
            // Already deleted from DB.
            // --------------------------------------------------------

            if (entry.State == EntityState.Deleted)
            {
                if (!existsInDatabase)
                {
                    entry.State =
                        EntityState.Detached;
                }

                continue;
            }

            // --------------------------------------------------------
            // Only repair Modified entities.
            // --------------------------------------------------------

            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var hasActualChanges =
                entry.Properties.Any(
                    property =>
                        !Equals(
                            property.CurrentValue,
                            property.OriginalValue));

            // --------------------------------------------------------
            // Existing row with no actual changes.
            // --------------------------------------------------------

            if (!hasActualChanges)
            {
                entry.State =
                    existsInDatabase
                        ? EntityState.Unchanged
                        : EntityState.Added;

                continue;
            }

            // --------------------------------------------------------
            // New row incorrectly marked Modified.
            // --------------------------------------------------------

            if (!existsInDatabase)
            {
                entry.State =
                    EntityState.Added;
            }
        }
    }
    public void NormalizeTrackedProductAttributeStates()
    {
        _context.ChangeTracker.DetectChanges();

        foreach (var entry in
                 _context.ChangeTracker
                     .Entries<ProductAttribute>()
                     .ToList())
        {
            /*
             * Existing ProductAttribute rows are metadata.
             * They are never updated by the Variant synchronization phase.
             *
             * Only newly-created attributes must remain Added.
             */
            if (entry.State != EntityState.Added)
            {
                entry.State = EntityState.Unchanged;
            }
        }

        foreach (var entry in
                 _context.ChangeTracker
                     .Entries<AttributeValue>()
                     .ToList())
        {
            /*
             * Existing AttributeValue rows are also metadata.
             * They must never reach the final SaveChanges as Modified.
             *
             * Newly-created values must remain Added so EF can INSERT them.
             */
            if (entry.State != EntityState.Added)
            {
                entry.State = EntityState.Unchanged;
            }
        }
    }
    public async Task<bool> UpdateVariantAsync(
    Guid variantId,
    string sku,
    decimal priceOverride,
    decimal? comparePrice,
    bool isActive,
    CancellationToken cancellationToken = default)
    {
        if (variantId == Guid.Empty)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException(
                "Variant SKU is required.",
                nameof(sku));
        }

        if (priceOverride < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(priceOverride));
        }

        var normalizedSku =
            sku.Trim();

        var now =
            DateTime.UtcNow;

        var affectedRows =
            await _context.ProductVariants
                .IgnoreQueryFilters()
                .Where(
                    variant =>
                        variant.Id == variantId &&
                        !variant.IsDeleted)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                variant => variant.Sku,
                                normalizedSku)
                            .SetProperty(
                                variant => variant.PriceOverride,
                                priceOverride)
                            .SetProperty(
                                variant => variant.ComparePrice,
                                comparePrice)
                            .SetProperty(
                                variant => variant.IsActive,
                                isActive)
                            .SetProperty(
                                variant => variant.UpdatedAt,
                                now),
                    cancellationToken);

        var trackedEntry =
            _context.ChangeTracker
                .Entries<ProductVariant>()
                .FirstOrDefault(
                    entry =>
                        entry.Entity.Id ==
                        variantId);

        if (trackedEntry is not null)
        {
            if (affectedRows == 1)
            {
                /*
                 * The SQL UPDATE has already persisted the values.
                 * Do not let SaveChanges() issue another UPDATE.
                 */
                trackedEntry.State =
                    EntityState.Unchanged;
            }
            else
            {
                /*
                 * The row does not exist anymore.
                 * Remove the stale tracked entity.
                 */
                trackedEntry.State =
                    EntityState.Detached;
            }
        }

        return affectedRows == 1;
    }


  public void NormalizeTrackedProductVariantStates()
    {
        _context.ChangeTracker.DetectChanges();

        foreach (var entry in
                 _context.ChangeTracker
                     .Entries<ProductVariant>()
                     .ToList())
        {
            /*
             * Existing ProductVariant scalar fields are persisted
             * explicitly through ExecuteUpdateAsync().
             *
             * Therefore an existing variant must not remain Modified
             * when the final SaveChanges() is executed.
             *
             * New variants must remain Added so EF Core can INSERT them.
             */
            if (entry.State == EntityState.Modified)
            {
                entry.State =
                    EntityState.Unchanged;
            }
        }
    }


    public void DetachTrackedVariantAttributeMappings()
    {
        _context.ChangeTracker.DetectChanges();

        foreach (var entry in
                 _context.ChangeTracker
                     .Entries<VariantAttributeValue>()
                     .ToList())
        {
            /*
             * Existing VAV rows are synchronized explicitly with
             * ExecuteDelete/ExecuteUpdate-style operations.
             *
             * They must never be persisted as Modified/Deleted
             * by the final SaveChanges().
             *
             * Keep Added entities because new mappings are created
             * through the aggregate and must be inserted by SaveChanges().
             */
            if (entry.State != EntityState.Added)
            {
                entry.State =
                    EntityState.Detached;
            }
        }
    }
    public async Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
     .AsSplitQuery()

     .Include(x => x.ProductCategories)
    .ThenInclude(x => x.Category)

.Include(x => x.Images)

.Include(x => x.Reviews)

.Include(x => x.Brand)

.Include(x => x.Manufacturer)

.Include(x => x.Attributes)
    .ThenInclude(x => x.CatalogAttribute)

.Include(x => x.Attributes)
    .ThenInclude(x => x.Values)
        .ThenInclude(x => x.CatalogAttributeValue)

.Include(x => x.Variants)
    .ThenInclude(x => x.AttributeValues)
        .ThenInclude(x => x.AttributeValue)
            .ThenInclude(x => x.ProductAttribute)

.Include(x => x.Variants)
    .ThenInclude(x => x.AttributeValues)
        .ThenInclude(x => x.AttributeValue)
            .ThenInclude(x => x.CatalogAttributeValue)

.Include(x => x.Variants)
    .ThenInclude(x => x.Images)

     .FirstOrDefaultAsync(
         p =>
             p.Id == id &&
             !p.IsDeleted,
         cancellationToken);
    }

    // ============================================================
    // Get By Slug
    // ============================================================
    public async Task<bool> ExistsByVariantCombinationAsync(
        Guid productId,
        string combinationKey,
        Guid? excludeVariantId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(combinationKey))
        {
            return false;
        }

        var query =
            _context.ProductVariants
                .AsNoTracking()
                .Where(
                    x =>
                        x.ProductId == productId &&
                        x.CombinationKey == combinationKey &&
                        !x.IsDeleted);

        if (excludeVariantId.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.Id != excludeVariantId.Value);
        }

        return await query.AnyAsync(
            cancellationToken);
    }
    public async Task ReplaceVariantImagesAsync(
     Guid variantId,
     IReadOnlyCollection<string>? imageUrls,
     CancellationToken cancellationToken = default)
    {
        var existingImages =
            await _context.ProductVariantImages
                .Where(
                    x =>
                        x.ProductVariantId ==
                        variantId)
                .ToListAsync(
                    cancellationToken);

        if (existingImages.Count > 0)
        {
            _context.ProductVariantImages.RemoveRange(
                existingImages);
        }

        var urls =
            (imageUrls ?? Array.Empty<string>())
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x))
                .Select(
                    x => x.Trim())
                .ToList();

        if (urls.Count == 0)
        {
            return;
        }

        var newImages =
            urls
                .Select(
                    (url, index) =>
                        new ProductVariantImage(
                            variantId,
                            url,
                            null,
                            index,
                            index == 0))
                .ToList();

        await _context.ProductVariantImages.AddRangeAsync(
            newImages,
            cancellationToken);
    }
    public async Task<Product?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug =
            slug.Trim()
                .ToLowerInvariant();

        return await _context.Products
            .AsNoTracking()
            .AsSplitQuery()

            .Where(
                p =>
                    p.Slug == normalizedSlug &&
                    !p.IsDeleted &&
                    p.IsActive &&
                    p.IsPublished)

         .Include(x => x.ProductCategories)
    .ThenInclude(x => x.Category)

.Include(x => x.Images)

.Include(x => x.Reviews)

.Include(x => x.Brand)

.Include(x => x.Manufacturer)

.Include(x => x.Attributes)
    .ThenInclude(x => x.CatalogAttribute)

.Include(x => x.Attributes)
    .ThenInclude(x => x.Values)
        .ThenInclude(x => x.CatalogAttributeValue)

.Include(x => x.Variants)
    .ThenInclude(x => x.AttributeValues)
        .ThenInclude(x => x.AttributeValue)
            .ThenInclude(x => x.ProductAttribute)

.Include(x => x.Variants)
    .ThenInclude(x => x.AttributeValues)
        .ThenInclude(x => x.AttributeValue)
            .ThenInclude(x => x.CatalogAttributeValue)

.Include(x => x.Variants)
    .ThenInclude(x => x.Images)

            .FirstOrDefaultAsync(
                cancellationToken);
    }

    // ============================================================
    //product SKU Exists
    // ============================================================

    public async Task<bool> ExistsBySkuAsync(
        string sku,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return false;
        }

        var normalizedSku =
            sku.Trim();

        return await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                p =>
                    p.Sku == normalizedSku &&
                    (
                        !excludeId.HasValue ||
                        p.Id != excludeId.Value
                    ),
                cancellationToken);
    }

    // ============================================================
    // Variant SKU Exists
    // ============================================================

    public async Task<bool> ExistsByVariantSkuAsync(
        string sku,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return false;
        }

        var normalizedSku =
            sku.Trim();

        return await _context
            .Set<ProductVariant>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                v =>
                    v.Sku == normalizedSku &&
                    (
                        !excludeId.HasValue ||
                        v.Id != excludeId.Value
                    ),
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
        {
            return false;
        }

        var normalizedSlug =
            slug.Trim()
                .ToLowerInvariant();

        return await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                p =>
                    p.Slug == normalizedSlug &&
                    (
                        !excludeId.HasValue ||
                        p.Id != excludeId.Value
                    ),
                cancellationToken);
    }

    // ============================================================
    // Get All
    // ============================================================

    public async Task<IEnumerable<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .AsSplitQuery()

            .Where(
                p =>
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
            .AsSplitQuery()

            .Where(
                p =>
                    !p.IsDeleted &&
                    p.IsActive &&
                    p.IsPublished &&
                    p.ProductCategories.Any(
                        pc =>
                            pc.CategoryId ==
                            categoryId))

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
        {
            return await GetAllAsync(
                cancellationToken);
        }

        var term =
            searchTerm.Trim();

        return await _context.Products
            .AsNoTracking()
            .AsSplitQuery()

            .Where(
                p =>
                    !p.IsDeleted &&
                    p.IsActive &&
                    p.IsPublished)

            .Where(
                p =>
                    EF.Functions.Like(
                        p.Name,
                        $"%{term}%")

                    ||

                    EF.Functions.Like(
                        p.Description ??
                        string.Empty,
                        $"%{term}%")

                    ||

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
            .AsSplitQuery()

            .Where(
                p =>
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
                .Where(
                    p =>
                        !p.IsDeleted);

        // --------------------------------------------------------
        // Visibility
        // --------------------------------------------------------

        if (!includeInactive)
        {
            query =
                query.Where(
                    p =>
                        p.IsActive);
        }

        if (!includeUnpublished)
        {
            query =
                query.Where(
                    p =>
                        p.IsPublished);
        }

        // --------------------------------------------------------
        // Status
        // --------------------------------------------------------

        if (isActive.HasValue)
        {
            query =
                query.Where(
                    p =>
                        p.IsActive ==
                        isActive.Value);
        }

        if (isPublished.HasValue)
        {
            query =
                query.Where(
                    p =>
                        p.IsPublished ==
                        isPublished.Value);
        }

        // --------------------------------------------------------
        // Category
        // --------------------------------------------------------

        if (categoryId.HasValue)
        {
            query =
                query.Where(
                    p =>
                        p.ProductCategories.Any(
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
                    p =>
                        p.BrandId ==
                        brandId.Value);
        }

        // --------------------------------------------------------
        // Price
        // --------------------------------------------------------

        if (minPrice.HasValue)
        {
            query =
                query.Where(
                    p =>
                        p.Price >=
                        minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query =
                query.Where(
                    p =>
                        p.Price <=
                        maxPrice.Value);
        }

        // --------------------------------------------------------
        // Featured
        // --------------------------------------------------------

        if (isFeatured.HasValue)
        {
            query =
                query.Where(
                    p =>
                        p.IsFeatured ==
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
                        p =>
                            p.Variants.Any(
                                v =>
                                    v.IsActive &&
                                    v.StockQuantity > 0))
                    : query.Where(
                        p =>
                            !p.Variants.Any(
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
                            $"%{term}%")

                        ||

                        EF.Functions.Like(
                            p.Description ??
                            string.Empty,
                            $"%{term}%")

                        ||

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
                                    r =>
                                        r.IsApproved))
                        : query.OrderByDescending(
                            p =>
                                p.Reviews.Count(
                                    r =>
                                        r.IsApproved)),

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
                .AsSplitQuery()

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
    public async Task DeleteProductSpecificationAttributesAsync(
        Guid productId,
        IReadOnlyCollection<Guid> protectedAttributeIds,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return;
        }

        var protectedIds =
            (protectedAttributeIds ?? Array.Empty<Guid>())
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

        /*
         * A ProductAttribute can be deleted only when:
         *
         * 1. It is not explicitly protected by the caller.
         * 2. None of its AttributeValues is referenced by an active
         *    Variant belonging to this Product.
         *
         * The second condition is the database-level safety net.
         * It prevents localized/legacy ProductAttribute.Code values
         * such as "سایز" from ever causing a Variant attribute to be
         * deleted accidentally.
         */
        var attributesToDelete =
            await _context.ProductAttributes
                .IgnoreQueryFilters()
                .Where(
                    attribute =>
                        attribute.ProductId == productId &&
                        !protectedIds.Contains(attribute.Id) &&
                        !_context.AttributeValues
                            .Any(
                                value =>
                                    value.ProductAttributeId ==
                                        attribute.Id &&
                                    _context.VariantAttributeValues
                                        .Any(
                                            mapping =>
                                                mapping.AttributeValueId ==
                                                    value.Id &&
                                                _context.ProductVariants
                                                    .Any(
                                                        variant =>
                                                            variant.Id ==
                                                                mapping.ProductVariantId &&
                                                            variant.ProductId ==
                                                                productId))))
                .Select(
                    attribute =>
                        attribute.Id)
                .ToListAsync(
                    cancellationToken);

        if (attributesToDelete.Count == 0)
        {
            return;
        }

        var attributeValuesToDelete =
            await _context.AttributeValues
                .IgnoreQueryFilters()
                .Where(
                    value =>
                        attributesToDelete.Contains(
                            value.ProductAttributeId))
                .Select(
                    value =>
                        value.Id)
                .ToListAsync(
                    cancellationToken);

        // ------------------------------------------------------------
        // Detach tracked entities which will be removed by ExecuteDelete
        // ------------------------------------------------------------

        foreach (var entry in
                 _context.ChangeTracker
                     .Entries<VariantAttributeValue>()
                     .Where(
                         entry =>
                             attributeValuesToDelete.Contains(
                                 entry.Entity.AttributeValueId))
                     .ToList())
        {
            entry.State =
                EntityState.Detached;
        }

        foreach (var entry in
                 _context.ChangeTracker
                     .Entries<AttributeValue>()
                     .Where(
                         entry =>
                             attributesToDelete.Contains(
                                 entry.Entity.ProductAttributeId))
                     .ToList())
        {
            entry.State =
                EntityState.Detached;
        }

        foreach (var entry in
                 _context.ChangeTracker
                     .Entries<ProductAttribute>()
                     .Where(
                         entry =>
                             attributesToDelete.Contains(
                                 entry.Entity.Id))
                     .ToList())
        {
            entry.State =
                EntityState.Detached;
        }

        // ------------------------------------------------------------
        // Delete only true specification attributes
        // ------------------------------------------------------------

        await _context.ProductAttributes
            .IgnoreQueryFilters()
            .Where(
                attribute =>
                    attributesToDelete.Contains(
                        attribute.Id))
            .ExecuteDeleteAsync(
                cancellationToken);
    }
    // ============================================================
    // Delete Variant Attribute Mappings
    // ============================================================
    public async Task ReplaceProductImagesAsync(
Guid productId,
IReadOnlyCollection<string>? imageUrls,
CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
            "Product id is required.",
            nameof(productId));
        }


var normalizedUrls =
    (imageUrls ?? Array.Empty<string>())
        .Select(
            url =>
                url?.Trim())
        .Where(
            url =>
                !string.IsNullOrWhiteSpace(url))
        .Select(
            url =>
                url!)
        .Distinct(
            StringComparer.OrdinalIgnoreCase)
        .ToList();

        // ------------------------------------------------------------
        // Detach previously tracked image entities.
        //
        // Product update loads Images as tracked entities. We replace
        // the database rows directly, so those old tracked entities
        // must not participate in SaveChanges afterwards.
        // ------------------------------------------------------------

        var trackedImages =
            _context.ChangeTracker
                .Entries<ProductImage>()
                .Where(
                    entry =>
                        entry.Entity.ProductId ==
                        productId)
                .ToList();

        foreach (var entry in trackedImages)
        {
            entry.State =
                EntityState.Detached;
        }

        // ------------------------------------------------------------
        // Remove current persisted images.
        // Ignore query filters so even soft-deleted image rows do not
        // remain behind when the desired image collection is replaced.
        // ------------------------------------------------------------

        await _context.ProductImages
            .IgnoreQueryFilters()
            .Where(
                image =>
                    image.ProductId ==
                    productId)
            .ExecuteDeleteAsync(
                cancellationToken);

        // ------------------------------------------------------------
        // No images requested.
        // ------------------------------------------------------------

        if (normalizedUrls.Count == 0)
        {
            return;
        }

        // ------------------------------------------------------------
        // Insert the desired image state.
        // New ProductImage entities receive new GUIDs and therefore
        // cannot collide with the deleted image rows.
        // ------------------------------------------------------------

        var newImages =
            normalizedUrls
                .Select(
                    (url, index) =>
                        new ProductImage(
                            productId,
                            url,
                            null,
                            index,
                            index == 0))
                .ToList();

        await _context.ProductImages.AddRangeAsync(
            newImages,
            cancellationToken);


}

    public async Task DeleteVariantAttributeMappingsAsync(
    Guid variantId,
    IReadOnlyCollection<Guid> attributeValueIds,
    CancellationToken cancellationToken = default)
    {
        if (variantId == Guid.Empty)
        {
            return;
        }


if (attributeValueIds is null ||
    attributeValueIds.Count == 0)
        {
            return;
        }

        var ids =
            attributeValueIds
                .Where(
                    id =>
                        id != Guid.Empty)
                .Distinct()
                .ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        // ------------------------------------------------------------
        // Detach tracked mapping entities first.
        //
        // ProductRepository.GetByIdAsync() loads VariantAttributeValue
        // entities as tracked. We are deleting their database rows
        // directly, so those old tracked instances must not be sent
        // through SaveChanges().
        // ------------------------------------------------------------

        var trackedMappings =
            _context.ChangeTracker
                .Entries<VariantAttributeValue>()
                .Where(
                    entry =>
                        entry.Entity.ProductVariantId ==
                        variantId &&

                        ids.Contains(
                            entry.Entity.AttributeValueId))
                .ToList();

        foreach (var entry in trackedMappings)
        {
            entry.State =
                EntityState.Detached;
        }

        // ------------------------------------------------------------
        // Delete the persisted mapping rows directly.
        // ------------------------------------------------------------

        await _context
            .Set<VariantAttributeValue>()
            .IgnoreQueryFilters()
            .Where(
                x =>
                    x.ProductVariantId ==
                    variantId &&

                    ids.Contains(
                        x.AttributeValueId))
            .ExecuteDeleteAsync(
                cancellationToken);


}


    // ============================================================
    // Add Variant Attribute Mappings
    // ============================================================

    public async Task AddVariantAttributeMappingsAsync(
      Guid variantId,
      IReadOnlyCollection<Guid> attributeValueIds,
      CancellationToken cancellationToken = default)
    {
        if (variantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Variant id is required.",
                nameof(variantId));
        }

        if (attributeValueIds is null ||
            attributeValueIds.Count == 0)
        {
            return;
        }

        var ids =
            attributeValueIds
                .Where(
                    id =>
                        id != Guid.Empty)
                .Distinct()
                .ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        /*
         * ------------------------------------------------------------
         * Verify Variant
         * ------------------------------------------------------------
         *
         * This method is used only for variants that already exist
         * in the database.
         */
        var variantExists =
            await _context
                .Set<ProductVariant>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id ==
                        variantId,
                    cancellationToken);

        if (!variantExists)
        {
            throw new KeyNotFoundException(
                $"Product variant '{variantId}' was not found.");
        }

        /*
         * ------------------------------------------------------------
         * Verify AttributeValue records
         * ------------------------------------------------------------
         */
        var existingAttributeValueIds =
            await _context
                .Set<AttributeValue>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    x =>
                        ids.Contains(
                            x.Id))
                .Select(
                    x =>
                        x.Id)
                .ToListAsync(
                    cancellationToken);

        var missingIds =
            ids
                .Except(
                    existingAttributeValueIds)
                .ToArray();

        if (missingIds.Length > 0)
        {
            throw new KeyNotFoundException(
                $"The following attribute values were not found: {string.Join(", ", missingIds)}");
        }

        /*
         * ------------------------------------------------------------
         * Existing persisted mappings
         * ------------------------------------------------------------
         */
        var existingMappings =
            await _context
                .Set<VariantAttributeValue>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    x =>
                        x.ProductVariantId ==
                        variantId &&

                        ids.Contains(
                            x.AttributeValueId))
                .Select(
                    x =>
                        x.AttributeValueId)
                .ToListAsync(
                    cancellationToken);

        var existingIds =
            existingMappings.ToHashSet();

        /*
         * ------------------------------------------------------------
         * IMPORTANT:
         *
         * ProductService may already have added a new
         * VariantAttributeValue to variant.AttributeValues.
         *
         * That mapping may already be tracked as Added.
         *
         * Do not create a second DB entity for it.
         * ------------------------------------------------------------
         */
        /*
   * ------------------------------------------------------------
   * IMPORTANT:
   *
   * ProductService may already have added a new
   * VariantAttributeValue to variant.AttributeValues.
   *
   * DetectChanges() is required here because the entity may have
   * been added through the aggregate navigation and not yet appear
   * in ChangeTracker.Entries<T>().
   * ------------------------------------------------------------
   */
        _context.ChangeTracker.DetectChanges();

        var trackedAddedIds =
            _context
                .ChangeTracker
                .Entries<VariantAttributeValue>()
                .Where(
                    entry =>
                        entry.State ==
                        EntityState.Added &&
                        entry.Entity.ProductVariantId ==
                        variantId)
                .Select(
                    entry =>
                        entry.Entity.AttributeValueId)
                .ToHashSet();

        var newIds =
            ids
                .Where(
                    id =>
                        !existingIds.Contains(id) &&
                        !trackedAddedIds.Contains(id))
                .ToArray();

        if (newIds.Length == 0)
        {
            return;
        }

        var mappings =
            newIds
                .Select(
                    attributeValueId =>
                        new VariantAttributeValue(
                            variantId,
                            attributeValueId))
                .ToList();

        await _context
            .Set<VariantAttributeValue>()
            .AddRangeAsync(
                mappings,
                cancellationToken);
    }

    // ============================================================
    // Add
    // ============================================================
    // ============================================================
    // Add Variant
    // ============================================================

    public async Task AddVariantAsync(
        ProductVariant variant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            variant);

        await _context.ProductVariants.AddAsync(
            variant,
            cancellationToken);
    }
    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            product);

        await _context.Products.AddAsync(
            product,
            cancellationToken);
    }
    public void ClearTracking()
    {
        _context.ChangeTracker.Clear();
    }
    // ============================================================
    // Update
    // ============================================================

    public void Update(
        Product product)
    {
        /*
         * ProductRepository.GetByIdAsync returns a tracked aggregate.
         *
         * Do NOT call DbSet.Update(product) here.
         *
         * Calling Update() on the entire graph can mark Brand,
         * Manufacturer, Reviews, Images, Variants and their children
         * as Modified. This is especially dangerous for entities that
         * use RowVersion concurrency.
         *
         * EF Core change tracking will detect the actual changes.
         */
    }

    // ============================================================
    // Delete - Soft Delete
    // ============================================================
    public void ResetModifiedAttributeValues()
    {
        foreach (var entry in
                 _context.ChangeTracker
                     .Entries<AttributeValue>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Unchanged;
            }
        }
    }
    public void Delete(
        Product product)
    {
        ArgumentNullException.ThrowIfNull(
            product);

        product.IsDeleted =
            true;

        product.DeletedAt =
            DateTime.UtcNow;

        /*
         * Product is already tracked.
         */
    }
}