﻿using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Tests.Integration.Fixtures;

using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Launch;

[Collection(IntegrationCollection.Name)]
public sealed class LaunchFulfillmentSmokeTests(
    CustomWebApplicationFactory factory)
{
    private const string TenantId = "default";

    [Fact]
    public async Task Realistic_purchase_flow_can_be_fulfilled_from_paid_to_delivered()
    {
        var userId =
            $"launch-fulfillment-{Guid.NewGuid():N}";

        using var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        // ============================================================
        // 1. Locate a real sellable catalog variant
        // ============================================================

        var variantId =
            await GetSellableVariantIdAsync();

        variantId
            .ShouldNotBe(Guid.Empty);

        // ============================================================
        // 2. Read global inventory before purchase
        // ============================================================

        var originalAvailableStock =
            await GetAvailableStockAsync(
                variantId);

        originalAvailableStock
            .ShouldBeGreaterThanOrEqualTo(1);

        // ============================================================
        // 3. Add product to cart
        // ============================================================

        var addCartResponse =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId =
                        variantId,
                    quantity = 1
                },
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            addCartResponse,
            HttpStatusCode.OK,
            "Add product to cart");

        // ============================================================
        // 4. Ensure a shipping method exists
        // ============================================================

        var shippingMethodId =
            await GetOrCreateShippingMethodIdAsync(
                client);

        shippingMethodId
            .ShouldNotBe(Guid.Empty);

        // ============================================================
        // 5. Checkout
        // ============================================================

        var checkoutIdempotencyKey =
            $"launch-fulfillment-checkout-{Guid.NewGuid():N}";

        using var checkoutRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/orders/checkout")
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            items =
                                new[]
                                {
                                    new
                                    {
                                        productVariantId =
                                            variantId,
                                        quantity = 1
                                    }
                                },

                            shippingFullName =
                                "Launch Fulfillment Customer",

                            shippingPhone =
                                "09000000000",

                            shippingAddress =
                                "Launch Fulfillment Test Address",

                            shippingCity =
                                "Tehran",

                            shippingPostalCode =
                                "1234567890",

                            shippingMethodId,

                            couponCode =
                                (string?)null,

                            taxRateId =
                                (Guid?)null
                        })
            };

        checkoutRequest.Headers.TryAddWithoutValidation(
            "Idempotency-Key",
            checkoutIdempotencyKey);

        var checkoutResponse =
            await client.SendAsync(
                checkoutRequest,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            checkoutResponse,
            HttpStatusCode.Created,
            "Checkout");

        var order =
            await checkoutResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var orderId =
            order
                .GetProperty("id")
                .GetGuid();

        orderId
            .ShouldNotBe(Guid.Empty);

        var orderNumber =
            order
                .GetProperty("orderNumber")
                .GetString();

        orderNumber
            .ShouldNotBeNullOrWhiteSpace();

        order
            .GetProperty("status")
            .GetString()
            .ShouldBe("PendingPayment");

        // ============================================================
        // 6. Verify order reservation exists
        // ============================================================

        var reservationBeforePayment =
            await GetOrderReservationAsync(
                orderId,
                variantId);

        reservationBeforePayment
            .ShouldNotBeNull();

        reservationBeforePayment!
            .Status
            .ShouldBe("Reserved");

        reservationBeforePayment
            .Quantity
            .ShouldBe(1);

        // ============================================================
        // 7. Start payment
        // ============================================================

        var paymentIdempotencyKey =
            $"launch-fulfillment-payment-{Guid.NewGuid():N}";


        var callbackUrl =
            new Uri(
                client.BaseAddress!,
                $"/api/orders/payment/zarinpal/callback?orderId={orderId:D}")
                .ToString();



        using var paymentRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/orders/payment/start")
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            orderId,

                            gatewayName =
                                "TestGateway",

                            callbackUrl
                        })
            };

        paymentRequest.Headers.TryAddWithoutValidation(
            "Idempotency-Key",
            paymentIdempotencyKey);

        var paymentResponse =
            await client.SendAsync(
                paymentRequest,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            paymentResponse,
            HttpStatusCode.OK,
            "Start payment");

        var payment =
            await paymentResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var paymentAttemptId =
            payment
                .GetProperty("paymentAttemptId")
                .GetGuid();

        paymentAttemptId
            .ShouldNotBe(Guid.Empty);

        var gatewayReference =
            payment
                .GetProperty("gatewayReference")
                .GetString();

        gatewayReference
            .ShouldNotBeNullOrWhiteSpace();

        // ============================================================
        // 8. Verify payment
        // ============================================================

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/orders/payment/verify",
                new
                {
                    paymentAttemptId,
                    gatewayReference
                },
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            verifyResponse,
            HttpStatusCode.OK,
            "Verify payment");

        // ============================================================
        // 9. Complete payment
        // ============================================================

        var completeResponse =
            await client.PostAsJsonAsync(
                "/api/orders/payment/complete",
                new
                {
                    paymentAttemptId,

                    gatewayName =
                        "TestGateway",

                    gatewayReference
                },
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            completeResponse,
            HttpStatusCode.OK,
            "Complete payment");

        var completedPayment =
            await completeResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        completedPayment
            .GetProperty("status")
            .GetString()
            .ShouldBe("Succeeded");

        // ============================================================
        // 10. Verify payment produced a Paid order
        // ============================================================

        var paidOrder =
            await GetOrderAsync(
                client,
                orderId);

        paidOrder
            .GetProperty("status")
            .GetString()
            .ShouldBe("Paid");

        // ============================================================
        // 11. Verify global inventory was committed
        // ============================================================

        var committedReservation =
            await GetOrderReservationAsync(
                orderId,
                variantId);

        committedReservation
            .ShouldNotBeNull();

        committedReservation!
            .Status
            .ShouldBe("Committed");

        var availableAfterPayment =
            await GetAvailableStockAsync(
                variantId);

        availableAfterPayment
            .ShouldBe(
                originalAvailableStock - 1);

        // ============================================================
        // 12. Prepare a real warehouse for fulfillment
        // ============================================================

        var warehouse =
            await CreateWarehouseAsync(
                $"E2E-{Guid.NewGuid():N}",
                "Launch E2E Warehouse",
                isDefault: true);

        var location =
            await CreateLocationAsync(
                warehouse.Id,
                $"PICK-{Guid.NewGuid():N}",
                "Launch E2E Picking Location");

        const int warehouseInitialQuantity = 2;

        await AddWarehouseStockAsync(
            warehouse.Id,
            location.Id,
            variantId,
            warehouseInitialQuantity);

        // ============================================================
        // 13. Start fulfillment
        // ============================================================

        var startFulfillmentResponse =
       await client.PostAsync(
           $"/api/fulfillment/orders/{orderId}/start",
           null,
           TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            startFulfillmentResponse,
            HttpStatusCode.OK,
            "Start fulfillment");

        await AssertExpectedStatusAsync(
            startFulfillmentResponse,
            HttpStatusCode.OK,
            "Start fulfillment");

        var startFulfillment =
            await startFulfillmentResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        startFulfillment
            .GetProperty("orderStatus")
            .GetString()
            .ShouldBe("Processing");

        startFulfillment
            .GetProperty("fulfillment")
            .GetProperty("status")
            .GetString()
            .ShouldBe("Pending");

        // ============================================================
        // 14. Allocate warehouse
        // ============================================================

        var allocationResponse =
     await client.PutAsJsonAsync(
         $"/api/fulfillment/orders/{orderId}/warehouse",
         new
         {
             warehouseId = warehouse.Id,
             pickingLocationId = location.Id
         },
         TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            allocationResponse,
            HttpStatusCode.OK,
            "Allocate warehouse");

        var allocation =
            await allocationResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        allocation
     .GetProperty("warehouseId")
     .GetGuid()
     .ShouldBe(warehouse.Id);

        allocation
            .GetProperty("pickingLocationId")
            .GetGuid()
            .ShouldBe(location.Id);

        // ============================================================
        // 15. Reserve warehouse stock
        // ============================================================

        var warehouseReserveResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/reserve-stock",
                null,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            warehouseReserveResponse,
            HttpStatusCode.OK,
            "Reserve warehouse stock");

        var warehouseReserve =
            await warehouseReserveResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        warehouseReserve
            .GetProperty("warehouseId")
            .GetGuid()
            .ShouldBe(warehouse.Id);

        warehouseReserve
            .GetProperty("alreadyReserved")
            .GetBoolean()
            .ShouldBeFalse();

        warehouseReserve
            .GetProperty("lines")
            .GetArrayLength()
            .ShouldBe(1);

        // ============================================================
        // 16. Verify warehouse reservation + reserved quantity
        // ============================================================

        var warehouseReservationBeforePicking =
            await GetWarehouseReservationAsync(
                orderId,
                warehouse.Id,
                location.Id,
                variantId);

        warehouseReservationBeforePicking
            .ShouldNotBeNull();

        warehouseReservationBeforePicking!
            .Status
            .ShouldBe("Reserved");

        warehouseReservationBeforePicking
            .Quantity
            .ShouldBe(1);

        var warehouseStockBeforePicking =
            await GetWarehouseStockAsync(
                warehouse.Id,
                location.Id,
                variantId);

        warehouseStockBeforePicking
            .ShouldNotBeNull();

        warehouseStockBeforePicking!
            .OnHandQuantity
            .ShouldBe(warehouseInitialQuantity);

        warehouseStockBeforePicking
            .ReservedQuantity
            .ShouldBe(1);

        warehouseStockBeforePicking
            .AvailableQuantity
            .ShouldBe(1);

        // ============================================================
        // 17. Start picking
        // ============================================================

        var startPickingResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/start-picking",
                null,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            startPickingResponse,
            HttpStatusCode.OK,
            "Start picking");

        var startPicking =
            await startPickingResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        startPicking
            .GetProperty("status")
            .GetString()
            .ShouldBe("Picking");

        // ============================================================
        // 18. Complete picking
        // ============================================================

        var pickedResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/picked",
                null,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            pickedResponse,
            HttpStatusCode.OK,
            "Complete picking");

        var picked =
            await pickedResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        picked
            .GetProperty("status")
            .GetString()
            .ShouldBe("Picked");

        // ============================================================
        // 19. Verify inventory was physically consumed
        // ============================================================

        var warehouseStockAfterPicking =
            await GetWarehouseStockAsync(
                warehouse.Id,
                location.Id,
                variantId);

        warehouseStockAfterPicking
            .ShouldNotBeNull();

        warehouseStockAfterPicking!
            .OnHandQuantity
            .ShouldBe(
                warehouseInitialQuantity - 1);

        warehouseStockAfterPicking
            .ReservedQuantity
            .ShouldBe(0);

        warehouseStockAfterPicking
            .AvailableQuantity
            .ShouldBe(1);

        var warehouseReservationAfterPicking =
            await GetWarehouseReservationAsync(
                orderId,
                warehouse.Id,
                location.Id,
                variantId);

        warehouseReservationAfterPicking
            .ShouldNotBeNull();

        warehouseReservationAfterPicking!
            .Status
            .ShouldBe("Consumed");

        // ============================================================
        // 20. Start packing
        // ============================================================

        var startPackingResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/start-packing",
                null,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            startPackingResponse,
            HttpStatusCode.OK,
            "Start packing");

        var startPacking =
            await startPackingResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        startPacking
            .GetProperty("status")
            .GetString()
            .ShouldBe("Packing");

        startPacking
            .GetProperty("packages")
            .GetArrayLength()
            .ShouldBeGreaterThan(0);

        // ============================================================
        // 21. Complete packing
        // ============================================================

        var packedResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/packed",
                null,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            packedResponse,
            HttpStatusCode.OK,
            "Complete packing");

        var packed =
            await packedResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        packed
            .GetProperty("status")
            .GetString()
            .ShouldBe("Packed");

        packed
            .GetProperty("packages")
            .GetArrayLength()
            .ShouldBeGreaterThan(0);

        // ============================================================
        // 22. Ready to ship
        // ============================================================

        var readyToShipResponse =
            await client.PostAsync(
                $"/api/fulfillment/orders/{orderId}/ready-to-ship",
                null,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            readyToShipResponse,
            HttpStatusCode.OK,
            "Mark ready to ship");

        var readyToShip =
            await readyToShipResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        readyToShip
            .GetProperty("status")
            .GetString()
            .ShouldBe("ReadyToShip");

        // ============================================================
        // 23. Create shipment
        // ============================================================

        var trackingNumber =
            $"NEXA-E2E-{Guid.NewGuid():N}";

        var createShipmentResponse =
            await client.PostAsJsonAsync(
                $"/api/orders/{orderId}/shipment",
                new
                {
                    orderId,

                    shippingMethod =
                        "Launch Express",

                    carrier =
                        "Launch Carrier",

                    trackingNumber
                },
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            createShipmentResponse,
            HttpStatusCode.Created,
            "Create shipment");

        var createdShipment =
            await createShipmentResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        createdShipment
            .GetProperty("status")
            .GetString()
            .ShouldBe("Pending");

        createdShipment
            .GetProperty("trackingNumber")
            .GetString()
            .ShouldBe(trackingNumber);

        // ============================================================
        // 24. Ship order
        // ============================================================

        var shipResponse =
            await client.PostAsync(
                $"/api/orders/{orderId}/shipment/ship",
                null,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            shipResponse,
            HttpStatusCode.OK,
            "Ship order");

        var shipped =
            await shipResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        shipped
            .GetProperty("fulfillmentStatus")
            .GetString()
            .ShouldBe("Shipped");

        shipped
            .GetProperty("shipment")
            .GetProperty("status")
            .GetString()
            .ShouldBe("Shipped");

        shipped
            .GetProperty("alreadyProcessed")
            .GetBoolean()
            .ShouldBeFalse();

        // ============================================================
        // 25. Delivery
        // ============================================================

        var deliverResponse =
            await client.PostAsync(
                $"/api/orders/{orderId}/shipment/deliver",
                null,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            deliverResponse,
            HttpStatusCode.OK,
            "Deliver order");

        var delivered =
            await deliverResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        delivered
            .GetProperty("fulfillmentStatus")
            .GetString()
            .ShouldBe("Delivered");

        delivered
            .GetProperty("shipment")
            .GetProperty("status")
            .GetString()
            .ShouldBe("Delivered");

        delivered
            .GetProperty("alreadyProcessed")
            .GetBoolean()
            .ShouldBeFalse();

        // ============================================================
        // 26. Verify final order state
        // ============================================================

        var finalOrder =
            await GetOrderAsync(
                client,
                orderId);

        finalOrder
            .GetProperty("id")
            .GetGuid()
            .ShouldBe(orderId);

        finalOrder
            .GetProperty("orderNumber")
            .GetString()
            .ShouldBe(orderNumber);

        finalOrder
            .GetProperty("status")
            .GetString()
            .ShouldBe("Delivered");

        // ============================================================
        // 27. Verify final fulfillment state
        // ============================================================

        await using var ordersScope =
            factory.Services.CreateAsyncScope();

        var ordersDb =
            ordersScope.ServiceProvider
                .GetRequiredService<
                    OrdersDbContext>();

        var fulfillment =
            await ordersDb.Fulfillments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.OrderId ==
                            orderId &&
                        x.TenantId ==
                            TenantId,
                    TestContext.Current.CancellationToken);

        fulfillment
            .ShouldNotBeNull();

        fulfillment!
            .Status
            .ShouldBe(
                FulfillmentStatus.Delivered);

        // ============================================================
        // 28. Verify shipment state
        // ============================================================

        var shipment =
            await ordersDb.Shipments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.OrderId ==
                            orderId &&
                        x.TenantId ==
                            TenantId,
                    TestContext.Current.CancellationToken);

        shipment
            .ShouldNotBeNull();

        shipment!
            .Status
            .ShouldBe(
                ShipmentStatus.Delivered);

        shipment
            .TrackingNumber
            .ShouldBe(trackingNumber);

        // ============================================================
        // 29. Verify package reached Delivered
        // ============================================================

        var packages =
            await ordersDb.Packages
                .AsNoTracking()
                .Where(
                    x =>
                        x.OrderId ==
                            orderId &&
                        x.TenantId ==
                            TenantId)
                .ToListAsync(
                    TestContext.Current.CancellationToken);

        packages
            .Count
            .ShouldBeGreaterThan(0);

        packages
            .All(
                x =>
                    x.Status ==
                    PackageStatus.Delivered)
            .ShouldBeTrue();

        packages
            .All(
                x =>
                    x.TrackingNumber ==
                    trackingNumber)
            .ShouldBeTrue();

        // ============================================================
        // 30. Verify warehouse stock final state
        // ============================================================

        var finalWarehouseStock =
            await GetWarehouseStockAsync(
                warehouse.Id,
                location.Id,
                variantId);

        finalWarehouseStock
            .ShouldNotBeNull();

        finalWarehouseStock!
            .OnHandQuantity
            .ShouldBe(warehouseInitialQuantity - 1);

        finalWarehouseStock
            .ReservedQuantity
            .ShouldBe(0);

        finalWarehouseStock
            .AvailableQuantity
            .ShouldBe(1);

        // ============================================================
        // 31. Verify warehouse reservation final state
        // ============================================================

        var finalWarehouseReservation =
            await GetWarehouseReservationAsync(
                orderId,
                warehouse.Id,
                location.Id,
                variantId);

        finalWarehouseReservation
            .ShouldNotBeNull();

        finalWarehouseReservation!
            .Status
            .ShouldBe("Consumed");

        finalWarehouseReservation
            .Quantity
            .ShouldBe(1);
    }

    private async Task<Guid>
        GetSellableVariantIdAsync()
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var catalogDb =
            scope.ServiceProvider
                .GetRequiredService<
                    CatalogDbContext>();

        var variantId =
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
                .FirstOrDefaultAsync(
                    TestContext.Current.CancellationToken);

        variantId
            .ShouldNotBe(Guid.Empty);

        return variantId;
    }

    private async Task<Guid>
        GetOrCreateShippingMethodIdAsync(
            HttpClient client)
    {
        var response =
            await client.GetAsync(
                "/api/shipping-methods/",
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            response,
            HttpStatusCode.OK,
            "Get shipping methods");

        var methods =
            await response.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        methods
            .ValueKind
            .ShouldBe(JsonValueKind.Array);

        foreach (
            var method
            in methods.EnumerateArray())
        {
            if (
                !method.TryGetProperty(
                    "id",
                    out var idProperty))
            {
                continue;
            }

            if (
                !method.TryGetProperty(
                    "isActive",
                    out var activeProperty))
            {
                continue;
            }

            if (!activeProperty.GetBoolean())
            {
                continue;
            }

            var id =
                idProperty.GetGuid();

            if (id != Guid.Empty)
            {
                return id;
            }
        }

        var createRequest =
            new
            {
                code =
                    $"E2E-{Guid.NewGuid():N}"
                        .ToUpperInvariant(),

                name =
                    "Launch E2E Shipping",

                carrier =
                    "Launch E2E Carrier",

                price =
                    25_000m,

                sortOrder =
                    1
            };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/shipping-methods/",
                createRequest,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            createResponse,
            HttpStatusCode.Created,
            "Create E2E shipping method");

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var createdId =
            created
                .GetProperty("id")
                .GetGuid();

        createdId
            .ShouldNotBe(Guid.Empty);

        return createdId;
    }

    private async Task<JsonElement>
        GetOrderAsync(
            HttpClient client,
            Guid orderId)
    {
        var response =
            await client.GetAsync(
                $"/api/orders/{orderId}",
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            response,
            HttpStatusCode.OK,
            "Get order");

        return await response.Content
            .ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);
    }

    private async Task<int>
        GetAvailableStockAsync(
            Guid variantId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var stock =
            await inventoryDb.StockItems
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId ==
                            TenantId &&
                        x.ProductVariantId ==
                            variantId,
                    TestContext.Current.CancellationToken);

        stock
            .ShouldNotBeNull();

        return stock!
            .AvailableQuantity;
    }

    private async Task<Warehouse>
        CreateWarehouseAsync(
            string code,
            string name,
            bool isDefault)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        await inventoryDb.Database
            .EnsureCreatedAsync(
                TestContext.Current.CancellationToken);

        var normalizedCode =
            code.Trim()
                .ToUpperInvariant();

        if (normalizedCode.Length > 32)
        {
            normalizedCode =
                normalizedCode[..23] +
                "-" +
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant();
        }

        var warehouse =
            Warehouse.Create(
                TenantId,
                normalizedCode,
                name,
                isDefault: isDefault);

        await inventoryDb.Warehouses.AddAsync(
            warehouse,
            TestContext.Current.CancellationToken);

        await inventoryDb.SaveChangesAsync(
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

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var normalizedCode =
            code.Trim()
                .ToUpperInvariant();

        if (normalizedCode.Length > 64)
        {
            normalizedCode =
                normalizedCode[..55] +
                "-" +
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant();
        }

        var location =
            WarehouseLocation.Create(
                TenantId,
                warehouseId,
                normalizedCode,
                name);

        await inventoryDb.WarehouseLocations
            .AddAsync(
                location,
                TestContext.Current.CancellationToken);

        await inventoryDb.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return location;
    }

    private async Task
        AddWarehouseStockAsync(
            Guid warehouseId,
            Guid locationId,
            Guid productVariantId,
            int quantity)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var stock =
            WarehouseStock.Create(
                TenantId,
                warehouseId,
                locationId,
                productVariantId,
                onHandQuantity:
                    quantity);

        await inventoryDb.WarehouseStocks
            .AddAsync(
                stock,
                TestContext.Current.CancellationToken);

        await inventoryDb.SaveChangesAsync(
            TestContext.Current.CancellationToken);
    }

    private async Task<WarehouseStockSnapshot?>
        GetWarehouseStockAsync(
            Guid warehouseId,
            Guid locationId,
            Guid productVariantId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var stock =
            await inventoryDb.WarehouseStocks
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId ==
                            TenantId &&
                        x.WarehouseId ==
                            warehouseId &&
                        x.LocationId ==
                            locationId &&
                        x.ProductVariantId ==
                            productVariantId,
                    TestContext.Current.CancellationToken);

        return stock is null
            ? null
            : new WarehouseStockSnapshot(
                stock.OnHandQuantity,
                stock.ReservedQuantity,
                stock.AvailableQuantity);
    }

    private async Task<WarehouseReservationSnapshot?>
        GetWarehouseReservationAsync(
            Guid orderId,
            Guid warehouseId,
            Guid locationId,
            Guid productVariantId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var reservation =
            await inventoryDb
                .WarehouseStockReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId ==
                            TenantId &&
                        x.OrderId ==
                            orderId &&
                        x.WarehouseId ==
                            warehouseId &&
                        x.LocationId ==
                            locationId &&
                        x.ProductVariantId ==
                            productVariantId,
                    TestContext.Current.CancellationToken);

        return reservation is null
            ? null
            : new WarehouseReservationSnapshot(
                reservation.Quantity,
                reservation.Status.ToString());
    }

    private async Task<OrderReservationSnapshot?>
        GetOrderReservationAsync(
            Guid orderId,
            Guid productVariantId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var ordersDb =
            scope.ServiceProvider
                .GetRequiredService<
                    OrdersDbContext>();

        var order =
            await ordersDb.Orders
                .AsNoTracking()
                .Include(
                    x =>
                        x.InventoryReservations)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id ==
                            orderId &&
                        x.TenantId ==
                            TenantId,
                    TestContext.Current.CancellationToken);

        if (order is null)
        {
            return null;
        }

        var reservation =
            order.InventoryReservations
                .FirstOrDefault(
                    x =>
                        x.ProductVariantId ==
                        productVariantId);

        return reservation is null
            ? null
            : new OrderReservationSnapshot(
                reservation.Quantity,
                reservation.Status.ToString());
    }

    private static async Task
        AssertExpectedStatusAsync(
            HttpResponseMessage response,
            HttpStatusCode expected,
            string operation)
    {
        if (response.StatusCode ==
            expected)
        {
            return;
        }

        var body =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        throw new Xunit.Sdk.XunitException(
            $"{operation} failed. " +
            $"Expected {(int)expected} {expected}, " +
            $"received {(int)response.StatusCode} " +
            $"{response.StatusCode}. " +
            $"Body: {body}");
    }

    private sealed record WarehouseStockSnapshot(
        int OnHandQuantity,
        int ReservedQuantity,
        int AvailableQuantity);

    private sealed record WarehouseReservationSnapshot(
        int Quantity,
        string Status);

    private sealed record OrderReservationSnapshot(
        int Quantity,
        string Status);
}

