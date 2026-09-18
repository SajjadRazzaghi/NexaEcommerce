using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaECommerce.Server.Features.Orders;

public sealed class FulfillmentEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/fulfillment")
                .WithTags("Fulfillment")
                .AddEndpointFilter<PerformanceFilter>();

        group.MapGet(
                "/queue",
                GetQueue)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapGet(
                "/orders/{orderId:guid}",
                GetByOrder)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
                "/orders/{orderId:guid}",
                Create)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPut(
                "/orders/{orderId:guid}/warehouse",
                AssignWarehouse)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
                "/orders/{orderId:guid}/start-picking",
                StartPicking)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
                "/orders/{orderId:guid}/picked",
                MarkPicked)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
                "/orders/{orderId:guid}/start-packing",
                StartPacking)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
                "/orders/{orderId:guid}/packed",
                MarkPacked)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
                "/orders/{orderId:guid}/ready-to-ship",
                MarkReadyToShip)
            .RequirePermission(
                OrderPermissions.Manage);
    }

    private static async Task<IResult> GetQueue(
        int skip,
        int take,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetQueueAsync(
                    tenant.Id,
                    skip,
                    take,
                    ct);

            return Results.Ok(
                new
                {
                    skip,
                    take = Math.Min(take, 200),
                    items = result
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

    private static async Task<IResult> GetByOrder(
        Guid orderId,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetByOrderAsync(
                    tenant.Id,
                    orderId,
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

    private static async Task<IResult> Create(
        Guid orderId,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.CreateForOrderAsync(
                    tenant.Id,
                    orderId,
                    ct);

            return Results.Created(
                $"/api/fulfillment/orders/{orderId}",
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

    private static async Task<IResult> AssignWarehouse(
        Guid orderId,
        [FromBody] AssignFulfillmentWarehouseRequest request,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.AssignWarehouseAsync(
                    tenant.Id,
                    orderId,
                    request.WarehouseId,
                    request.PickingLocationId,
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

    private static Task<IResult> StartPicking(
        Guid orderId,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        return Transition(
            () =>
                service.StartPickingAsync(
                    tenant.Id,
                    orderId,
                    ct));
    }

    private static Task<IResult> MarkPicked(
        Guid orderId,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        return Transition(
            () =>
                service.MarkPickedAsync(
                    tenant.Id,
                    orderId,
                    ct));
    }

    private static Task<IResult> StartPacking(
        Guid orderId,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        return Transition(
            () =>
                service.StartPackingAsync(
                    tenant.Id,
                    orderId,
                    ct));
    }

    private static Task<IResult> MarkPacked(
        Guid orderId,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        return Transition(
            () =>
                service.MarkPackedAsync(
                    tenant.Id,
                    orderId,
                    ct));
    }

    private static Task<IResult> MarkReadyToShip(
        Guid orderId,
        IFulfillmentService service,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        return Transition(
            () =>
                service.MarkReadyToShipAsync(
                    tenant.Id,
                    orderId,
                    ct));
    }

    private static async Task<IResult> Transition(
        Func<Task<FulfillmentDto>> action)
    {
        try
        {
            var result =
                await action();

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
