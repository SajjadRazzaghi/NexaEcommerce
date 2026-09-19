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
public sealed class OrderFulfillmentStartTests(
CustomWebApplicationFactory factory)
{
    private const string TenantId =
    "default";


[Fact]
    public async Task Starting_paid_order_creates_processing_fulfillment()
    {
        var userId =
            $"fulfillment-start-{Guid.NewGuid():N}";

        var orderId =
            await CreatePaidOrderWithCommittedInventoryAsync(
                userId);

        using var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/start",
                content: null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        json
            .GetProperty("orderId")
            .GetGuid()
            .ShouldBe(orderId);

        json
            .GetProperty("orderStatus")
            .GetString()
            .ShouldBe("Processing");

        var fulfillment =
            json.GetProperty("fulfillment");

        fulfillment
            .GetProperty("orderId")
            .GetGuid()
            .ShouldBe(orderId);

        fulfillment
            .GetProperty("status")
            .GetString()
            .ShouldBe("Pending");

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var order =
            await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        order.ShouldNotBeNull();

        order!
            .Status
            .ShouldBe(OrderStatus.Processing);

        var fulfillmentCount =
            await db.Fulfillments
                .CountAsync(
                    x =>
                        x.OrderId == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        fulfillmentCount
            .ShouldBe(1);
    }

    [Fact]
    public async Task Starting_processing_order_is_idempotent()
    {
        var userId =
            $"fulfillment-idempotent-{Guid.NewGuid():N}";

        var orderId =
            await CreatePaidOrderWithCommittedInventoryAsync(
                userId);

        using var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var first =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/start",
                content: null,
                TestContext.Current.CancellationToken);

        first.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var firstJson =
            await first.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var firstFulfillmentId =
            firstJson
                .GetProperty("fulfillment")
                .GetProperty("id")
                .GetGuid();

        firstFulfillmentId
            .ShouldNotBe(Guid.Empty);

        var second =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/start",
                content: null,
                TestContext.Current.CancellationToken);

        second.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var secondJson =
            await second.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var secondFulfillmentId =
            secondJson
                .GetProperty("fulfillment")
                .GetProperty("id")
                .GetGuid();

        secondFulfillmentId
            .ShouldBe(firstFulfillmentId);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var fulfillmentCount =
            await db.Fulfillments
                .CountAsync(
                    x =>
                        x.OrderId == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        fulfillmentCount
            .ShouldBe(1);
    }

    [Fact]
    public async Task Starting_paid_order_without_committed_inventory_fails()
    {
        var userId =
            $"fulfillment-no-inventory-{Guid.NewGuid():N}";

        var orderId =
            await CreatePaidOrderWithoutCommittedInventoryAsync(
                userId);

        using var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/start",
                content: null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var order =
            await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        order.ShouldNotBeNull();

        order!
            .Status
            .ShouldBe(OrderStatus.Paid);

        var fulfillmentCount =
            await db.Fulfillments
                .CountAsync(
                    x =>
                        x.OrderId == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        fulfillmentCount
            .ShouldBe(0);
    }

    [Fact]
    public async Task Starting_pending_payment_order_fails()
    {
        var userId =
            $"fulfillment-pending-{Guid.NewGuid():N}";

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

        order.AddItem(
            Guid.NewGuid(),
            "TEST-SKU-PENDING",
            "Integration Test Product",
            100_000m,
            1);

        var orderId =
            order.Id;

        await using (
            var scope =
                factory.Services.CreateAsyncScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        OrdersDbContext>();

            await db.Orders.AddAsync(
                order,
                TestContext.Current.CancellationToken);

            await db.SaveChangesAsync(
                TestContext.Current.CancellationToken);
        }

        using var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/start",
                content: null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    private async Task<Guid>
        CreatePaidOrderWithCommittedInventoryAsync(
            string userId)
    {
        var order =
            CreateBaseOrder(
                userId,
                "TEST-SKU-COMMITTED");

        var reservation =
            order.AddInventoryReservation(
                $"inventory-{Guid.NewGuid():N}",
                order.Items
                    .Single()
                    .ProductVariantId,
                1,
                DateTimeOffset.UtcNow.AddMinutes(10));

        reservation.MarkCommitted();

        order.MarkPaid();

        await SaveOrderAsync(order);

        return order.Id;
    }

    private async Task<Guid>
        CreatePaidOrderWithoutCommittedInventoryAsync(
            string userId)
    {
        var order =
            CreateBaseOrder(
                userId,
                "TEST-SKU-NOT-COMMITTED");

        order.AddInventoryReservation(
            $"inventory-{Guid.NewGuid():N}",
            order.Items
                .Single()
                .ProductVariantId,
            1,
            DateTimeOffset.UtcNow.AddMinutes(10));

        order.MarkPaid();

        await SaveOrderAsync(order);

        return order.Id;
    }

    private static Order CreateBaseOrder(
        string userId,
        string sku)
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

        order.AddItem(
            Guid.NewGuid(),
            sku,
            "Integration Test Product",
            100_000m,
            1);

        return order;
    }

    private async Task SaveOrderAsync(
        Order order)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    OrdersDbContext>();

        await db.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        await db.Orders.AddAsync(
            order,
            TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);
    }


}
