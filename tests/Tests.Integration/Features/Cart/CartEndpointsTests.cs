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
    public async Task Cart_uses_inventory_available_stock_instead_of_catalog_stock()
    {
        var client = CreateGuestClient();

        var variantId =
            await GetExistingSellableVariantIdAsync();

        const string tenantId = "default";

        int originalAvailableQuantity;

        using (var scope =
               factory.Services.CreateScope())
        {
            var inventoryDb =
                scope.ServiceProvider
                    .GetRequiredService<
                        NexaEcommerce.Modules.Inventory.Infrastructure.Persistence
                            .InventoryDbContext>();

            var stock =
                await inventoryDb.StockItems
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.ProductVariantId == variantId);

            stock.ShouldNotBeNull();

            originalAvailableQuantity =
                stock!.AvailableQuantity;

            if (stock.ReservedQuantity > 0)
            {
                stock.Release(
                    stock.ReservedQuantity);
            }

            if (stock.AvailableQuantity > 1)
            {
                stock.Remove(
                    stock.AvailableQuantity - 1);
            }
            else if (stock.AvailableQuantity == 0)
            {
                stock.Add(1);
            }

            await inventoryDb.SaveChangesAsync();
        }

        try
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/cart/items",
                    new
                    {
                        productVariantId = variantId,
                        quantity = 2
                    });

            response.StatusCode
                .ShouldBe(HttpStatusCode.Conflict);

            var body =
                await response.Content
                    .ReadAsStringAsync();

            body.ShouldContain(
                "Requested quantity exceeds available stock");
        }
        finally
        {
            using var scope =
                factory.Services.CreateScope();

            var inventoryDb =
                scope.ServiceProvider
                    .GetRequiredService<
                        NexaEcommerce.Modules.Inventory.Infrastructure.Persistence
                            .InventoryDbContext>();

            var stock =
                await inventoryDb.StockItems
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.ProductVariantId == variantId);

            stock.ShouldNotBeNull();

            if (stock!.ReservedQuantity > 0)
            {
                stock.Release(
                    stock.ReservedQuantity);
            }

            if (stock.AvailableQuantity < originalAvailableQuantity)
            {
                stock.Add(
                    originalAvailableQuantity -
                    stock.AvailableQuantity);
            }
            else if (stock.AvailableQuantity > originalAvailableQuantity)
            {
                stock.Remove(
                    stock.AvailableQuantity -
                    originalAvailableQuantity);
            }

            await inventoryDb.SaveChangesAsync();
        }
    }
  
