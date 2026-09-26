using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using System.Security.Claims;

namespace NexaECommerce.Server.Features.Orders;

public sealed class ShipmentEndpoints
    : IFeatureEndpoints
{
    public void Map(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/orders")
                .WithTags("Shipments")
                .RequireAuthorization();

        group.MapGet(
            "/{orderId:guid}/shipment",
            GetShipment);
        group.MapGet(
        "/admin/{orderId:guid}/shipment",
        GetAdminShipment)
    .RequirePermission(OrderPermissions.Manage);
        group.MapPost(
            "/{orderId:guid}/shipment",
            CreateShipment)
            .RequirePermission(OrderPermissions.Manage);

        group.MapPut(
            "/{orderId:guid}/shipment/tracking",
            SetTrackingNumber)
            .RequirePermission(OrderPermissions.Manage);

        group.MapPost(
            "/{orderId:guid}/shipment/ship",
            Ship)
            .RequirePermission(OrderPermissions.UpdateStatus);

        group.MapPost(
            "/{orderId:guid}/shipment/deliver",
            Deliver)
            .RequirePermission(OrderPermissions.UpdateStatus);
    }
    private static async Task<IResult>
    GetAdminShipment(
        Guid orderId,
        [FromServices]
        IShipmentService shipments,
        [FromServices]
        IFulfillmentService fulfillments,
        [FromServices]
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var fulfillment =
                await fulfillments.GetByOrderAsync(
                    tenant.Id,
                    orderId,
                    ct);

            if (fulfillment is null)
            {
                return Results.NotFound(
                    new
                    {
                        error =
                            "Fulfillment was not found for this order."
                    });
            }

            /*
             * This endpoint is for administrators.
             *
             * ReadyToShip orders should already have a Shipment.
             * If this is an older/partial record and the Shipment
             * is missing, repair it here from the persisted shipping
             * method selected during checkout.
             */
            var result =
                await shipments.GetByOrderAsync(
                    tenant.Id,
                    orderId,
                    null,
                    ct);

            if (result is null &&
                string.Equals(
                    fulfillment.Status,
                    "ReadyToShip",
                    StringComparison.Ordinal))
            {
                result =
                    await shipments.PrepareAsync(
                        tenant.Id,
                        orderId,
                        ct);
            }

            return result is null
                ? Results.NotFound()
                : Results.Ok(result);
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
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }
    private static async Task<IResult>
        GetShipment(
            Guid orderId,
            [FromServices]
            IShipmentService shipments,
            [FromServices]
            ICurrentTenant tenant,
            HttpContext http,
            CancellationToken ct)
    {
        var userId =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var result =
            await shipments.GetByOrderAsync(
                tenant.Id,
                orderId,
                userId,
                ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult>
        CreateShipment(
            Guid orderId,
            [FromBody]
            CreateShipmentRequest request,
            [FromServices]
            IShipmentService shipments,
            [FromServices]
            ICurrentTenant tenant,
            CancellationToken ct)
    {
        try
        {
            if (request.OrderId != Guid.Empty &&
                request.OrderId != orderId)
            {
                return Results.BadRequest(
                    new
                    {
                        error =
                            "Order id in route and body do not match."
                    });
            }

            var result =
                await shipments.CreateAsync(
                    tenant.Id,
                    orderId,
                    request.ShippingMethod,
                    request.Carrier,
                    request.TrackingNumber,
                    ct);

            return Results.Created(
                $"/api/orders/{orderId}/shipment",
                result);
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
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult>
        SetTrackingNumber(
            Guid orderId,
            [FromBody]
            UpdateTrackingNumberRequest request,
            [FromServices]
            IShipmentService shipments,
            [FromServices]
            ICurrentTenant tenant,
            CancellationToken ct)
    {
        try
        {
            var result =
                await shipments.SetTrackingNumberAsync(
                    tenant.Id,
                    orderId,
                    request.TrackingNumber,
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


private static async Task<IResult>
    Ship(
        Guid orderId,
        [FromServices]
        WarehouseShipmentOrchestrator orchestrator,
        [FromServices]
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.ShipAsync(
                    tenant.Id,
                    orderId,
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



   
private static async Task<IResult>
    Deliver(
        Guid orderId,
        [FromServices]
        WarehouseShipmentOrchestrator orchestrator,
        [FromServices]
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orchestrator.DeliverAsync(
                    tenant.Id,
                    orderId,
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