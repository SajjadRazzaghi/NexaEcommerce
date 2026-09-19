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
public sealed class WarehousePickingTests(
    CustomWebApplicationFactory factory)
{
    private const string TenantId = "default";

    [Fact]
    public async Task Start_picking_requires_active_warehouse_reservation()
    {
        var scenario =
            await CreateScenarioAsync(
                createWarehouseReservation: false);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/start-picking",
                null,
                TestContext.Current.CancellationToken);

        if (response.StatusCode !=
            HttpStatusCode.Conflict)
        {
            var body =
                await response.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Start picking returned an unexpected status. " +
                $"Status: {(int)response.StatusCode} " +
                $"{response.StatusCode}. " +
                $"Body: {body}");
        }
    }

    [Fact]
    public async Task Picking_consumes_reserved_warehouse_stock_and_marks_fulfillment_picked()
    {
        var scenario =
            await CreateScenarioAsync(
                createWarehouseReservation: true);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var startResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/start-picking",
                null,
                TestContext.Current.CancellationToken);

        if (startResponse.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await startResponse.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Start picking failed. " +
                $"Status: {(int)startResponse.StatusCode} " +
                $"{startResponse.StatusCode}. " +
                $"Body: {body}");
        }

        var startJson =
            await startResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        startJson
            .GetProperty("status")
            .GetString()
            .ShouldBe("Picking");

        var pickResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/picked",
                null,
                TestContext.Current.CancellationToken);

        if (pickResponse.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await pickResponse.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Complete picking failed. " +
                $"Status: {(int)pickResponse.StatusCode} " +
                $"{pickResponse.StatusCode}. " +
                $"Body: {body}");
        }

        var pickJson =
            await pickResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        pickJson
            .GetProperty("status")
            .GetString()
            .ShouldBe("Picked");

        await using var inventoryScope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            inventoryScope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        var stock =
            await inventoryDb.WarehouseStocks
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.WarehouseStockId,
                    TestContext.Current.CancellationToken);

        stock.OnHandQuantity
            .ShouldBe(8);

        stock.ReservedQuantity
            .ShouldBe(0);

        stock.AvailableQuantity
            .ShouldBe(8);

        var reservation =
            await inventoryDb.WarehouseStockReservations
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.WarehouseReservationId,
                    TestContext.Current.CancellationToken);

        reservation.Status
            .ShouldBe(
                WarehouseStockReservationStatus.Consumed);

        var movement =
            await inventoryDb.WarehouseStockMovements
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.ReferenceType ==
                            "WarehousePicking" &&
                        x.ReferenceId ==
                            scenario.OrderId.ToString(),
                    TestContext.Current.CancellationToken);

        movement.Type
            .ShouldBe(
                WarehouseStockMovementType.Picking);

        movement.QuantityDelta
            .ShouldBe(-2);

        movement.BalanceAfter
            .ShouldBe(8);
    }

    [Fact]
    public async Task Completing_picking_twice_does_not_consume_stock_twice()
    {
        var scenario =
            await CreateScenarioAsync(
                createWarehouseReservation: true);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var startResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/start-picking",
                null,
                TestContext.Current.CancellationToken);

        if (startResponse.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await startResponse.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Start picking failed. " +
                $"Status: {(int)startResponse.StatusCode} " +
                $"{startResponse.StatusCode}. " +
                $"Body: {body}");
        }

        var firstPick =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/picked",
                null,
                TestContext.Current.CancellationToken);

        if (firstPick.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await firstPick.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"First picking completion failed. " +
                $"Status: {(int)firstPick.StatusCode} " +
                $"{firstPick.StatusCode}. " +
                $"Body: {body}");
        }

        var secondPick =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/picked",
                null,
                TestContext.Current.CancellationToken);

        if (secondPick.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await secondPick.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Second picking completion failed. " +
                $"Status: {(int)secondPick.StatusCode} " +
                $"{secondPick.StatusCode}. " +
                $"Body: {body}");
        }

        var secondJson =
            await secondPick.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        secondJson
            .GetProperty("status")
            .GetString()
            .ShouldBe("Picked");

        secondJson
            .GetProperty("alreadyProcessed")
            .GetBoolean()
            .ShouldBeTrue();

        await using var inventoryScope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            inventoryScope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        var stock =
            await inventoryDb.WarehouseStocks
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.WarehouseStockId,
                    TestContext.Current.CancellationToken);

        stock.OnHandQuantity
            .ShouldBe(8);

        stock.ReservedQuantity
            .ShouldBe(0);

        var movementCount =
            await inventoryDb.WarehouseStockMovements
                .CountAsync(
                    x =>
                        x.ReferenceType ==
                            "WarehousePicking" &&
                        x.ReferenceId ==
                            scenario.OrderId.ToString(),
                    TestContext.Current.CancellationToken);

        movementCount
            .ShouldBe(1);
    }

    private async Task<PickingScenario> CreateScenarioAsync(
        bool createWarehouseReservation)
    {
        var userId =
            $"picking-{Guid.NewGuid():N}";

        var productVariantId =
            Guid.NewGuid();

        // ============================================================
        // Inventory: Warehouse
        // ============================================================

        await using var inventoryScope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            inventoryScope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        await inventoryDb.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        var warehouseCode =
            $"PICK-{Guid.NewGuid():N}"[..20];

        var warehouse =
            Warehouse.Create(
                TenantId,
                warehouseCode,
                "Picking Test Warehouse",
                isDefault: true);

        await inventoryDb.Warehouses.AddAsync(
            warehouse,
            TestContext.Current.CancellationToken);

        var locationCode =
            $"LOC-{Guid.NewGuid():N}"[..20];

        var location =
            WarehouseLocation.Create(
                TenantId,
                warehouse.Id,
                locationCode,
                "Picking Location");

        await inventoryDb.WarehouseLocations.AddAsync(
            location,
            TestContext.Current.CancellationToken);

        var stock =
            WarehouseStock.Create(
                TenantId,
                warehouse.Id,
                location.Id,
                productVariantId,
                onHandQuantity: 10,
                reservedQuantity: 2);

        await inventoryDb.WarehouseStocks.AddAsync(
            stock,
            TestContext.Current.CancellationToken);

        await inventoryDb.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // ============================================================
        // Orders: Order + Inventory Reservation + Fulfillment
        // ============================================================

        var order =
            Order.Create(
                TenantId,
                userId,
                $"PICK-{Guid.NewGuid():N}",
                $"pick-{Guid.NewGuid():N}",
                "IRR",
                100_000m,
                20_000m,
                0m,
                "Picking Test User",
                "09000000000",
                "Test Address",
                "Tehran",
                "1234567890");

        order.AddItem(
            productVariantId,
            "PICK-SKU-001",
            "Picking Test Product",
            100_000m,
            2);

        order.MarkPaid();

        order.StartProcessing();

        var orderReservation =
            order.AddInventoryReservation(
                $"inventory-pick-{Guid.NewGuid():N}",
                productVariantId,
                2,
                DateTimeOffset.UtcNow.AddHours(1));

        orderReservation.MarkCommitted();

        var fulfillment =
            Fulfillment.Create(
                order.Id,
                TenantId);

        fulfillment.AssignWarehouse(
            warehouse.Id,
            location.Id);

        await using var orderScope =
            factory.Services.CreateAsyncScope();

        var ordersDb =
            orderScope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        await ordersDb.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        await ordersDb.Orders.AddAsync(
            order,
            TestContext.Current.CancellationToken);

        await ordersDb.Fulfillments.AddAsync(
            fulfillment,
            TestContext.Current.CancellationToken);

        await ordersDb.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // ============================================================
        // Warehouse reservation
        // ============================================================

        if (createWarehouseReservation)
        {
            var warehouseReservation =
                WarehouseStockReservation.Create(
                    TenantId,
                    order.Id,
                    fulfillment.Id,
                    orderReservation.Id,
                    orderReservation.ReservationKey,
                    warehouse.Id,
                    location.Id,
                    productVariantId,
                    2);

            await inventoryDb
                .WarehouseStockReservations
                .AddAsync(
                    warehouseReservation,
                    TestContext.Current.CancellationToken);

            await inventoryDb.SaveChangesAsync(
                TestContext.Current.CancellationToken);

            return new PickingScenario(
                userId,
                order.Id,
                fulfillment.Id,
                warehouse.Id,
                location.Id,
                stock.Id,
                warehouseReservation.Id,
                productVariantId);
        }

        return new PickingScenario(
            userId,
            order.Id,
            fulfillment.Id,
            warehouse.Id,
            location.Id,
            stock.Id,
            Guid.Empty,
            productVariantId);
    }

    private sealed record PickingScenario(
        string UserId,
        Guid OrderId,
        Guid FulfillmentId,
        Guid WarehouseId,
        Guid LocationId,
        Guid WarehouseStockId,
        Guid WarehouseReservationId,
        Guid ProductVariantId);
}
