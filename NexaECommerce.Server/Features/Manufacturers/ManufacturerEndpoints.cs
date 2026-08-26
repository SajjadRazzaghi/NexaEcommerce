using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Application.Manufacturers.DTOs;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;
using NexaECommerce.Server.Platform.Pagination;

namespace NexaECommerce.Server.Features.Manufacturers;

public sealed class ManufacturerEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/manufacturers")
            .WithTags("Manufacturers");

        group.MapGet("/", List)
            .AllowAnonymous();

        group.MapGet("/lookup", Lookup)
            .AllowAnonymous();

        group.MapGet("/{id:guid}", Get)
            .AllowAnonymous();

        group.MapGet("/slug/{slug}", GetBySlug)
            .AllowAnonymous();

        group.MapPost("/", Create)
            .RequirePermission(
                ManufacturerPermissions.Create)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapPut("/{id:guid}", Update)
            .RequirePermission(
                ManufacturerPermissions.Update)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete("/{id:guid}", Delete)
            .RequirePermission(
                ManufacturerPermissions.Delete)
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/restore", Restore)
            .RequirePermission(
                ManufacturerPermissions.Restore)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/activate", Activate)
            .RequirePermission(
                ManufacturerPermissions.ManageStatus)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/deactivate", Deactivate)
            .RequirePermission(
                ManufacturerPermissions.ManageStatus)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/publish", Publish)
            .RequirePermission(
                ManufacturerPermissions.Publish)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/unpublish", UnPublish)
            .RequirePermission(
                ManufacturerPermissions.Publish)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/feature", Feature)
            .RequirePermission(
                ManufacturerPermissions.Feature)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/{id:guid}/unfeature", UnFeature)
            .RequirePermission(
                ManufacturerPermissions.Feature)
            .AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> List(
        PagedRequest request,
        IManufacturerService service,
        CancellationToken ct)
    {
        var filter = new ManufacturerFilterDto
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Search = request.Search,
            SortBy = ExtractSortField(request.Sort),
            Desc = IsDescending(request.Sort),
            IsActive = GetBoolFilter(request, "isActive"),
            IsPublished =
                GetBoolFilter(request, "isPublished"),
            IsFeatured =
                GetBoolFilter(request, "isFeatured")
        };

        var result =
            await service.GetPagedAsync(
                filter,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Lookup(
        IManufacturerService service,
        CancellationToken ct)
    {
        var result =
            await service.GetLookupAsync(ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Get(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        var result =
            await service.GetByIdAsync(id, ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> GetBySlug(
        string slug,
        IManufacturerService service,
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

    private static async Task<IResult> Create(
        [FromBody] CreateManufacturerDto request,
        IManufacturerService service,
        CancellationToken ct)
    {
        var id =
            await service.CreateAsync(
                request,
                ct);

        return Results.Created(
            $"/api/manufacturers/{id}",
            new { id });
    }

    private static async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateManufacturerDto request,
        IManufacturerService service,
        CancellationToken ct)
    {
        var result =
            await service.UpdateAsync(
                id,
                request,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Delete(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> Restore(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        await service.RestoreAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> Activate(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        await service.ActivateAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> Deactivate(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> Publish(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        await service.PublishAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> UnPublish(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        await service.UnPublishAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> Feature(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        await service.FeatureAsync(id, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> UnFeature(
        Guid id,
        IManufacturerService service,
        CancellationToken ct)
    {
        await service.UnFeatureAsync(id, ct);

        return Results.NoContent();
    }

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

    private static string? ExtractSortField(
        string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return null;

        var first = sort
            .Split(
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

        var first = sort
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(first))
            return false;

        var parts = first.Split(
            ':',
            2,
            StringSplitOptions.TrimEntries);

        return parts.Length > 1 &&
               parts[1].Equals(
                   "desc",
                   StringComparison.OrdinalIgnoreCase);
    }
}