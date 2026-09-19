
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
public sealed class WarehouseShipmentTests(
    CustomWebApplicationFactory factory)
{
    private const string TenantId = "default";

    [Fact]
    public async Task Shipping_requires_ready_to_ship_fulfillment()
    {
        var scenario = await CreateScenarioAsync(
     FulfillmentStatus.Packed,
     null,
     OrderStatus.Processing,
     ShipmentStatus.Pending);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/orders/{scenario.OrderId}/shipment/ship",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Shipping_requires_tracking_number()
    {
        var scenario =
            await CreateScenarioAsync(
                fulfillmentStatus:
                    FulfillmentStatus.ReadyToShip,
                trackingNumber: null);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/orders/{scenario.OrderId}/shipment/ship",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Shipping_updates_order_shipment_and_fulfillment()
    {
        var scenario =
            await CreateScenarioAsync(
                fulfillmentStatus:
                    FulfillmentStatus.ReadyToShip,
                trackingNumber:
                    "TRK-001");

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/orders/{scenario.OrderId}/shipment/ship",
                null,
                TestContext.Current.CancellationToken);

        if (response.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Shipping failed. " +
                $"Status: {(int)response.StatusCode} " +
                $"{response.StatusCode}. " +
                $"Body: {body}");
        }

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("fulfillmentStatus")
            .GetString()
            .ShouldBe("Shipped");

        json.GetProperty("alreadyProcessed")
            .GetBoolean()
            .ShouldBeFalse();

        json.GetProperty("shipment")
            .GetProperty("status")
            .GetString()
            .ShouldBe("Shipped");

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var order =
            await db.Orders
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.OrderId,
                    TestContext.Current.CancellationToken);

        order.Status
            .ShouldBe(
                OrderStatus.Shipped);

        var fulfillment =
            await db.Fulfillments
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.FulfillmentId,
                    TestContext.Current.CancellationToken);

        fulfillment.Status
            .ShouldBe(
                FulfillmentStatus.Shipped);

        var shipment =
            await db.Shipments
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.OrderId ==
                        scenario.OrderId,
                    TestContext.Current.CancellationToken);

        shipment.Status
            .ShouldBe(
                ShipmentStatus.Shipped);

        shipment.TrackingNumber
            .ShouldBe("TRK-001");
    }

    [Fact]
    public async Task Shipping_twice_is_idempotent()
    {
        var scenario =
            await CreateScenarioAsync(
                fulfillmentStatus:
                    FulfillmentStatus.ReadyToShip,
                trackingNumber:
                    "TRK-002");

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var first =
            await client.PostAsync(
                $"/api/orders/{scenario.OrderId}/shipment/ship",
                null,
                TestContext.Current.CancellationToken);

        first.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var second =
            await client.PostAsync(
                $"/api/orders/{scenario.OrderId}/shipment/ship",
                null,
                TestContext.Current.CancellationToken);

        if (second.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await second.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Second shipping request failed. " +
                $"Status: {(int)second.StatusCode} " +
                $"{second.StatusCode}. " +
                $"Body: {body}");
        }

        var json =
            await second.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("fulfillmentStatus")
            .GetString()
            .ShouldBe("Shipped");

        json.GetProperty("alreadyProcessed")
            .GetBoolean()
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Delivery_updates_order_shipment_and_fulfillment()
    {
        var scenario =
            await CreateScenarioAsync(
                fulfillmentStatus:
                    FulfillmentStatus.Shipped,
                trackingNumber:
                    "TRK-003",
                orderStatus:
                    OrderStatus.Shipped,
                shipmentStatus:
                    ShipmentStatus.Shipped);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/orders/{scenario.OrderId}/shipment/deliver",
                null,
                TestContext.Current.CancellationToken);

        if (response.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Delivery failed. " +
                $"Status: {(int)response.StatusCode} " +
                $"{response.StatusCode}. " +
                $"Body: {body}");
        }

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("fulfillmentStatus")
            .GetString()
            .ShouldBe("Delivered");

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var order =
            await db.Orders
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.OrderId,
                    TestContext.Current.CancellationToken);

        order.Status
            .ShouldBe(
                OrderStatus.Delivered);

        var fulfillment =
            await db.Fulfillments
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.FulfillmentId,
                    TestContext.Current.CancellationToken);

        fulfillment.Status
            .ShouldBe(
                FulfillmentStatus.Delivered);

        var shipment =
            await db.Shipments
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.OrderId ==
                        scenario.OrderId,
                    TestContext.Current.CancellationToken);

        shipment.Status
            .ShouldBe(
                ShipmentStatus.Delivered);
    }

    [Fact]
    public async Task Delivery_twice_is_idempotent()
    {
        var scenario =
            await CreateScenarioAsync(
                fulfillmentStatus:
                    FulfillmentStatus.Shipped,
                trackingNumber:
                    "TRK-004",
                orderStatus:
                    OrderStatus.Shipped,
                shipmentStatus:
                    ShipmentStatus.Shipped);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var first =
            await client.PostAsync(
                $"/api/orders/{scenario.OrderId}/shipment/deliver",
                null,
                TestContext.Current.CancellationToken);

        first.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var second =
            await client.PostAsync(
                $"/api/orders/{scenario.OrderId}/shipment/deliver",
                null,
                TestContext.Current.CancellationToken);

        if (second.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await second.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Second delivery request failed. " +
                $"Status: {(int)second.StatusCode} " +
                $"{second.StatusCode}. " +
                $"Body: {body}");
        }

        var json =
            await second.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("fulfillmentStatus")
            .GetString()
            .ShouldBe("Delivered");

        json.GetProperty("alreadyProcessed")
            .GetBoolean()
            .ShouldBeTrue();
    }

    private async Task<ShipmentScenario> CreateScenarioAsync(
        FulfillmentStatus fulfillmentStatus,
        string? trackingNumber,
        OrderStatus orderStatus = OrderStatus.Processing,
        ShipmentStatus shipmentStatus = ShipmentStatus.Pending)
    {
        var userId =
            $"shipment-{Guid.NewGuid():N}";

        var order =
            Order.Create(
                TenantId,
                userId,
                $"SHIP-{Guid.NewGuid():N}",
                $"ship-{Guid.NewGuid():N}",
                "IRR",
                100_000m,
                20_000m,
                0m,
                "Shipment Test User",
                "09000000000",
                "Test Address",
                "Tehran",
                "1234567890");

        order.AddItem(
            Guid.NewGuid(),
            "SHIP-SKU-001",
            "Shipment Test Product",
            100_000m,
            2);

        order.MarkPaid();
        order.StartProcessing();

        if (orderStatus ==
            OrderStatus.Shipped)
        {
            order.MarkShipped();
        }
        else if (orderStatus ==
                 OrderStatus.Delivered)
        {
            order.MarkShipped();
            order.MarkDelivered();
        }

        var fulfillment =
            Fulfillment.Create(
                order.Id,
                TenantId);

        fulfillment.AssignWarehouse(
            Guid.NewGuid());

        fulfillment.StartPicking();
        fulfillment.MarkPicked();
        fulfillment.StartPacking();

        var package =
            Package.Create(
                TenantId,
                order.Id,
                fulfillment.Id,
                1,
                DateTime.UtcNow);

        package.StartPacking(
            DateTime.UtcNow);

        package.MarkPacked(
            DateTime.UtcNow);

        fulfillment.MarkPacked();
        fulfillment.MarkReadyToShip();

        if (fulfillmentStatus ==
            FulfillmentStatus.Shipped)
        {
            fulfillment.MarkShipped();
        }
        else if (fulfillmentStatus ==
                 FulfillmentStatus.Delivered)
        {
            fulfillment.MarkShipped();
            fulfillment.MarkDelivered();
        }

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

        await db.Fulfillments.AddAsync(
            fulfillment,
            TestContext.Current.CancellationToken);

        await db.Packages.AddAsync(
            package,
            TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var shipment =
            Shipment.Create(
                order.Id,
                TenantId,
                "Express",
                "TestCarrier",
                trackingNumber);

        if (shipmentStatus ==
            ShipmentStatus.Shipped)
        {
            shipment.MarkShipped();
        }
        else if (shipmentStatus ==
                 ShipmentStatus.Delivered)
        {
            shipment.MarkShipped();
            shipment.MarkDelivered();
        }

        db.Shipments.Add(
            shipment);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return new ShipmentScenario(
            userId,
            order.Id,
            fulfillment.Id,
            shipment.Id);
    }

    private sealed record ShipmentScenario(
        string UserId,
        Guid OrderId,
        Guid FulfillmentId,
        Guid ShipmentId);
}