[Fact]
public async Task Guest_cart_can_be_merged_into_authenticated_user_cart()
    {
        using var client =
            factory.CreateClient();

        var variantId =
            await GetVariantForMergeTestAsync();

        // ------------------------------------------------------------
        // 1. Guest adds product
        // ------------------------------------------------------------

        var guestAddResponse =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 2
                });

        guestAddResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        guestAddResponse.Headers
            .TryGetValues(
                "Set-Cookie",
                out var cookies)
            .ShouldBeTrue();

        cookies!
            .Any(x =>
                x.StartsWith(
                    "nexa_cart=",
                    StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue();

        // ------------------------------------------------------------
        // 2. Authenticate the same client
        // ------------------------------------------------------------

        var userId =
            $"merge-user-{Guid.NewGuid():N}";

        client.DefaultRequestHeaders.Add(
            TestAuthHandler.UserIdHeader,
            userId);

        // ------------------------------------------------------------
        // 3. Create user cart with same product
        // ------------------------------------------------------------

        var userAddResponse =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 3
                });

        userAddResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        // ------------------------------------------------------------
        // 4. Merge guest cart
        // ------------------------------------------------------------

        var mergeResponse =
            await client.PostAsync(
                "/api/cart/merge",
                content: null);

        mergeResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var merged =
            await mergeResponse.Content
                .ReadFromJsonAsync<CartResponse>();

        merged.ShouldNotBeNull();

        var item =
            merged!.Items
                .Single(
                    x =>
                        x.ProductVariantId ==
                        variantId);

        item.Quantity
            .ShouldBe(5);

        // ------------------------------------------------------------
        // 5. Merge again must be idempotent
        // ------------------------------------------------------------

        var secondMergeResponse =
            await client.PostAsync(
                "/api/cart/merge",
                content: null);

        secondMergeResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var secondMerged =
            await secondMergeResponse.Content
                .ReadFromJsonAsync<CartResponse>();

        secondMerged.ShouldNotBeNull();

        var secondItem =
            secondMerged!.Items
                .Single(
                    x =>
                        x.ProductVariantId ==
                        variantId);

        secondItem.Quantity
            .ShouldBe(5);
    }

    [Fact]
    public async Task Guest_cart_merge_caps_quantity_at_inventory_stock()
    {
        using var client =
            factory.CreateClient();

        var variantId =
            await GetVariantForMergeTestAsync();

        var guestAddResponse =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 7
                });

        guestAddResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var userId =
            $"merge-cap-user-{Guid.NewGuid():N}";

        client.DefaultRequestHeaders.Add(
            TestAuthHandler.UserIdHeader,
            userId);

        var userAddResponse =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId = variantId,
                    quantity = 3
                });

        userAddResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var mergeResponse =
            await client.PostAsync(
                "/api/cart/merge",
                content: null);

        mergeResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var merged =
            await mergeResponse.Content
                .ReadFromJsonAsync<CartResponse>();

        merged.ShouldNotBeNull();

        var item =
            merged!.Items
                .Single(
                    x =>
                        x.ProductVariantId ==
                        variantId);

        // Integration fixture seeds 1000 units.
        // Change the assertion only if a deliberate stock
        // boundary test is introduced later.
        item.Quantity
            .ShouldBe(10);
    }

    private async Task<Guid>
        GetVariantForMergeTestAsync()
    {
        using var scope =
            factory.Services.CreateScope();

        var catalogDb =
            scope.ServiceProvider
                .GetRequiredService<
                    NexaEcommerce.Modules.Catalog.Infrastructure
                        .CatalogDbContext>();

        var variant =
            await catalogDb.ProductVariants
                .AsNoTracking()
                .Where(
                    x =>
                        x.IsActive &&
                        !x.IsDeleted &&
                        x.Product.IsActive &&
                        x.Product.IsPublished &&
                        !x.Product.IsDeleted)
                .OrderBy(
                    x => x.Id)
                .Select(
                    x => x.Id)
                .FirstOrDefaultAsync();

        variant
            .ShouldNotBe(Guid.Empty);

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

[Fact]
public async Task Anonymous_get_returns_empty_cart()
    {
        var client = CreateGuestClient();

        var response =
            await client.GetAsync(
                "/api/cart/");

        var body =
            await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode
            .ShouldBeTrue(
                $"HTTP {(int)response.StatusCode} {response.StatusCode}\n" +
                $"Response body:\n{body}");

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
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    HandleCookies = false
                });

        var variantId =
            await GetExistingSellableVariantIdAsync();

        var guestToken =
            Guid.NewGuid().ToString("N");

        // ------------------------------------------------------------
        // First request
        // ------------------------------------------------------------

        using var firstRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/cart/items")
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            productVariantId = variantId,
                            quantity = 1
                        })
            };

        firstRequest.Headers.TryAddWithoutValidation(
            "Cookie",
            $"nexa_cart={guestToken}");

        var first =
            await client.SendAsync(firstRequest);

        first.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        // ------------------------------------------------------------
        // Verify first request persisted the cart.
        // ------------------------------------------------------------

        using (var scope =
               factory.Services.CreateScope())
        {
            var cartDb =
                scope.ServiceProvider
                    .GetRequiredService<
                        NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence
                            .ShoppingCartDbContext>();

            var createdCart =
                await cartDb.Carts
                    .Include(x => x.Items)
                    .SingleOrDefaultAsync(
                        x =>
                            x.GuestToken == guestToken &&
                            x.UserId == null);

            createdCart.ShouldNotBeNull();

            createdCart!
                .Items.Count
                .ShouldBe(1);

            var createdItem =
                createdCart.Items.Single();

            createdItem.ProductVariantId
                .ShouldBe(variantId);

            createdItem.Quantity
                .ShouldBe(1);
        }

        // ------------------------------------------------------------
        // Second request
        // ------------------------------------------------------------

        using var secondRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/cart/items")
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            productVariantId = variantId,
                            quantity = 2
                        })
            };

        secondRequest.Headers.TryAddWithoutValidation(
            "Cookie",
            $"nexa_cart={guestToken}");

        var second =
            await client.SendAsync(secondRequest);

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

        var responseItem =
            cart.Items.Single();

        responseItem.Quantity
            .ShouldBe(3);

        // ------------------------------------------------------------
        // Final database verification
        // ------------------------------------------------------------

        using (var scope =
               factory.Services.CreateScope())
        {
            var cartDb =
                scope.ServiceProvider
                    .GetRequiredService<
                        NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence
                            .ShoppingCartDbContext>();

            var finalCart =
                await cartDb.Carts
                    .Include(x => x.Items)
                    .SingleOrDefaultAsync(
                        x =>
                            x.GuestToken == guestToken &&
                            x.UserId == null);

            finalCart.ShouldNotBeNull();

            finalCart!
                .Items.Count
                .ShouldBe(1);

            var finalItem =
                finalCart.Items.Single();

            finalItem.ProductVariantId
                .ShouldBe(variantId);

            finalItem.Quantity
                .ShouldBe(3);
        }
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
        var client = CreateGuestClient();

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
        var client = CreateGuestClient();

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
        var client = CreateGuestClient();

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

        var setCookie =
            add.Headers
                .GetValues("Set-Cookie")
                .FirstOrDefault(x =>
                    x.StartsWith(
                        "nexa_cart=",
                        StringComparison.OrdinalIgnoreCase));

        setCookie.ShouldNotBeNull();

        var cookie =
            setCookie!
                .Split(';', 2)[0];

        client.DefaultRequestHeaders.Remove(
            "Cookie");

        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

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
        cart.TotalQuantity.ShouldBe(0);
    }

    [Fact]
    public async Task Clearing_cart_removes_all_items()
    {
        var client = CreateGuestClient();

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

        var setCookie =
            add.Headers
                .GetValues("Set-Cookie")
                .FirstOrDefault(x =>
                    x.StartsWith(
                        "nexa_cart=",
                        StringComparison.OrdinalIgnoreCase));

        setCookie.ShouldNotBeNull();

        var cookie =
            setCookie!
                .Split(';', 2)[0];

        client.DefaultRequestHeaders.Remove(
            "Cookie");

        client.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

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
        cart.TotalQuantity.ShouldBe(0);
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

    private HttpClient CreateGuestClient()
    {
        return factory.CreateClient();
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
}