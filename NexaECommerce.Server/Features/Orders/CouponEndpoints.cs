using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;

namespace NexaECommerce.Server.Features.Orders;

public sealed class CouponEndpoints
    : IFeatureEndpoints
{
    public void Map(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup(
                    "/api/coupons")
                .WithTags(
                    "Coupons")
                .RequireAuthorization();

        group.MapGet(
            "/",
            GetAll)
            .RequirePermission(
                OrderPermissions.ShippingRead);

        group.MapGet(
            "/{id:guid}",
            Get)
            .RequirePermission(
                OrderPermissions.ShippingRead);

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
            "/validate",
            Validate);
    }

    private static async Task<IResult> GetAll(
        [FromServices]
        ICouponService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        return Results.Ok(
            await service.GetAllAsync(
                tenant.Id,
                ct));
    }

    private static async Task<IResult> Get(
        Guid id,

        [FromServices]
        ICouponService service,

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

    private static async Task<IResult> Validate(
        [FromQuery]
        string code,

        [FromQuery]
        decimal orderAmount,

        HttpContext http,

        [FromServices]
        ICouponService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        var userId =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await service.ValidateAsync(
                    tenant.Id,
                    userId,
                    code,
                    orderAmount,
                    ct);

            return Results.Ok(result);
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

    private static async Task<IResult> Create(
        [FromBody]
        CreateCouponRequest request,

        [FromServices]
        ICouponService service,

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
                $"/api/coupons/{result.Id}",
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
        UpdateCouponRequest request,

        [FromServices]
        ICouponService service,

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
        SetCouponActiveRequest request,

        [FromServices]
        ICouponService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        var result =
            await service.SetActiveAsync(
                tenant.Id,
                id,
                request.Active,
                ct);

        return result
            ? Results.Ok()
            : Results.NotFound();
    }

    private static async Task<IResult> Delete(
        Guid id,

        [FromServices]
        ICouponService service,

        [FromServices]
        ICurrentTenant tenant,

        CancellationToken ct)
    {
        var result =
            await service.DeleteAsync(
                tenant.Id,
                id,
                ct);

        return result
            ? Results.NoContent()
            : Results.NotFound();
    }
}

public sealed record SetCouponActiveRequest(
    bool Active);
