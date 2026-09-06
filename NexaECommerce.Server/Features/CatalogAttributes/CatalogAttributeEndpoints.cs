using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Application.CatalogAttributes.DTOs;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaECommerce.Server.Features.Products;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.CatalogAttributes;

public sealed class CatalogAttributeEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/catalog/attributes")
                .WithTags("Catalog Attributes");

        group.MapGet("/", List)
            .AllowAnonymous();

        group.MapGet("/{id:guid}", Get)
            .AllowAnonymous();

        group.MapPost("/", Create)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapPut("/{id:guid}", Update)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete("/{id:guid}", Delete)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost(
                "/{attributeId:guid}/values",
                AddValue)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapPut(
                "/{attributeId:guid}/values/{valueId:guid}",
                UpdateValue)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete(
                "/{attributeId:guid}/values/{valueId:guid}",
                DeleteValue)
            .RequirePermission(ProductPermissions.Update)
            .AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> List(
        [FromQuery] string? search,
        ICatalogAttributeService service,
        CancellationToken ct)
    {
        var result =
            await service.GetAllAsync(
                search,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Get(
        Guid id,
        ICatalogAttributeService service,
        CancellationToken ct)
    {
        var result =
            await service.GetByIdAsync(
                id,
                ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> Create(
        [FromBody] CreateCatalogAttributeDto request,
        ICatalogAttributeService service,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.CreateAsync(
                    request,
                    ct);

            return Results.Created(
                $"/api/catalog/attributes/{result.Id}",
                result);
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
        [FromBody] UpdateCatalogAttributeDto request,
        ICatalogAttributeService service,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.UpdateAsync(
                    id,
                    request,
                    ct);

            return result is null
                ? Results.NotFound()
                : Results.Ok(result);
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

    private static async Task<IResult> Delete(
        Guid id,
        ICatalogAttributeService service,
        CancellationToken ct)
    {
        var result =
            await service.DeleteAsync(
                id,
                ct);

        return result
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> AddValue(
        Guid attributeId,
        [FromBody] CreateCatalogAttributeValueDto request,
        ICatalogAttributeService service,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.AddValueAsync(
                    attributeId,
                    request,
                    ct);

            return result is null
                ? Results.NotFound()
                : Results.Created(
                    $"/api/catalog/attributes/{attributeId}/values/{result.Id}",
                    result);
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

    private static async Task<IResult> UpdateValue(
        Guid attributeId,
        Guid valueId,
        [FromBody] UpdateCatalogAttributeValueDto request,
        ICatalogAttributeService service,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.UpdateValueAsync(
                    attributeId,
                    valueId,
                    request,
                    ct);

            return result is null
                ? Results.NotFound()
                : Results.Ok(result);
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

    private static async Task<IResult> DeleteValue(
        Guid attributeId,
        Guid valueId,
        ICatalogAttributeService service,
        CancellationToken ct)
    {
        var result =
            await service.DeleteValueAsync(
                attributeId,
                valueId,
                ct);

        return result
            ? Results.NoContent()
            : Results.NotFound();
    }
}

