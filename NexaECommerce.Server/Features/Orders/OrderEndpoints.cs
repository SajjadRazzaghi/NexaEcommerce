using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaECommerce.Server.Platform.Features;
using NexaEcommerce.SharedKernel.Abstractions;

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
            "/{id:guid}",
            Get);
    }

    private static async Task<IResult> Checkout(
        [FromBody] CheckoutRequest request,
        ICartService cartService,
        CheckoutOrchestrator checkout,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Results.Unauthorized();

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

        /*
         * Never trust checkout line items from the browser.
         *
         * The authoritative quantities come from the current
         * server-side shopping cart.
         */
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

    private static async Task<IResult> Get(
        Guid id,
        NexaEcommerce.Modules.Orders.Application.Services.IOrderService orderService,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Results.Unauthorized();

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
}