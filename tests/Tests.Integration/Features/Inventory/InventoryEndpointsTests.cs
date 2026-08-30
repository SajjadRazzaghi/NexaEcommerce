using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaECommerce.Server.Features.Inventory;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace NexaECommerce.Tests.Integration.Features.Inventory;

[Collection(IntegrationCollection.Name)]
public sealed class InventoryEndpointsTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Get_stock_requires_read_permission()
    {
        using var client =
            factory.CreateClient();

        var variantId =
            await GetExistingVariantIdAsync();

        var response =
            await client.GetAsync(
                $"/api/inventory/{variantId}");

        response.StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_stock_returns_stock_for_user_with_read_permission()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Read
                ]);

        var variantId =
            await GetExistingVariantIdAsync();

        var response =
            await client.GetAsync(
                $"/api/inventory/{variantId}");

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<StockResponse>();

        result.ShouldNotBeNull();

        result!.ProductVariantId
            .ShouldBe(variantId);
    }

    [Fact]
    public async Task Set_stock_requires_manage_permission()
    {
        using var client =
            factory.CreateAuthenticatedClient();

        var variantId =
            await GetExistingVariantIdAsync();

        var response =
            await client.PutAsJsonAsync(
                "/api/inventory/stock",
                new
                {
                    productVariantId = variantId,
                    quantity = 20
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Set_stock_updates_inventory()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Manage
                ]);

        var variantId =
            await GetExistingVariantIdAsync();

        var response =
            await client.PutAsJsonAsync(
                "/api/inventory/stock",
                new
                {
                    productVariantId = variantId,
                    quantity = 20
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<StockResponse>();

        result.ShouldNotBeNull();

        result!.AvailableQuantity
            .ShouldBe(20);

        result.ReservedQuantity
            .ShouldBe(0);

        result.TotalQuantity
            .ShouldBe(20);
    }

    [Fact]
    public async Task Reserve_endpoint_creates_active_reservation()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Manage
                ]);

        var variantId =
            await GetExistingVariantIdAsync();

        var reservationKey =
            $"api-test-{Guid.NewGuid():N}";

        var response =
            await client.PostAsJsonAsync(
                "/api/inventory/reservations",
                new
                {
                    productVariantId = variantId,
                    quantity = 3,
                    reservationKey,
                    expirationMinutes = 10
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<ReservationResponse>();

        result.ShouldNotBeNull();

        result!.ReservationKey
            .ShouldBe(reservationKey);

        result.ProductVariantId
            .ShouldBe(variantId);

        result.Quantity
            .ShouldBe(3);

        result.Status
            .ShouldBe("Active");
    }

    [Fact]
    public async Task Release_endpoint_releases_reservation()
    {
        using var client =
     factory.CreateAuthenticatedClient(
         permissions:
         [
             InventoryPermissions.Read,
            InventoryPermissions.Manage
         ]);

        var variantId =
            await GetExistingVariantIdAsync();

        var reservationKey =
            $"api-test-{Guid.NewGuid():N}";

        await ReserveAsync(
            client,
            variantId,
            reservationKey,
            3);

        var response =
            await client.PostAsync(
                $"/api/inventory/reservations/{reservationKey}/release",
                content: null);

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<ReservationResponse>();

        result.ShouldNotBeNull();

        result!.Status
            .ShouldBe("Released");

        var stockResponse =
            await client.GetAsync(
                $"/api/inventory/{variantId}");

        stockResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var stock =
            await stockResponse.Content
                .ReadFromJsonAsync<StockResponse>();

        stock.ShouldNotBeNull();

        stock!.AvailableQuantity
            .ShouldBe(10);

        stock.ReservedQuantity
            .ShouldBe(0);
    }

    [Fact]
    public async Task Commit_endpoint_commits_reservation()
    {
        using var client =
     factory.CreateAuthenticatedClient(
         permissions:
         [
             InventoryPermissions.Read,
            InventoryPermissions.Manage
         ]);

        var variantId =
            await GetExistingVariantIdAsync();

        var reservationKey =
            $"api-test-{Guid.NewGuid():N}";

        await ReserveAsync(
            client,
            variantId,
            reservationKey,
            4);

        var response =
            await client.PostAsync(
                $"/api/inventory/reservations/{reservationKey}/commit",
                content: null);

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var responseBody =
     await response.Content
         .ReadAsStringAsync();

        responseBody
            .ShouldNotBeNullOrWhiteSpace();

        var result =
            await response.Content
                .ReadFromJsonAsync<ReservationResponse>();

        result.ShouldNotBeNull();

        result!.Status
            .ShouldBe("Committed");

        var stockResponse =
            await client.GetAsync(
                $"/api/inventory/{variantId}");

        stockResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var stockBody =
            await stockResponse.Content
                .ReadAsStringAsync();

        stockBody
            .ShouldNotBeNullOrWhiteSpace();

        var stock =
      await stockResponse.Content
          .ReadFromJsonAsync<StockResponse>();

        stock.ShouldNotBeNull();

        stock!.AvailableQuantity
            .ShouldBe(6);

        stock.ReservedQuantity
            .ShouldBe(0);
    }

    private async Task<Guid>
        GetExistingVariantIdAsync()
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
                .Select(
                    x => x.Id)
                .FirstOrDefaultAsync();

        variant.ShouldNotBe(Guid.Empty);

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<
                    NexaEcommerce.Modules.Inventory.Infrastructure.Persistence
                        .InventoryDbContext>();

        const string tenantId = "default";
        const int baselineQuantity = 10;

        var stock =
            await inventoryDb.StockItems
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.ProductVariantId == variant);

        if (stock is null)
        {
            stock =
                NexaEcommerce.Modules.Inventory.Domain.Entities
                    .StockItem.Create(
                        tenantId,
                        variant,
                        baselineQuantity);

            await inventoryDb.StockItems.AddAsync(stock);
        }
        else
        {
            if (stock.ReservedQuantity > 0)
            {
                stock.Release(
                    stock.ReservedQuantity);
            }

            if (stock.AvailableQuantity > baselineQuantity)
            {
                stock.Remove(
                    stock.AvailableQuantity -
                    baselineQuantity);
            }
            else if (stock.AvailableQuantity < baselineQuantity)
            {
                stock.Add(
                    baselineQuantity -
                    stock.AvailableQuantity);
            }
        }

        var reservations =
            await inventoryDb.StockReservations
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.ProductVariantId == variant)
                .ToListAsync();

        if (reservations.Count > 0)
        {
            inventoryDb.StockReservations
                .RemoveRange(reservations);
        }

        await inventoryDb.SaveChangesAsync();

        return variant;
    }

    private static async Task ReserveAsync(
        HttpClient client,
        Guid variantId,
        string reservationKey,
        int quantity)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/inventory/reservations",
                new
                {
                    productVariantId = variantId,
                    quantity,
                    reservationKey,
                    expirationMinutes = 10
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);
    }

    private sealed record StockResponse(
        Guid ProductVariantId,
        int AvailableQuantity,
        int ReservedQuantity,
        int TotalQuantity);

    private sealed record ReservationResponse(
        string ReservationKey,
        Guid ProductVariantId,
        int Quantity,
        string Status,
        DateTimeOffset ExpiresAt);
}
