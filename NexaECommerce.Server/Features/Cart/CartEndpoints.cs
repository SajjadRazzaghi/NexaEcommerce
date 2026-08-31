using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.ShoppingCart.Application.DTOs;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using System.Security.Claims;

namespace NexaECommerce.Server.Features.Cart;

public sealed class CartEndpoints : IFeatureEndpoints
{
    private const string GuestCartCookie = "nexa_cart";

    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/cart")
                .WithTags("Cart");

        group.MapGet("/", Get);
        group.MapPost("/items", AddItem);
        group.MapPost("/merge", Merge);
        group.MapPut("/items", SetQuantity);
        group.MapDelete(
            "/items/{productVariantId:guid}",
            RemoveItem);
        group.MapDelete("/", Clear);
    }

    private static async Task<IResult> Get(
        HttpContext http,
        CancellationToken ct)
    {
        var cartService =
            http.RequestServices
                .GetRequiredService<ICartService>();

        var tenant =
            http.RequestServices
                .GetRequiredService<ICurrentTenant>();

        var userId =
            GetUserId(http);

        var guestToken =
            GetGuestToken(
                http,
                userId);

        var result =
            await cartService.GetAsync(
                tenant.Id,
                userId,
                guestToken,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> AddItem(
        [FromBody] AddCartItemDto request,
        HttpContext http,
        CancellationToken ct)
    {
        var cartService =
            http.RequestServices
                .GetRequiredService<ICartService>();

        var tenant =
            http.RequestServices
                .GetRequiredService<ICurrentTenant>();

        try
        {
            var userId =
                GetUserId(http);

            var guestToken =
                GetGuestToken(
                    http,
                    userId,
                    create: true);

            var result =
                await cartService.AddItemAsync(
                    tenant.Id,
                    userId,
                    guestToken,
                    request,
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
        catch (ArgumentOutOfRangeException ex)
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

    private static async Task<IResult> SetQuantity(
        [FromBody] SetCartItemQuantityDto request,
        HttpContext http,
        CancellationToken ct)
    {
        var cartService =
            http.RequestServices
                .GetRequiredService<ICartService>();

        var tenant =
            http.RequestServices
                .GetRequiredService<ICurrentTenant>();

        try
        {
            var userId =
                GetUserId(http);

            var guestToken =
                GetGuestToken(
                    http,
                    userId,
                    create: true);

            var result =
                await cartService.SetQuantityAsync(
                    tenant.Id,
                    userId,
                    guestToken,
                    request,
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

    private static async Task<IResult> RemoveItem(
        Guid productVariantId,
        HttpContext http,
        CancellationToken ct)
    {
        var cartService =
            http.RequestServices
                .GetRequiredService<ICartService>();

        var tenant =
            http.RequestServices
                .GetRequiredService<ICurrentTenant>();

        var userId =
            GetUserId(http);

        var guestToken =
            GetGuestToken(
                http,
                userId);

        var result =
            await cartService.RemoveItemAsync(
                tenant.Id,
                userId,
                guestToken,
                productVariantId,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Clear(
        HttpContext http,
        CancellationToken ct)
    {
        var cartService =
            http.RequestServices
                .GetRequiredService<ICartService>();

        var tenant =
            http.RequestServices
                .GetRequiredService<ICurrentTenant>();

        var userId =
            GetUserId(http);

        var guestToken =
            GetGuestToken(
                http,
                userId);

        var result =
            await cartService.ClearAsync(
                tenant.Id,
                userId,
                guestToken,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Merge(
        HttpContext http,
        CancellationToken ct)
    {
        var cartService =
            http.RequestServices
                .GetRequiredService<ICartService>();

        var tenant =
            http.RequestServices
                .GetRequiredService<ICurrentTenant>();

        var userId =
            GetUserId(http);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var guestToken =
            GetGuestToken(
                http,
                userId: null);

        if (string.IsNullOrWhiteSpace(guestToken))
        {
            return Results.Ok(
                await cartService.GetAsync(
                    tenant.Id,
                    userId,
                    null,
                    ct));
        }

        try
        {
            var result =
                await cartService.MergeGuestCartAsync(
                    tenant.Id,
                    userId,
                    guestToken,
                    ct);

            http.Response.Cookies.Delete(
                GuestCartCookie);

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

    private static string? GetUserId(
        HttpContext http)
    {
        return http.User
            .FindFirstValue(
                ClaimTypes.NameIdentifier);
    }

    private static string? GetGuestToken(
        HttpContext http,
        string? userId,
        bool create = false)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (http.Request.Cookies.TryGetValue(
                GuestCartCookie,
                out var existing) &&
            !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        if (!create)
        {
            return null;
        }

        var token =
            Convert.ToHexString(
                System.Security.Cryptography
                    .RandomNumberGenerator
                    .GetBytes(32));

        http.Response.Cookies.Append(
            GuestCartCookie,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = http.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(30)
            });

        return token;
    }
}