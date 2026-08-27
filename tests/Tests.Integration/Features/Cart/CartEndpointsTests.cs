using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace NexaECommerce.Tests.Integration.Features.Cart;

[Collection(IntegrationCollection.Name)]
public sealed class CartEndpointsTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Anonymous_get_returns_empty_cart()
    {
        var client = CreateGuestClient();

        var response =
            await client.GetAsync(
                "/api/cart/");

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var cart =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        cart.ShouldNotBeNull();
        cart!.TotalQuantity.ShouldBe(0);
        cart.TotalAmount.ShouldBe(0);
    }

    [Fact]
    public async Task Anonymous_add_sets_guest_cart_cookie()
    {
        var client = CreateGuestClient();

        var variantId =
            await GetExistingSellableVariantIdAsync();

        var response =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 1
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        response.Headers
            .Any(x =>
                x.Key.Equals(
                    "Set-Cookie",
                    StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Adding_same_variant_twice_increases_quantity()
    {
        var client =
            factory.CreateClient();

        var variantId =
            await GetExistingSellableVariantIdAsync();

        var first =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 1
                });

        first.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var setCookie =
            first.Headers
                .GetValues("Set-Cookie")
                .FirstOrDefault(x =>
                    x.StartsWith(
                        "nexa_cart=",
                        StringComparison.OrdinalIgnoreCase));

        setCookie.ShouldNotBeNull();

        var guestCookie =
            setCookie!
                .Split(';', 2)[0];

        client.DefaultRequestHeaders.Remove(
            "Cookie");

        client.DefaultRequestHeaders.Add(
            "Cookie",
            guestCookie);

        var second =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 2
                });

        second.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var cart =
            await second.Content
                .ReadFromJsonAsync<CartResponse>();

        cart.ShouldNotBeNull();

        cart!.TotalQuantity
            .ShouldBe(3);

        cart.Items.Count
            .ShouldBe(1);

        cart.Items[0]
            .Quantity
            .ShouldBe(3);
    }
    [Fact]
    public async Task Authenticated_user_gets_own_cart()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "cart-user-1");

        var response =
            await client.GetAsync(
                "/api/cart/");

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var cart =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        cart.ShouldNotBeNull();
        cart!.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task Anonymous_unknown_variant_returns_not_found()
    {
        var client =
             CreateGuestClient();;

        var response =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId =
                        Guid.NewGuid(),
                    quantity = 1
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymous_invalid_quantity_returns_bad_request()
    {
        var client =
             CreateGuestClient();;

        var variantId =
            await GetExistingSellableVariantIdAsync();

        var response =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 0
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Removing_item_from_cart_empties_it()
    {
        var client =
             CreateGuestClient();;

        var variantId =
            await GetExistingSellableVariantIdAsync();

        var add =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 1
                });

        add.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var remove =
            await client.DeleteAsync(
                $"/api/cart/items/{variantId}");

        remove.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var cart =
            await remove.Content
                .ReadFromJsonAsync<CartResponse>();

        cart.ShouldNotBeNull();
        cart!.Items.ShouldBeEmpty();
        cart.TotalQuantity
            .ShouldBe(0);
    }

    [Fact]
    public async Task Clearing_cart_removes_all_items()
    {
        var client =
             CreateGuestClient();;

        var variantId =
            await GetExistingSellableVariantIdAsync();

        var add =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 1
                });

        add.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var clear =
            await client.DeleteAsync(
                "/api/cart/");

        clear.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var cart =
            await clear.Content
                .ReadFromJsonAsync<CartResponse>();

        cart.ShouldNotBeNull();
        cart!.Items.ShouldBeEmpty();
        cart.TotalQuantity
            .ShouldBe(0);
    }

    [Fact]
    public async Task Different_authenticated_users_have_separate_carts()
    {
        var client1 =
            factory.CreateAuthenticatedClient(
                "cart-user-a");

        var client2 =
            factory.CreateAuthenticatedClient(
                "cart-user-b");

        var variantId =
            await GetExistingSellableVariantIdAsync();

        var add =
            await client1.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 2
                });

        add.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var cart1 =
            await client1.GetFromJsonAsync<CartResponse>(
                "/api/cart/");

        var cart2 =
            await client2.GetFromJsonAsync<CartResponse>(
                "/api/cart/");

        cart1.ShouldNotBeNull();
        cart2.ShouldNotBeNull();

        cart1!.TotalQuantity
            .ShouldBe(2);

        cart2!.TotalQuantity
            .ShouldBe(0);
    }

    private async Task<Guid>
        GetExistingSellableVariantIdAsync()
    {
        using var scope =
            factory.Services.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    NexaEcommerce.Modules.Catalog.Infrastructure
                        .CatalogDbContext>();

        var variant =
            await db.ProductVariants
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.StockQuantity > 0 &&
                    x.Product.IsActive &&
                    x.Product.IsPublished &&
                    !x.Product.IsDeleted)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

        variant.ShouldNotBe(Guid.Empty);

        return variant;
    }

    private sealed record CartResponse(
        Guid Id,
        string TenantId,
        List<CartItemResponse> Items,
        int TotalQuantity,
        decimal TotalAmount);

    private sealed record CartItemResponse(
        Guid ProductVariantId,
        string ProductName,
        string? ImageUrl,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);
   
}
