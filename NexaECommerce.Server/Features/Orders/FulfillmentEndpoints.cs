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
        group.MapPost(
        "/orders/{orderId:guid}/start",
        StartFulfillment)
    .RequirePermission(
        OrderPermissions.Manage);
        group.MapPut(
                "/orders/{orderId:guid}/warehouse",
                AssignWarehouse)
            .RequirePermission(
                OrderPermissions.Manage);
        group.MapPost(
        "/orders/{orderId:guid}/allocate",
        AllocateWarehouse)
    .RequirePermission(
        OrderPermissions.Manage);
        // ========================================================
        // Picking
        // ========================================================

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

        // ========================================================
        // Packing
        // ========================================================

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

        // ========================================================
        // Ready to ship
        // ========================================================

        group.MapPost(
                "/orders/{orderId:guid}/ready-to-ship",
                MarkReadyToShip)
            .RequirePermission(
                OrderPermissions.Manage);
   
    group.MapPost(
        "/orders/{orderId:guid}/rollback",
        Rollback)
    .RequirePermission(
        OrderPermissions.Manage); }
    // ============================================================
    // Queue
    // ============================================================

    private static async Task<IResult> GetQueue(
        int skip,
        int take,
        [FromServices] IFulfillmentService service,
        [FromServices] ICurrentTenant tenant,
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

    // ============================================================
    // Get fulfillment by order
    // ============================================================
    private static async Task<IResult> Rollback(
    Guid orderId,
    [FromBody]
    FulfillmentRollbackRequest request,
    [FromServices]
    WarehouseFulfillmentRollbackOrchestrator orchestrator,
    [FromServices]
    ICurrentTenant tenant,
    CancellationToken ct)
    {
        try
        {
            var result =
               await orchestrator.ExecuteAsync(
    tenant.Id,
    orderId,
    ct);

            return Results.Ok(
                result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        ex.Message
                });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error =
                        ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error =
                        ex.Message
                });
        }
    }

    public sealed record FulfillmentRollbackRequest(
        string? Reason);
    private static async Task<IResult> GetByOrder(
        Guid orderId,
        [FromServices] IFulfillmentService service,
        [FromServices] ICurrentTenant tenant,
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

    // ============================================================
    // Create fulfillment
    // ============================================================

    private static async Task<IResult> Create(
        Guid orderId,
        [FromServices] IFulfillmentService service,
        [FromServices] ICurrentTenant tenant,
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

    // ============================================================
    // Assign warehouse
    // ============================================================

    private static async Task<IResult> AssignWarehouse(
        Guid orderId,
        [FromBody] AssignFulfillmentWarehouseRequest request,
        [FromServices] IFulfillmentService service,
        [FromServices] ICurrentTenant tenant,
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

    private static async Task<IResult> AllocateWarehouse(
    Guid orderId,
    [FromServices] WarehouseAllocationOrchestrator orchestrator,
    [FromServices] ICurrentTenant tenant,
    CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.AllocateAsync(
                    tenant.Id,
                    orderId,
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
    // ============================================================
    // Start fulfillment
    // ============================================================

    private static async Task<IResult> StartFulfillment(
        Guid orderId,
        [FromServices] OrderFulfillmentOrchestrator orchestrator,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.StartAsync(
                    tenant.Id,
                    orderId,
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
    // ============================================================
    // Start picking
    // ============================================================

    private static async Task<IResult> StartPicking(
        Guid orderId,
        [FromServices] WarehousePickingOrchestrator orchestrator,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.StartAsync(
                    tenant.Id,
                    orderId,
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

    // ============================================================
    // Complete picking
    // ============================================================

    private static async Task<IResult> MarkPicked(
        Guid orderId,
        [FromServices] WarehousePickingOrchestrator orchestrator,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.CompleteAsync(
                    tenant.Id,
                    orderId,
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

    // ============================================================
    // Start packing
    // ============================================================

    private static async Task<IResult> StartPacking(
        Guid orderId,
        [FromServices] WarehousePackingOrchestrator orchestrator,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.StartAsync(
                    tenant.Id,
                    orderId,
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

    // ============================================================
    // Complete packing
    // ============================================================

    private static async Task<IResult> MarkPacked(
        Guid orderId,
        [FromServices] WarehousePackingOrchestrator orchestrator,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.CompleteAsync(
                    tenant.Id,
                    orderId,
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

    // ============================================================
    // Ready to ship
    // ============================================================


private static async Task<IResult> MarkReadyToShip(
    Guid orderId,
    [FromServices] WarehouseReadyToShipOrchestrator orchestrator,
    [FromServices] ICurrentTenant tenant,
    CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.ExecuteAsync(
                    tenant.Id,
                    orderId,
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


    // ============================================================
    // Generic service transition
    // ============================================================

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