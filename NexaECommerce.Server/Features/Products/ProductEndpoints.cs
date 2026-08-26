using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Products;

public sealed class ProductEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/", List).AllowAnonymous();
        group.MapGet("/admin", AdminList)
            .RequirePermission(ProductPermissions.Read);
        group.MapGet("/category/{categoryId:guid}", GetByCategory).AllowAnonymous();
        group.MapGet("/search", Search).AllowAnonymous();
        group.MapGet("/featured", GetFeatured).AllowAnonymous();
        group.MapGet("/{id:guid}", Get).AllowAnonymous();

        group.MapPost("/", Create)
            .RequirePermission(ProductPermissions.Create)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPut("/{id:guid}", Update)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPatch("/{id:guid}/stock", UpdateStock)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPatch("/{id:guid}/active", SetActive)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPatch("/{id:guid}/featured", SetFeatured)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete("/{id:guid}", Delete)
            .RequirePermission(ProductPermissions.Delete)
            .AddEndpointFilter<TransactionFilter>();
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
        CancellationToken ct = default)
    {
        if (!TryParseOptionalGuid(categoryId, out var categoryGuid))
            return Results.BadRequest(new { error = "Invalid categoryId." });

        if (!TryParseOptionalGuid(brandId, out var brandGuid))
            return Results.BadRequest(new { error = "Invalid brandId." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await productService.GetPagedAsync(
            page, pageSize, search, categoryGuid, brandGuid,
            minPrice, maxPrice, isFeatured, isInStock, isActive, isPublished: true,
            includeInactive: false, includeUnpublished: false,
            sortBy: sortBy, desc: desc, cancellationToken: ct);

        var items = result.Items.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            sku = p.Sku,
            slug = p.Slug,
            price = p.Price,
            comparePrice = p.ComparePrice,
            finalPrice = p.FinalPrice,
            discountPercentage = p.DiscountPercentage,
            currency = p.Currency,
            brandId = p.BrandId,
            brandName = p.BrandName,
            isActive = p.IsActive,
            isFeatured = p.IsFeatured,
            isPublished = p.IsPublished,
            isInStock = p.IsInStock,
            stockQuantity = p.StockQuantity,
            mainImage = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                        ?? p.Images.FirstOrDefault()?.ImageUrl,
            categoryNames = p.Categories,
            categoryIds = p.CategoryIds,
            createdAt = p.CreatedAt
        }).ToList();

        return Results.Ok(new
        {
            items,
            total = result.TotalItems,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
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
        CancellationToken ct = default)
    {
        if (!TryParseOptionalGuid(categoryId, out var categoryGuid))
            return Results.BadRequest(new { error = "Invalid categoryId." });

        if (!TryParseOptionalGuid(brandId, out var brandGuid))
            return Results.BadRequest(new { error = "Invalid brandId." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await productService.GetPagedAsync(
            page, pageSize, search, categoryGuid, brandGuid,
            minPrice, maxPrice, isFeatured, isInStock, isActive, isPublished,
            includeInactive: true, includeUnpublished: true,
            sortBy: sortBy, desc: desc, cancellationToken: ct);

        var items = result.Items.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            sku = p.Sku,
            slug = p.Slug,
            price = p.Price,
            comparePrice = p.ComparePrice,
            finalPrice = p.FinalPrice,
            discountPercentage = p.DiscountPercentage,
            currency = p.Currency,
            brandId = p.BrandId,
            brandName = p.BrandName,
            isActive = p.IsActive,
            isFeatured = p.IsFeatured,
            isPublished = p.IsPublished,
            isInStock = p.IsInStock,
            stockQuantity = p.StockQuantity,
            mainImage = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                        ?? p.Images.FirstOrDefault()?.ImageUrl,
            categoryNames = p.Categories,
            categoryIds = p.CategoryIds,
            createdAt = p.CreatedAt
        }).ToList();

        return Results.Ok(new
        {
            items,
            total = result.TotalItems,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
        });
    }

    private static async Task<IResult> Get(
        Guid id,
        IProductService productService,
        CancellationToken ct)
    {
        var product = await productService.GetByIdAsync(id, ct);
        return product is null
            ? Results.NotFound(new { error = "Product not found." })
            : Results.Ok(product);
    }

    private static async Task<IResult> GetByCategory(
        Guid categoryId,
        IProductService productService,
        CancellationToken ct)
    {
        var products = await productService.GetByCategoryAsync(categoryId, ct);
        return Results.Ok(products);
    }

    private static async Task<IResult> Search(
        [FromQuery] string? q,
        IProductService productService,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Results.Ok(Array.Empty<ProductDto>());

        var products = await productService.SearchAsync(q, ct);
        return Results.Ok(products);
    }

    private static async Task<IResult> GetFeatured(
        [FromQuery] int count = 8,
        IProductService productService = null!,
        CancellationToken ct = default)
    {
        var products = await productService.GetFeaturedAsync(Math.Clamp(count, 1, 50), ct);
        return Results.Ok(products);
    }

    private static async Task<IResult> Create(
        [FromBody] CreateProductDto request,
        IProductService productService,
        CancellationToken ct)
    {
        try
        {
            var product = await productService.CreateAsync(request, ct);
            return Results.Created($"/api/products/{product.Id}", product);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateProductDto request,
        IProductService productService,
        CancellationToken ct)
    {
        try
        {
            await productService.UpdateAsync(id, request, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateStock(
        Guid id,
        [FromBody] UpdateStockRequest request,
        IProductService productService,
        CancellationToken ct)
    {
        try
        {
            await productService.UpdateStockAsync(id, request.Quantity, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> SetActive(
        Guid id,
        [FromBody] SetProductStateRequest request,
        IProductService productService,
        CancellationToken ct)
    {
        try
        {
            await productService.SetActiveAsync(id, request.Value, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> SetFeatured(
        Guid id,
        [FromBody] SetProductStateRequest request,
        IProductService productService,
        CancellationToken ct)
    {
        try
        {
            await productService.SetFeaturedAsync(id, request.Value, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Delete(
        Guid id,
        IProductService productService,
        CancellationToken ct)
    {
        try
        {
            await productService.DeleteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static bool TryParseOptionalGuid(string? value, out Guid? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        if (Guid.TryParse(value, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }
}

public sealed record UpdateStockRequest(int Quantity);
public sealed record SetProductStateRequest(bool Value);
