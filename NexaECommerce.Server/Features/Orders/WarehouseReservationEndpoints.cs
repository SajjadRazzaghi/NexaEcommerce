using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaECommerce.Server.Features.Orders;

public sealed class WarehouseReservationEndpoints
    : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/fulfillment")
                .WithTags("Fulfillment Warehouse Reservation")
                .AddEndpointFilter<PerformanceFilter>();

        group.MapPost(
                "/orders/{orderId:guid}/reserve-stock",
                Reserve)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapDelete(
                "/orders/{orderId:guid}/reserve-stock",
                Release)
            .RequirePermission(
                OrderPermissions.Manage);
    }

    private static async Task<IResult> Reserve(
        Guid orderId,
        WarehouseReservationOrchestrator orchestrator,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.ReserveAsync(
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

    private static async Task<IResult> Release(
        Guid orderId,
        WarehouseReservationOrchestrator orchestrator,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.ReleaseAsync(
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