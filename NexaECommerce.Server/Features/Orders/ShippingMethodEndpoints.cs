using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;

namespace NexaECommerce.Server.Features.Orders;

public sealed class ShippingMethodEndpoints
    : IFeatureEndpoints
{
    public void Map(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/shipping-methods")
                .WithTags("Shipping Methods")
                .RequireAuthorization();

        group.MapGet(
            "/",
            GetActive);

        group.MapGet(
            "/admin",
            GetAll)
            .RequirePermission(
                OrderPermissions.ShippingRead);

        group.MapGet(
            "/{id:guid}",
            Get);

        group.MapPost(
            "/",
            Create)
            .RequirePermission(
                OrderPermissions.ShippingCreate);

        group.MapPut(
            "/{id:guid}",
            Update)
            .RequirePermission(
                OrderPermissions.ShippingUpdate);

        group.MapPut(
            "/{id:guid}/active",
            SetActive)
            .RequirePermission(
                OrderPermissions.ShippingUpdate);

        group.MapDelete(
            "/{id:guid}",
            Delete)
            .RequirePermission(
                OrderPermissions.ShippingDelete);

        group.MapGet(
            "/{id:guid}/quote",
            Quote);
    }

    private static async Task<IResult> GetActive(
        [FromServices]
        IShippingMethodService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        var result =
            await service.GetActiveAsync(
                tenant.Id,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetAll(
        [FromServices]
        IShippingMethodService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        var result =
            await service.GetAllAsync(
                tenant.Id,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Get(
        Guid id,

        [FromServices]
        IShippingMethodService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        var result =
            await service.GetAsync(
                tenant.Id,
                id,
                ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> Create(
        [FromBody]
        CreateShippingMethodRequest request,

        [FromServices]
        IShippingMethodService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        try
        {
            var result =
                await service.CreateAsync(
                    tenant.Id,
                    request,
                    ct);

            return Results.Created(
                $"/api/shipping-methods/{result.Id}",
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
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Update(
        Guid id,

        [FromBody]
        UpdateShippingMethodRequest request,

        [FromServices]
        IShippingMethodService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        try
        {
            var result =
                await service.UpdateAsync(
                    tenant.Id,
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

    private static async Task<IResult> SetActive(
        Guid id,

        [FromBody]
        SetShippingMethodActiveRequest request,

        [FromServices]
        IShippingMethodService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        try
        {
            var success =
                await service.SetActiveAsync(
                    tenant.Id,
                    id,
                    request.Active,
                    ct);

            return success
                ? Results.Ok()
                : Results.NotFound();
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

        [FromServices]
        IShippingMethodService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        var success =
            await service.DeleteAsync(
                tenant.Id,
                id,
                ct);

        return success
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> Quote(
        Guid id,

        [FromServices]
        IShippingMethodService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        try
        {
            var result =
                await service.QuoteAsync(
                    tenant.Id,
                    id,
                    ct);

            return Results.Ok(result);
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
    }
}

public sealed record SetShippingMethodActiveRequest(
    bool Active);
