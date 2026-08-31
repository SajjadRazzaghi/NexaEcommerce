using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using System.Security.Claims;

namespace NexaECommerce.Server.Features.Orders;

public sealed class OrderEndpoints
    : IFeatureEndpoints
{
    private const string IdempotencyHeader =
        "Idempotency-Key";

    public void Map(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/orders")
                .WithTags("Orders")
                .RequireAuthorization();

        group.MapPost(
            "/checkout",
            Checkout);

        group.MapGet(
            "/",
            ListMine)
            .RequirePermission(OrderPermissions.Read);

        group.MapGet(
            "/admin",
            ListAdmin)
            .RequirePermission(OrderPermissions.Manage);

        group.MapGet(
            "/{id:guid}",
            Get)
            .RequirePermission(OrderPermissions.Read);

        group.MapPut(
            "/{id:guid}/status",
            UpdateStatus)
            .RequirePermission(OrderPermissions.UpdateStatus);

        group.MapPost(
            "/{id:guid}/cancel",
            Cancel);
    }

    private static async Task<IResult> Checkout(
        [FromBody] CheckoutRequest request,
        [FromServices] ICartService cartService,
        [FromServices] CheckoutOrchestrator checkout,
        [FromServices] ICurrentTenant tenant,
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

        if (!http.Request.Headers.TryGetValue(
                IdempotencyHeader,
                out var headerValue))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{IdempotencyHeader} header is required."
                });
        }

        var idempotencyKey =
            headerValue
                .FirstOrDefault()?
                .Trim();

        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{IdempotencyHeader} header cannot be empty."
                });
        }

        if (idempotencyKey.Length > 128)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{IdempotencyHeader} cannot exceed 128 characters."
                });
        }

        var cart =
            await cartService.GetAsync(
                tenant.Id,
                userId,
                null,
                ct);

        if (cart.Items.Count == 0)
        {
            return Results.Conflict(
                new
                {
                    error =
                        "Your shopping cart is empty."
                });
        }

        var serverLines =
            cart.Items
                .Select(
                    item =>
                        new CheckoutLineDto(
                            item.ProductVariantId,
                            item.Quantity))
                .ToList();

        var serverRequest =
            request with
            {
                Items = serverLines
            };

        try
        {
            var order =
                await checkout.ExecuteAsync(
                    tenant.Id,
                    userId,
                    idempotencyKey,
                    serverRequest,
                    ct);

            return Results.Created(
                $"/api/orders/{order.Id}",
                order);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
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
    }

    private static async Task<IResult> ListMine(
        [FromServices] IOrderService orderService,
        [FromServices] ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        string? status = null)
    {
        var userId =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var result =
            await orderService.GetUserOrdersAsync(
                tenant.Id,
                userId,
                page,
                pageSize,
                status,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> ListAdmin(
        [FromServices] IOrderService orderService,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? search = null)
    {
        var result =
            await orderService.GetTenantOrdersAsync(
                tenant.Id,
                page,
                pageSize,
                status,
                search,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Get(
        Guid id,
        [FromServices] IOrderService orderService,
        [FromServices] ICurrentTenant tenant,
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

        var order =
            await orderService.GetAsync(
                tenant.Id,
                id,
                userId,
                ct);

        return order is null
            ? Results.NotFound()
            : Results.Ok(order);
    }

    private static async Task<IResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        [FromServices] IOrderService orderService,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await orderService.UpdateStatusAsync(
                    tenant.Id,
                    id,
                    request.Status,
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

    private static async Task<IResult> Cancel(
        Guid id,
        [FromServices] OrderCancellationOrchestrator cancellation,
        HttpContext http,
        CancellationToken ct,
        [FromServices] ICurrentTenant tenant)
    {
        var userId =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            await cancellation.CancelAsync(
                tenant.Id,
                userId,
                id,
                ct);

            return Results.Ok(
                new
                {
                    message =
                        "Order cancelled successfully."
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
