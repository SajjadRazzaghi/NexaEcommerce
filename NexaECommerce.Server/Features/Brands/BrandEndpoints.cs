using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Application.Brands.DTOs;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;
using NexaECommerce.Server.Platform.Pagination;

namespace NexaECommerce.Server.Features.Brands;

public sealed class BrandEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/brands")
            .WithTags("Brands");

        // =====================================================
        // Public
        // =====================================================

        group.MapGet("/", List)
            .AllowAnonymous();

        group.MapGet("/lookup", Lookup)
            .AllowAnonymous();

        group.MapGet("/{id:guid}", Get)
            .AllowAnonymous();

        group.MapGet("/slug/{slug}", GetBySlug)
            .AllowAnonymous();

        // =====================================================
        // Admin - CRUD
        // =====================================================

        group.MapPost("/", Create)
            .RequirePermission(BrandPermissions.Create)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapPut("/{id:guid}", Update)
            .RequirePermission(BrandPermissions.Update)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete("/{id:guid}", Delete)
            .RequirePermission(BrandPermissions.Delete)
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        // =====================================================
        // Admin - Restore
        // =====================================================

        group.MapPost("/{id:guid}/restore", Restore)
            .RequirePermission(BrandPermissions.Restore)
            .AddEndpointFilter<TransactionFilter>();

        // =====================================================
        // Admin - Active
        // =====================================================

        group.MapPost("/{id:guid}/activate", Activate)
            .RequirePermission(BrandPermissions.ManageStatus)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/deactivate", Deactivate)
            .RequirePermission(BrandPermissions.ManageStatus)
            .AddEndpointFilter<TransactionFilter>();

        // =====================================================
        // Admin - Publish
        // =====================================================

        group.MapPost("/{id:guid}/publish", Publish)
            .RequirePermission(BrandPermissions.Publish)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/unpublish", UnPublish)
            .RequirePermission(BrandPermissions.Publish)
            .AddEndpointFilter<TransactionFilter>();

        // =====================================================
        // Admin - Featured
        // =====================================================

        group.MapPost("/{id:guid}/feature", Feature)
            .RequirePermission(BrandPermissions.Feature)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/unfeature", UnFeature)
            .RequirePermission(BrandPermissions.Feature)
            .AddEndpointFilter<TransactionFilter>();
    }

    // =========================================================
    // GET /api/brands
    // =========================================================

    private static async Task<IResult> List(
        PagedRequest request,
        IBrandService service,
        CancellationToken ct)
    {
        var filter = new BrandFilterDto
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Search = request.Search,
            SortBy = ExtractSortField(request.Sort),
            Desc = IsDescending(request.Sort),
            IsActive = GetBoolFilter(request, "isActive"),
            IsPublished = GetBoolFilter(request, "isPublished"),
            IsFeatured = GetBoolFilter(request, "isFeatured")
        };

        var result = await service.GetPagedAsync(
            filter,
            ct);

        return Results.Ok(result);
    }

    // =========================================================
    // GET /api/brands/lookup
    // =========================================================

    private static async Task<IResult> Lookup(
        IBrandService service,
        CancellationToken ct)
    {
        var result =
            await service.GetLookupAsync(ct);

        return Results.Ok(result);
    }

    // =========================================================
    // GET /api/brands/{id}
    // =========================================================

    private static async Task<IResult> Get(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        var result =
            await service.GetByIdAsync(id, ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    // =========================================================
    // GET /api/brands/slug/{slug}
    // =========================================================

    private static async Task<IResult> GetBySlug(
        string slug,
        IBrandService service,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Results.BadRequest();

        var result =
            await service.GetBySlugAsync(
                slug,
                ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    // =========================================================
    // POST
    // =========================================================

    private static async Task<IResult> Create(
        [FromBody] CreateBrandDto request,
        IBrandService service,
        CancellationToken ct)
    {
        var id =
            await service.CreateAsync(
                request,
                ct);

        return Results.Created(
            $"/api/brands/{id}",
            new
            {
                id
            });
    }

    // =========================================================
    // PUT
    // =========================================================

    private static async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateBrandDto request,
        IBrandService service,
        CancellationToken ct)
    {
        var result =
            await service.UpdateAsync(
                id,
                request,
                ct);

        return Results.Ok(result);
    }

    // =========================================================
    // DELETE
    // =========================================================

    private static async Task<IResult> Delete(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        await service.DeleteAsync(
            id,
            ct);

        return Results.NoContent();
    }

    // =========================================================
    // RESTORE
    // =========================================================

    private static async Task<IResult> Restore(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        await service.RestoreAsync(
            id,
            ct);

        return Results.NoContent();
    }

    // =========================================================
    // ACTIVE
    // =========================================================

    private static async Task<IResult> Activate(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        await service.ActivateAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> Deactivate(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);

        return Results.NoContent();
    }

    // =========================================================
    // PUBLISH
    // =========================================================

    private static async Task<IResult> Publish(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        await service.PublishAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> UnPublish(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        await service.UnPublishAsync(id, ct);

        return Results.NoContent();
    }

    // =========================================================
    // FEATURE
    // =========================================================

    private static async Task<IResult> Feature(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        await service.FeatureAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> UnFeature(
        Guid id,
        IBrandService service,
        CancellationToken ct)
    {
        await service.UnFeatureAsync(id, ct);

        return Results.NoContent();
    }

    // =========================================================
    // Filters
    // =========================================================

    private static bool? GetBoolFilter(
        PagedRequest request,
        string key)
    {
        if (request.Filters is null)
            return null;

        if (!request.Filters.TryGetValue(
                key,
                out var value))
            return null;

        return bool.TryParse(
            value,
            out var result)
            ? result
            : null;
    }

    // =========================================================
    // Sort
    // =========================================================

    private static string? ExtractSortField(
        string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return null;

        var first =
            sort.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(first))
            return null;

        return first
            .Split(
                ':',
                2,
                StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private static bool IsDescending(
        string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return false;

        var first =
            sort.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(first))
            return false;

        var parts =
            first.Split(
                ':',
                2,
                StringSplitOptions.TrimEntries);

        return parts.Length > 1 &&
               parts[1].Equals(
                   "desc",
                   StringComparison.OrdinalIgnoreCase);
    }
}