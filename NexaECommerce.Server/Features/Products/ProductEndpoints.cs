using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Products;

public sealed class ProductEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/products")
                .WithTags("Products")
                .AddEndpointFilter<ValidationFilter>()
                .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/", List)
            .AllowAnonymous();

        group.MapGet("/admin", AdminList)
            .RequirePermission(ProductPermissions.Read);

        group.MapGet(
                "/category/{categoryId:guid}",
                GetByCategory)
            .AllowAnonymous();

        group.MapGet(
                "/search",
                Search)
            .AllowAnonymous();

        group.MapGet(
                "/featured",
                GetFeatured)
            .AllowAnonymous();

        group.MapGet(
                "/slug/{slug}",
                GetBySlug)
            .AllowAnonymous();

        group.MapGet(
                "/{id:guid}",
                Get)
            .AllowAnonymous();

        group.MapPost(
                "/",
                Create)
            .RequirePermission(ProductPermissions.Create)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPut(
                "/{id:guid}",
                Update)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPatch(
                "/{id:guid}/stock",
                UpdateStock)
            .RequirePermission(ProductPermissions.Update);

        group.MapPatch(
                "/{id:guid}/active",
                SetActive)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPatch(
                "/{id:guid}/featured",
                SetFeatured)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete(
                "/{id:guid}",
                Delete)
            .RequirePermission(ProductPermissions.Delete)
            .AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> GetBySlug(
        string slug,
        [FromServices] IProductService productService,
        [FromServices] IStockReader stockReader,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Results.BadRequest(
                new
                {
                    error = "Product slug is required."
                });
        }

        var product =
            await productService.GetBySlugAsync(
                slug,
                ct);

        if (product is null)
        {
            return Results.NotFound(
                new
                {
                    error = "Product not found."
                });
        }

        await ApplyLiveStockAsync(
            product,
            stockReader,
            currentTenant.Id,
            ct);

        return Results.Ok(
            product);
    }

    private static async Task<IResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] string? brandId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] bool? isInStock = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool desc = false,
        IProductService productService = null!,
        IStockReader stockReader = null!,
        ICurrentTenant currentTenant = null!,
        CancellationToken ct = default)
    {
        if (!TryParseOptionalGuid(
                categoryId,
                out var categoryGuid))
        {
            return Results.BadRequest(
                new
                {
                    error = "Invalid categoryId."
                });
        }

        if (!TryParseOptionalGuid(
                brandId,
                out var brandGuid))
        {
            return Results.BadRequest(
                new
                {
                    error = "Invalid brandId."
                });
        }

        page = Math.Max(
            1,
            page);

        pageSize = Math.Clamp(
            pageSize,
            1,
            100);

        /*
         * Catalog owns product metadata.
         *
         * Inventory owns sellable stock.
         *
         * Do not let the legacy Catalog stock flag hide products
         * before the live Inventory quantities are evaluated.
         */
        var result =
            await productService.GetPagedAsync(
                page,
                pageSize,
                search,
                categoryGuid,
                brandGuid,
                minPrice,
                maxPrice,
                isFeatured,
                isInStock.HasValue
                    ? null
                    : null,
                isActive,
                isPublished: true,
                includeInactive: false,
                includeUnpublished: false,
                sortBy: sortBy,
                desc: desc,
                cancellationToken: ct);

        var variantIds =
            result.Items
                .SelectMany(
                    x => x.Variants)
                .Where(
                    x => x.IsActive)
                .Select(
                    x => x.Id)
                .Distinct()
                .ToArray();

        var stockQuantities =
            variantIds.Length == 0
                ? new Dictionary<Guid, int>()
                : (
                    await stockReader.GetAvailableQuantitiesAsync(
                        currentTenant.Id,
                        variantIds,
                        ct)
                ).ToDictionary(
                    x => x.Key,
                    x => Math.Max(
                        0,
                        x.Value));

        var items =
            result.Items
                .Select(
                    p =>
                    {
                        var stockQuantity =
                            p.Variants
                                .Where(
                                    v => v.IsActive)
                                .Sum(
                                    v =>
                                        stockQuantities.TryGetValue(
                                            v.Id,
                                            out var quantity)
                                            ? quantity
                                            : 0);

                        return new
                        {
                            id = p.Id,
                            name = p.Name,
                            sku = p.Sku,
                            slug = p.Slug,
                            price = p.Price,
                            comparePrice = p.ComparePrice,
                            finalPrice = p.FinalPrice,
                            discountPercentage =
                                p.DiscountPercentage,
                            currency = p.Currency,
                            brandId = p.BrandId,
                            brandName = p.BrandName,
                            isActive = p.IsActive,
                            isFeatured = p.IsFeatured,
                            isPublished = p.IsPublished,
                            isInStock =
                                stockQuantity > 0,
                            stockQuantity,
                            mainImage =
                                p.Images
                                    .FirstOrDefault(
                                        i =>
                                            i.IsPrimary)
                                    ?.ImageUrl
                                ??
                                p.Images
                                    .FirstOrDefault()
                                    ?.ImageUrl,
                            categoryNames =
                                p.Categories,
                            categoryIds =
                                p.CategoryIds,
                            createdAt =
                                p.CreatedAt
                        };
                    })
                .Where(
                    item =>
                        !isInStock.HasValue ||
                        (isInStock.Value
                            ? item.isInStock
                            : !item.isInStock))
                .ToList();

        /*
         * The inventory-aware filter is evaluated after live stock
         * has been calculated.
         *
         * For the normal storefront path this is not relevant unless
         * the user explicitly asks for in-stock/out-of-stock products.
         */
        var filteredTotal =
            isInStock.HasValue
                ? items.Count
                : result.TotalItems;

        var totalPages =
            filteredTotal == 0
                ? 0
                : (int)Math.Ceiling(
                    filteredTotal /
                    (double)pageSize);

        return Results.Ok(
            new
            {
                items,
                total =
                    filteredTotal,
                page =
                    result.Page,
                pageSize =
                    result.PageSize,
                totalPages
            });
    }

    private static async Task<IResult> AdminList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] string? brandId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] bool? isInStock = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] bool desc = false,
        IProductService productService = null!,
        IStockReader stockReader = null!,
        ICurrentTenant currentTenant = null!,
        CancellationToken ct = default)
    {
        if (!TryParseOptionalGuid(
                categoryId,
                out var categoryGuid))
        {
            return Results.BadRequest(
                new
                {
                    error = "Invalid categoryId."
                });
        }

        if (!TryParseOptionalGuid(
                brandId,
                out var brandGuid))
        {
            return Results.BadRequest(
                new
                {
                    error = "Invalid brandId."
                });
        }

        page = Math.Max(
            1,
            page);

        pageSize = Math.Clamp(
            pageSize,
            1,
            100);

        /*
         * Inventory is the source of truth for current stock.
         * The legacy Catalog stock filter must not eliminate products
         * before live stock has been evaluated.
         */
        var result =
            await productService.GetPagedAsync(
                page,
                pageSize,
                search,
                categoryGuid,
                brandGuid,
                minPrice,
                maxPrice,
                isFeatured,
                null,
                isActive,
                isPublished,
                includeInactive: true,
                includeUnpublished: true,
                sortBy: sortBy,
                desc: desc,
                cancellationToken: ct);

        var variantIds =
            result.Items
                .SelectMany(
                    x => x.Variants)
                .Where(
                    x => x.IsActive)
                .Select(
                    x => x.Id)
                .Distinct()
                .ToArray();

        var stockQuantities =
            variantIds.Length == 0
                ? new Dictionary<Guid, int>()
                : (
                    await stockReader.GetAvailableQuantitiesAsync(
                        currentTenant.Id,
                        variantIds,
                        ct)
                ).ToDictionary(
                    x => x.Key,
                    x => Math.Max(
                        0,
                        x.Value));

        var items =
            result.Items
                .Select(
                    p =>
                    {
                        var stockQuantity =
                            p.Variants
                                .Where(
                                    v => v.IsActive)
                                .Sum(
                                    v =>
                                        stockQuantities.TryGetValue(
                                            v.Id,
                                            out var quantity)
                                            ? quantity
                                            : 0);

                        return new
                        {
                            id = p.Id,
                            name = p.Name,
                            sku = p.Sku,
                            slug = p.Slug,
                            price = p.Price,
                            comparePrice = p.ComparePrice,
                            finalPrice = p.FinalPrice,
                            discountPercentage =
                                p.DiscountPercentage,
                            currency = p.Currency,
                            brandId = p.BrandId,
                            brandName = p.BrandName,
                            isActive = p.IsActive,
                            isFeatured = p.IsFeatured,
                            isPublished = p.IsPublished,
                            isInStock =
                                stockQuantity > 0,
                            stockQuantity,
                            mainImage =
                                p.Images
                                    .FirstOrDefault(
                                        i =>
                                            i.IsPrimary)
                                    ?.ImageUrl
                                ??
                                p.Images
                                    .FirstOrDefault()
                                    ?.ImageUrl,
                            categoryNames =
                                p.Categories,
                            categoryIds =
                                p.CategoryIds,
                            createdAt =
                                p.CreatedAt
                        };
                    })
                .Where(
                    item =>
                        !isInStock.HasValue ||
                        (isInStock.Value
                            ? item.isInStock
                            : !item.isInStock))
                .ToList();

        var filteredTotal =
            isInStock.HasValue
                ? items.Count
                : result.TotalItems;

        var totalPages =
            filteredTotal == 0
                ? 0
                : (int)Math.Ceiling(
                    filteredTotal /
                    (double)pageSize);

        return Results.Ok(
            new
            {
                items,
                total =
                    filteredTotal,
                page =
                    result.Page,
                pageSize =
                    result.PageSize,
                totalPages
            });
    }

    private static async Task<IResult> Get(
        Guid id,
        [FromServices] IProductService productService,
        [FromServices] IStockReader stockReader,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        var product =
            await productService.GetByIdAsync(
                id,
                ct);

        if (product is null)
        {
            return Results.NotFound(
                new
                {
                    error = "Product not found."
                });
        }

        await ApplyLiveStockAsync(
            product,
            stockReader,
            currentTenant.Id,
            ct);

        return Results.Ok(
            product);
    }

    private static async Task<IResult> GetByCategory(
        Guid categoryId,
        [FromServices] IProductService productService,
        [FromServices] IStockReader stockReader,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        var products =
            await productService.GetByCategoryAsync(
                categoryId,
                ct);

        await ApplyLiveStockAsync(
            products,
            stockReader,
            currentTenant.Id,
            ct);

        return Results.Ok(
            products);
    }

    private static async Task<IResult> Search(
        [FromQuery] string? q,
        [FromServices] IProductService productService,
        [FromServices] IStockReader stockReader,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.Ok(
                Array.Empty<ProductDto>());
        }

        var products =
            await productService.SearchAsync(
                q,
                ct);

        await ApplyLiveStockAsync(
            products,
            stockReader,
            currentTenant.Id,
            ct);

        return Results.Ok(
            products);
    }

    private static async Task<IResult> GetFeatured(
        [FromQuery] int count = 8,
        [FromServices] IProductService productService = null!,
        [FromServices] IStockReader stockReader = null!,
        [FromServices] ICurrentTenant currentTenant = null!,
        CancellationToken ct = default)
    {
        var products =
            await productService.GetFeaturedAsync(
                Math.Clamp(
                    count,
                    1,
                    50),
                ct);

        await ApplyLiveStockAsync(
            products,
            stockReader,
            currentTenant.Id,
            ct);

        return Results.Ok(
            products);
    }

    private static async Task<IResult> Create(
        [FromBody] CreateProductDto request,
        [FromServices] IProductService productService,
        ProductInventorySynchronizer inventorySynchronizer,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var product =
                await productService.CreateAsync(
                    request,
                    ct);

            await inventorySynchronizer.SyncMissingStockAsync(
                currentTenant.Id,
                product,
                ct);

            return Results.Created(
                $"/api/products/{product.Id}",
                product);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateProductDto request,
        [FromServices] IProductService productService,
        ProductInventorySynchronizer inventorySynchronizer,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            await productService.UpdateAsync(
                id,
                request,
                ct);

            var product =
                await productService.GetByIdAsync(
                    id,
                    ct);

            if (product is not null)
            {
                await inventorySynchronizer.SyncMissingStockAsync(
                    currentTenant.Id,
                    product,
                    ct);
            }

            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> UpdateStock(
        Guid id,
        [FromBody] UpdateStockRequest request,
        [FromServices] IProductService productService,
        [FromServices] IInventoryService inventoryService,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            if (request.Quantity < 0)
            {
                return Results.BadRequest(
                    new
                    {
                        error =
                            "Stock quantity cannot be negative."
                    });
            }

            var product =
                await productService.GetByIdAsync(
                    id,
                    ct);

            if (product is null)
            {
                return Results.NotFound(
                    new
                    {
                        error = "Product not found."
                    });
            }

            var variant =
                product.Variants
                    .FirstOrDefault(
                        x => x.IsActive);

            if (variant is null)
            {
                return Results.Conflict(
                    new
                    {
                        error =
                            "Product does not have an active variant."
                    });
            }

            var stock =
                await inventoryService.SetStockAsync(
                    currentTenant.Id,
                    variant.Id,
                    request.Quantity,
                    ct);

            return Results.Ok(
                stock);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> SetActive(
        Guid id,
        [FromBody] SetProductStateRequest request,
        [FromServices] IProductService productService,
        CancellationToken ct)
    {
        try
        {
            await productService.SetActiveAsync(
                id,
                request.Value,
                ct);

            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> SetFeatured(
        Guid id,
        [FromBody] SetProductStateRequest request,
        [FromServices] IProductService productService,
        CancellationToken ct)
    {
        try
        {
            await productService.SetFeaturedAsync(
                id,
                request.Value,
                ct);

            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Delete(
        Guid id,
        [FromServices] IProductService productService,
        CancellationToken ct)
    {
        try
        {
            await productService.DeleteAsync(
                id,
                ct);

            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task ApplyLiveStockAsync(
        ProductDto product,
        IStockReader stockReader,
        string tenantId,
        CancellationToken cancellationToken)
    {
        await ApplyLiveStockAsync(
            new[]
            {
                product
            },
            stockReader,
            tenantId,
            cancellationToken);
    }

    private static async Task ApplyLiveStockAsync(
        IEnumerable<ProductDto> products,
        IStockReader stockReader,
        string tenantId,
        CancellationToken cancellationToken)
    {
        var productList =
            products.ToList();

        if (productList.Count == 0)
        {
            return;
        }

        var variantIds =
            productList
                .SelectMany(
                    product =>
                        product.Variants ?? new List<ProductVariantDto>())
                .Where(
                    variant =>
                        variant.IsActive)
                .Select(
                    variant =>
                        variant.Id)
                .Distinct()
                .ToArray();

        if (variantIds.Length == 0)
        {
            foreach (var product in productList)
            {
                product.StockQuantity = 0;
                product.IsInStock = false;

                foreach (
                    var variant
                    in product.Variants ??
                       new List<ProductVariantDto>())
                {
                    variant.StockQuantity = 0;
                }
            }

            return;
        }

        var quantities =
            await stockReader.GetAvailableQuantitiesAsync(
                tenantId,
                variantIds,
                cancellationToken);

        foreach (var product in productList)
        {
            var productStock =
                0;

            foreach (
                var variant
                in product.Variants ??
                   new List<ProductVariantDto>())
            {
                if (!variant.IsActive)
                {
                    variant.StockQuantity = 0;
                    continue;
                }

                variant.StockQuantity =
                    quantities.TryGetValue(
                        variant.Id,
                        out var quantity)
                        ? Math.Max(
                            0,
                            quantity)
                        : 0;

                productStock +=
                    variant.StockQuantity;
            }

            product.StockQuantity =
                productStock;

            product.IsInStock =
                productStock > 0;
        }
    }

    private static bool TryParseOptionalGuid(
        string? value,
        out Guid? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        if (Guid.TryParse(
                value,
                out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }
}

public sealed record UpdateStockRequest(
    int Quantity);

public sealed record SetProductStateRequest(
    bool Value);
