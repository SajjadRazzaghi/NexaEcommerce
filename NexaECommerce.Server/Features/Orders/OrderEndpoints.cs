using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using NexaEcommerce.SharedKernel.Abstractions;
using System.Security.Claims;

namespace NexaECommerce.Server.Features.Orders;

public sealed class OrderEndpoints
    : IFeatureEndpoints
{
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
        IOrderService orderService,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Results.Unauthorized();

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Checkout must contain at least one item."
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

        // Never trust client-submitted line items.
        // The server takes the current cart contents.
        var serverLines =
            cart.Items
                .Select(x =>
                    new CheckoutLineDto(
                        x.ProductVariantId,
                        x.Quantity))
                .ToList();

        var serverRequest =
            request with
            {
                Items = serverLines
            };

        try
        {
            var order =
                await orderService
                    .CreateFromCheckoutAsync(
                        tenant.Id,
                        userId,
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
        IOrderService orderService,
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