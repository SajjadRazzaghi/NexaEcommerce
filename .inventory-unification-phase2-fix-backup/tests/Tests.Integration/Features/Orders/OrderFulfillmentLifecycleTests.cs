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
public sealed class OrderFulfillmentLifecycleTests(
    CustomWebApplicationFactory factory)
{
    private const string TenantId = "default";
    [Fact]
    public async Task Full_fulfillment_lifecycle_moves_order_to_delivered()
    {
        var userId =
            $"fulfillment-{Guid.NewGuid():N}";

        var orderId =
            await CreateProcessingOrderAsync(
                userId);

        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var initialTrackingNumber =
            $"TRACK-E2E-{Guid.NewGuid():N}";

        var updatedTrackingNumber =
            $"TRACK-E2E-UPDATED-{Guid.NewGuid():N}";

        // ------------------------------------------------------------
        // 1. Create shipment
        // ------------------------------------------------------------

        var createResponse =
            await client.PostAsJsonAsync(
                $"/api/orders/{orderId}/shipment",
                new
                {
                    orderId,
                    shippingMethod = "Test Shipping",
                    carrier = "Test Carrier",
                    trackingNumber = initialTrackingNumber
                },
                TestContext.Current.CancellationToken);

        createResponse.StatusCode
            .ShouldBe(HttpStatusCode.Created);

        var createJson =
            await createResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var shipmentId =
            createJson
                .GetProperty("id")
                .GetGuid();

        shipmentId
            .ShouldNotBe(Guid.Empty);

        createJson
            .GetProperty("status")
            .GetString()
            .ShouldBe("Pending");

        createJson
            .GetProperty("trackingNumber")
            .GetString()
            .ShouldBe(initialTrackingNumber);

        // ------------------------------------------------------------
        // 2. Update tracking number
        // ------------------------------------------------------------

        var trackingResponse =
            await client.PutAsJsonAsync(
                $"/api/orders/{orderId}/shipment/tracking",
                new
                {
                    trackingNumber = updatedTrackingNumber
                },
                TestContext.Current.CancellationToken);

        if (trackingResponse.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await trackingResponse.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Set tracking failed. " +
                $"Status: {(int)trackingResponse.StatusCode} " +
                $"{trackingResponse.StatusCode}. " +
                $"Body: {body}");
        }

        var trackingJson =
            await trackingResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        trackingJson
            .GetProperty("trackingNumber")
            .GetString()
            .ShouldBe(updatedTrackingNumber);

        // ------------------------------------------------------------
        // 3. Ship
        // ------------------------------------------------------------

        var shipResponse =
            await client.PostAsync(
                $"/api/orders/{orderId}/shipment/ship",
                content: null,
                TestContext.Current.CancellationToken);

        shipResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var shipJson =
            await shipResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        shipJson
            .GetProperty("status")
            .GetString()
            .ShouldBe("Shipped");

        // ------------------------------------------------------------
        // 4. Deliver
        // ------------------------------------------------------------

        var deliverResponse =
            await client.PostAsync(
                $"/api/orders/{orderId}/shipment/deliver",
                content: null,
                TestContext.Current.CancellationToken);

        deliverResponse.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var deliverJson =
            await deliverResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        deliverJson
            .GetProperty("status")
            .GetString()
            .ShouldBe("Delivered");

        // ------------------------------------------------------------
        // 5. Verify persisted Order + Shipment state
        // ------------------------------------------------------------

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var persistedOrder =
            await db.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        persistedOrder
            .ShouldNotBeNull();

        persistedOrder!
            .Status
            .ShouldBe(OrderStatus.Delivered);

        var shipment =
            await db.Shipments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.OrderId == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        shipment
            .ShouldNotBeNull();

        shipment!
            .TrackingNumber
            .ShouldBe(updatedTrackingNumber);

        shipment.Status
            .ToString()
            .ShouldBe("Delivered");

        shipment.ShippedAt
            .ShouldNotBeNull();

        shipment.DeliveredAt
            .ShouldNotBeNull();
    }
    [Fact]
    public async Task Creating_the_same_shipment_twice_is_idempotent()
    {
        var userId =
            $"shipment-idempotency-{Guid.NewGuid():N}";

        var orderId =
            await CreateProcessingOrderAsync(
                userId);

        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var trackingNumber =
            $"TRACK-IDEMPOTENT-{Guid.NewGuid():N}";

        var request =
            new
            {
                orderId,
                shippingMethod = "Test Shipping",
                carrier = "Test Carrier",
                trackingNumber
            };

        var firstResponse =
            await client.PostAsJsonAsync(
                $"/api/orders/{orderId}/shipment",
                request,
                TestContext.Current.CancellationToken);

        if (firstResponse.StatusCode !=
            HttpStatusCode.Created)
        {
            var body =
                await firstResponse.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"First shipment creation failed. " +
                $"Status: {(int)firstResponse.StatusCode} " +
                $"{firstResponse.StatusCode}. " +
                $"Body: {body}");
        }

        var firstJson =
            await firstResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var firstShipmentId =
            firstJson
                .GetProperty("id")
                .GetGuid();

        firstShipmentId
            .ShouldNotBe(Guid.Empty);

        firstJson
            .GetProperty("trackingNumber")
            .GetString()
            .ShouldBe(trackingNumber);

        var secondResponse =
            await client.PostAsJsonAsync(
                $"/api/orders/{orderId}/shipment",
                request,
                TestContext.Current.CancellationToken);

        if (secondResponse.StatusCode !=
            HttpStatusCode.Created)
        {
            var body =
                await secondResponse.Content
                    .ReadAsStringAsync(
                        TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Second shipment creation failed. " +
                $"Status: {(int)secondResponse.StatusCode} " +
                $"{secondResponse.StatusCode}. " +
                $"Body: {body}");
        }

        var secondJson =
            await secondResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var secondShipmentId =
            secondJson
                .GetProperty("id")
                .GetGuid();

        secondShipmentId
            .ShouldBe(firstShipmentId);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var shipmentCount =
            await db.Shipments
                .CountAsync(
                    x =>
                        x.OrderId == orderId &&
                        x.TenantId == TenantId,
                    TestContext.Current.CancellationToken);

        shipmentCount
            .ShouldBe(1);
    }
    [Fact]
    public async Task Generic_status_endpoint_cannot_fake_paid()
    {
        var userId =
            $"status-paid-{Guid.NewGuid():N}";

        var orderId =
            await CreateProcessingOrderAsync(
                userId);

        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var response =
            await client.PutAsJsonAsync(
                $"/api/orders/{orderId}/status",
                new
                {
                    status = "Paid"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Generic_status_endpoint_cannot_fake_shipped()
    {
        var userId =
            $"status-shipped-{Guid.NewGuid():N}";

        var orderId =
            await CreateProcessingOrderAsync(
                userId);

        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var response =
            await client.PutAsJsonAsync(
                $"/api/orders/{orderId}/status",
                new
                {
                    status = "Shipped"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Generic_status_endpoint_cannot_fake_delivered()
    {
        var userId =
            $"status-delivered-{Guid.NewGuid():N}";

        var orderId =
            await CreateProcessingOrderAsync(
                userId);

        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var response =
            await client.PutAsJsonAsync(
                $"/api/orders/{orderId}/status",
                new
                {
                    status = "Delivered"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Shipment_cannot_be_created_for_paid_order()
    {
        var userId =
            $"shipment-paid-{Guid.NewGuid():N}";

        var orderId =
            await CreatePaidOrderAsync(
                userId);

        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var response =
            await client.PostAsJsonAsync(
                $"/api/orders/{orderId}/shipment",
                new
                {
                    orderId,
                    shippingMethod = "Test Shipping",
                    carrier = "Test Carrier",
                    trackingNumber = "TRACK-PAID"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Ship_requires_processing_order_and_existing_shipment()
    {
        var userId =
            $"ship-validation-{Guid.NewGuid():N}";

        var orderId =
            await CreatePaidOrderAsync(
                userId);

        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/orders/{orderId}/shipment/ship",
                content: null,
                TestContext.Current.CancellationToken);

        /*
         * The shipment endpoint delegates to ShipmentService.
         *
         * ShipmentService looks up the shipment before attempting
         * the state transition. Since this Paid order has no shipment,
         * the service returns KeyNotFoundException and the endpoint maps
         * that exception to HTTP 404 NotFound.
         */
        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task Deliver_requires_shipped_order()
    {
        var userId =
            $"deliver-validation-{Guid.NewGuid():N}";

        /*
         * Shipment can only be created for an order in Processing.
         *
         * We therefore create a Processing order here and deliberately
         * attempt Deliver before Ship.
         */
        var orderId =
            await CreateProcessingOrderAsync(
                userId);

        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        var createResponse =
            await client.PostAsJsonAsync(
                $"/api/orders/{orderId}/shipment",
                new
                {
                    orderId,
                    shippingMethod = "Test Shipping",
                    carrier = "Test Carrier",
                    trackingNumber = $"TRACK-DELIVER-{Guid.NewGuid():N}"
                },
                TestContext.Current.CancellationToken);

        if (createResponse.StatusCode !=
            HttpStatusCode.Created)
        {
            var body =
                await createResponse.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Create shipment failed. " +
                $"Status: {(int)createResponse.StatusCode} " +
                $"{createResponse.StatusCode}. " +
                $"Body: {body}");
        }

        var response =
            await client.PostAsync(
                $"/api/orders/{orderId}/shipment/deliver",
                content: null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }
    private async Task<Guid> CreateProcessingOrderAsync(
        string userId)
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
            "TEST-SKU-001",
            "Integration Test Product",
            100_000m,
            1);

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

        return order.Id;
    }

    private async Task<Guid> CreatePaidOrderAsync(
        string userId)
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
            "TEST-SKU-002",
            "Integration Test Product",
            100_000m,
            1);

        order.MarkPaid();

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

        return order.Id;
    }
}