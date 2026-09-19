using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace NexaECommerce.Tests.Integration.Features.Orders;

[Collection(IntegrationCollection.Name)]
public sealed class WarehouseAllocationTests(
    CustomWebApplicationFactory factory)
{
    private const string TenantId = "default";

    [Fact]
    public async Task Allocation_prefers_default_warehouse_and_single_picking_location()
    {
        var variant1 = Guid.NewGuid();
        var variant2 = Guid.NewGuid();

        var orderId =
            await CreateProcessingOrderAsync(
                $"allocation-default-{Guid.NewGuid():N}",
                (variant1, "SKU-A1", "Product A1", 2),
                (variant2, "SKU-A2", "Product A2", 3));

        var warehouse =
            await CreateWarehouseAsync(
                $"DEF-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Default Test Warehouse",
                isDefault: true);

        var location =
            await CreateLocationAsync(
                warehouse.Id,
                $"LOC-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Main Picking Location");

        await AddWarehouseStockAsync(
            warehouse.Id,
            location.Id,
            variant1,
            10);

        await AddWarehouseStockAsync(
            warehouse.Id,
            location.Id,
            variant2,
            10);

        var nonDefaultWarehouse =
            await CreateWarehouseAsync(
                $"NON-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Non Default Warehouse",
                isDefault: false);

        var nonDefaultLocation =
            await CreateLocationAsync(
                nonDefaultWarehouse.Id,
                $"LOC-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Secondary Location");

        await AddWarehouseStockAsync(
            nonDefaultWarehouse.Id,
            nonDefaultLocation.Id,
            variant1,
            10);

        await AddWarehouseStockAsync(
            nonDefaultWarehouse.Id,
            nonDefaultLocation.Id,
            variant2,
            10);

        var client =
            factory.CreateAuthenticatedClient(
                $"allocation-default-{Guid.NewGuid():N}",
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/allocate",
                null,
                TestContext.Current.CancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body =
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Allocation failed. " +
                $"Status: {(int)response.StatusCode} " +
                $"Body: {body}");
        }

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("warehouseId")
            .GetGuid()
            .ShouldBe(warehouse.Id);

        json.GetProperty("warehouseCode")
            .GetString()
            .ShouldBe(warehouse.Code);

        json.GetProperty("pickingLocationId")
            .GetGuid()
            .ShouldBe(location.Id);

        json.GetProperty("pickingLocationCode")
            .GetString()
            .ShouldBe(location.Code);

        json.GetProperty("alreadyAllocated")
            .GetBoolean()
            .ShouldBeFalse();

        json.GetProperty("lines")
            .GetArrayLength()
            .ShouldBe(2);
    }

    [Fact]
    public async Task Allocation_skips_default_warehouse_when_it_cannot_fulfill_all_lines()
    {
        var variant1 = Guid.NewGuid();
        var variant2 = Guid.NewGuid();

        var orderId =
            await CreateProcessingOrderAsync(
                $"allocation-fallback-{Guid.NewGuid():N}",
                (variant1, "SKU-B1", "Product B1", 2),
                (variant2, "SKU-B2", "Product B2", 2));

        var defaultWarehouse =
            await CreateWarehouseAsync(
                $"DEF-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Default Incomplete Warehouse",
                isDefault: true);

        var defaultLocation =
            await CreateLocationAsync(
                defaultWarehouse.Id,
                $"LOC-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Default Location");

        await AddWarehouseStockAsync(
            defaultWarehouse.Id,
            defaultLocation.Id,
            variant1,
            10);

        // variant2 intentionally missing.

        var fallbackWarehouse =
            await CreateWarehouseAsync(
                $"ALT-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Fallback Complete Warehouse",
                isDefault: false);

        var fallbackLocation =
            await CreateLocationAsync(
                fallbackWarehouse.Id,
                $"LOC-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Fallback Location");

        await AddWarehouseStockAsync(
            fallbackWarehouse.Id,
            fallbackLocation.Id,
            variant1,
            10);

        await AddWarehouseStockAsync(
            fallbackWarehouse.Id,
            fallbackLocation.Id,
            variant2,
            10);

        var client =
            factory.CreateAuthenticatedClient(
                $"allocation-fallback-{Guid.NewGuid():N}",
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/allocate",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("warehouseId")
            .GetGuid()
            .ShouldBe(fallbackWarehouse.Id);

        json.GetProperty("warehouseCode")
            .GetString()
            .ShouldBe(fallbackWarehouse.Code);
    }

    [Fact]
    public async Task Allocation_returns_conflict_when_no_single_warehouse_can_fulfill_complete_order()
    {
        var variant1 = Guid.NewGuid();
        var variant2 = Guid.NewGuid();

        var orderId =
            await CreateProcessingOrderAsync(
                $"allocation-conflict-{Guid.NewGuid():N}",
                (variant1, "SKU-C1", "Product C1", 2),
                (variant2, "SKU-C2", "Product C2", 2));

        var warehouseA =
            await CreateWarehouseAsync(
                $"WH-A-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Warehouse A",
                isDefault: true);

        var locationA =
            await CreateLocationAsync(
                warehouseA.Id,
                $"LOC-A-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Location A");

        await AddWarehouseStockAsync(
            warehouseA.Id,
            locationA.Id,
            variant1,
            10);

        var warehouseB =
            await CreateWarehouseAsync(
                $"WH-B-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Warehouse B",
                isDefault: false);

        var locationB =
            await CreateLocationAsync(
                warehouseB.Id,
                $"LOC-B-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Location B");

        await AddWarehouseStockAsync(
            warehouseB.Id,
            locationB.Id,
            variant2,
            10);

        var client =
            factory.CreateAuthenticatedClient(
                $"allocation-conflict-{Guid.NewGuid():N}",
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/allocate",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var fulfillment =
            await db.Fulfillments
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.OrderId == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        fulfillment.WarehouseId
            .ShouldBeNull();

        fulfillment.PickingLocationId
            .ShouldBeNull();
    }

    [Fact]
    public async Task Allocation_allows_multiple_locations_inside_same_warehouse()
    {
        var variant1 = Guid.NewGuid();
        var variant2 = Guid.NewGuid();

        var orderId =
            await CreateProcessingOrderAsync(
                $"allocation-multi-location-{Guid.NewGuid():N}",
                (variant1, "SKU-D1", "Product D1", 10),
                (variant2, "SKU-D2", "Product D2", 10));

        var warehouse =
            await CreateWarehouseAsync(
                $"ML-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Multi Location Warehouse",
                isDefault: true);

        var locationA =
            await CreateLocationAsync(
                warehouse.Id,
                $"A-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Location A");

        var locationB =
            await CreateLocationAsync(
                warehouse.Id,
                $"B-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Location B");

        await AddWarehouseStockAsync(
            warehouse.Id,
            locationA.Id,
            variant1,
            5);

        await AddWarehouseStockAsync(
            warehouse.Id,
            locationA.Id,
            variant2,
            5);

        await AddWarehouseStockAsync(
            warehouse.Id,
            locationB.Id,
            variant1,
            5);

        await AddWarehouseStockAsync(
            warehouse.Id,
            locationB.Id,
            variant2,
            5);

        var client =
            factory.CreateAuthenticatedClient(
                $"allocation-multi-location-{Guid.NewGuid():N}",
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/allocate",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("warehouseId")
            .GetGuid()
            .ShouldBe(warehouse.Id);

        /*
         * No single location has enough quantity for the entire order,
         * so warehouse is assigned while PickingLocation remains null.
         */
        json.GetProperty("pickingLocationId")
            .ValueKind
            .ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Allocation_is_idempotent_and_does_not_change_stock()
    {
        var variant1 = Guid.NewGuid();

        var orderId =
            await CreateProcessingOrderAsync(
                $"allocation-idempotent-{Guid.NewGuid():N}",
                (variant1, "SKU-E1", "Product E1", 2));

        var warehouse =
            await CreateWarehouseAsync(
                $"ID-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Idempotent Warehouse",
                isDefault: true);

        var location =
            await CreateLocationAsync(
                warehouse.Id,
                $"LOC-{Guid.NewGuid():N}".ToUpperInvariant(),
                "Idempotent Location");

        await AddWarehouseStockAsync(
            warehouse.Id,
            location.Id,
            variant1,
            17);

        var client =
            factory.CreateAuthenticatedClient(
                $"allocation-idempotent-{Guid.NewGuid():N}",
                PermissionClaims.All);

        var firstResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/allocate",
                null,
                TestContext.Current.CancellationToken);

        firstResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var firstJson =
            await firstResponse.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        firstJson
            .GetProperty("warehouseId")
            .GetGuid()
            .ShouldBe(warehouse.Id);

        firstJson
            .GetProperty("alreadyAllocated")
            .GetBoolean()
            .ShouldBeFalse();

        var secondResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/allocate",
                null,
                TestContext.Current.CancellationToken);

        secondResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var secondJson =
            await secondResponse.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        secondJson
            .GetProperty("warehouseId")
            .GetGuid()
            .ShouldBe(warehouse.Id);

        secondJson
            .GetProperty("pickingLocationId")
            .GetGuid()
            .ShouldBe(location.Id);

        secondJson
            .GetProperty("alreadyAllocated")
            .GetBoolean()
            .ShouldBeTrue();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        var stock =
            await inventoryDb.WarehouseStocks
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.TenantId == TenantId &&
                        x.WarehouseId == warehouse.Id &&
                        x.LocationId == location.Id &&
                        x.ProductVariantId == variant1,
                    TestContext.Current.CancellationToken);

        /*
         * Allocation is deliberately read-only against inventory.
         */
        stock.OnHandQuantity
            .ShouldBe(17);

        stock.ReservedQuantity
            .ShouldBe(0);

        stock.AvailableQuantity
            .ShouldBe(17);
    }

    private async Task<Guid> CreateProcessingOrderAsync(
        string userId,
        params (Guid VariantId, string Sku, string ProductName, int Quantity)[] items)
    {
        var order =
            Order.Create(
                TenantId,
                userId,
                $"TEST-{Guid.NewGuid():N}",
                $"test-{Guid.NewGuid():N}",
                "IRR",
                100_000m,
                20_000m,
                0m,
                "Integration Test User",
                "09000000000",
                "Test Address",
                "Tehran",
                "1234567890");

        foreach (var item in items)
        {
            order.AddItem(
                item.VariantId,
                item.Sku,
                item.ProductName,
                100_000m,
                item.Quantity);
        }

        order.MarkPaid();
        order.StartProcessing();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        await db.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        await db.Orders.AddAsync(
            order,
            TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var fulfillment =
            Fulfillment.Create(
                order.Id,
                TenantId);

        await db.Fulfillments.AddAsync(
            fulfillment,
            TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return order.Id;
    }

private async Task<Warehouse> CreateWarehouseAsync(
    string code,
    string name,
    bool isDefault)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        await db.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        /*
         * Warehouse.Code حداکثر 32 کاراکتر است.
         *
         * بعضی از تست‌ها از الگوی:
         *     DEF-{Guid:N}
         * استفاده می‌کنند که 36+ کاراکتر می‌شود.
         *
         * بنابراین فقط در محیط تست آن را به یک کد کوتاه و
         * همچنان یکتا تبدیل می‌کنیم.
         */
        if (normalizedCode.Length > 32)
        {
            var prefix =
                normalizedCode[..23];

            var uniqueSuffix =
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant();

            normalizedCode =
                $"{prefix}-{uniqueSuffix}";
        }

        var warehouse =
            Warehouse.Create(
                TenantId,
                normalizedCode,
                name,
                isDefault: isDefault);

        await db.Warehouses.AddAsync(
            warehouse,
            TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return warehouse;
    }


    private async Task<WarehouseLocation>
        CreateLocationAsync(
            Guid warehouseId,
            string code,
            string name)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        var location =
            WarehouseLocation.Create(
                TenantId,
                warehouseId,
                code,
                name);

        await db.WarehouseLocations.AddAsync(
            location,
            TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return location;
    }

    private async Task AddWarehouseStockAsync(
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        int quantity)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        var stock =
            WarehouseStock.Create(
                TenantId,
                warehouseId,
                locationId,
                productVariantId,
                onHandQuantity: quantity);

        await db.WarehouseStocks.AddAsync(
            stock,
            TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);
    }
}

