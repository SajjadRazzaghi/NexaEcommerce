using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Tests.Integration.Fixtures;

using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Launch;

[Collection(IntegrationCollection.Name)]
public sealed class LaunchPurchaseSmokeTests(
    CustomWebApplicationFactory factory)
{
    private const string TenantId = "default";

    [Fact]
    public async Task Realistic_purchase_flow_creates_paid_order_and_commits_inventory()
    {
        var userId =
            $"launch-buyer-{Guid.NewGuid():N}";

        /*
         * The smoke test needs access to the shipping-method creation
         * endpoint when the isolated integration database does not have
         * a shipping method seeded yet.
         */
        var client =
            factory.CreateAuthenticatedClient(
                userId,
                PermissionClaims.All);

        // ============================================================
        // 1. Locate a real published/sellable Variant
        // ============================================================

        var variantId =
            await GetSellableVariantIdAsync();

        variantId
            .ShouldNotBe(Guid.Empty);

        // ============================================================
        // 2. Ensure inventory exists
        // ============================================================

        var originalAvailableStock =
            await GetAvailableStockAsync(
                variantId);

        originalAvailableStock
            .ShouldBeGreaterThanOrEqualTo(1);

        // ============================================================
        // 3. Add Variant to Cart
        // ============================================================

        var addResponse =
            await client.PostAsJsonAsync(
                "/api/cart/items",
                new
                {
                    productVariantId =
                        variantId,

                    quantity = 1,
                },
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            addResponse,
            HttpStatusCode.OK,
            "Add to cart");

        // ============================================================
        // 4. Verify Cart
        // ============================================================

        var cartResponse =
            await client.GetAsync(
                "/api/cart/",
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            cartResponse,
            HttpStatusCode.OK,
            "Get cart");

        var cart =
            await cartResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var cartItems =
            cart.GetProperty("items");

        cartItems
            .GetArrayLength()
            .ShouldBe(1);

        var cartItem =
            cartItems[0];

        cartItem
            .GetProperty("productVariantId")
            .GetGuid()
            .ShouldBe(variantId);

        cartItem
            .GetProperty("quantity")
            .GetInt32()
            .ShouldBe(1);

        // ============================================================
        // 5. Get or create an active Shipping Method
        // ============================================================

        var shippingMethodId =
            await GetOrCreateShippingMethodIdAsync(
                client);

        shippingMethodId
            .ShouldNotBe(Guid.Empty);

        // ============================================================
        // 6. Checkout
        // ============================================================

        var checkoutIdempotencyKey =
            $"launch-checkout-{Guid.NewGuid():N}";

        using var checkoutRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/orders/checkout")
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            /*
                             * Checkout uses the server-side Cart as the
                             * source of truth for actual order lines.
                             */
                            items =
                                new[]
                                {
                                    new
                                    {
                                        productVariantId =
                                            variantId,

                                        quantity = 1,
                                    },
                                },

                            shippingFullName =
                                "Launch Test Customer",

                            shippingPhone =
                                "09000000000",

                            shippingAddress =
                                "Launch Test Address",

                            shippingCity =
                                "Tehran",

                            shippingPostalCode =
                                "1234567890",

                            shippingMethodId,

                            couponCode =
                                (string?)null,

                            taxRateId =
                                (Guid?)null,
                        }),
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
        // 7. Verify reservation persisted
        // ============================================================

        var reservationInfo =
            await GetReservationInfoAsync(
                orderId,
                variantId);

        reservationInfo
            .ShouldNotBeNull();

        reservationInfo!
            .Status
            .ShouldBe("Reserved");

        reservationInfo
            .Quantity
            .ShouldBe(1);

        // ============================================================
        // 8. Start Payment
        // ============================================================

        var paymentIdempotencyKey =
            $"launch-payment-{Guid.NewGuid():N}";

        var callbackUrl =
            "https://localhost:3000/orders/payment/" +
            orderId;

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

                            callbackUrl,
                        }),
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
                .GetProperty(
                    "paymentAttemptId")
                .GetGuid();

        paymentAttemptId
            .ShouldNotBe(Guid.Empty);

        var gatewayReference =
            payment
                .GetProperty(
                    "gatewayReference")
                .GetString();

        gatewayReference
            .ShouldNotBeNullOrWhiteSpace();

        payment
            .GetProperty("status")
            .GetString()
            .ShouldBe("Pending");

        // ============================================================
        // 9. Verify Gateway Payment
        // ============================================================

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/orders/payment/verify",
                new
                {
                    paymentAttemptId,

                    gatewayReference,
                },
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            verifyResponse,
            HttpStatusCode.OK,
            "Verify payment");

        var verifiedPayment =
            await verifyResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        verifiedPayment
            .GetProperty("id")
            .GetGuid()
            .ShouldBe(paymentAttemptId);

        /*
         * Verification must not finalize payment.
         */
        verifiedPayment
            .GetProperty("status")
            .GetString()
            .ShouldBe("Pending");

        // ============================================================
        // 10. Complete Payment
        // ============================================================

        var completeResponse =
            await client.PostAsJsonAsync(
                "/api/orders/payment/complete",
                new
                {
                    paymentAttemptId,

                    gatewayName =
                        "TestGateway",

                    gatewayReference,
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

        /*
         * The current PaymentCompletion endpoint returns the payment
         * attempt DTO, so both supported names are checked safely below.
         */
        var completedAttemptId =
            completedPayment.TryGetProperty(
                    "paymentAttemptId",
                    out var paymentAttemptIdProperty)
                ? paymentAttemptIdProperty.GetGuid()
                : completedPayment
                    .GetProperty("id")
                    .GetGuid();

        completedAttemptId
            .ShouldBe(paymentAttemptId);

        completedPayment
            .GetProperty("status")
            .GetString()
            .ShouldBe("Succeeded");

        // ============================================================
        // 11. Verify final Order
        // ============================================================

        var orderResponse =
            await client.GetAsync(
                $"/api/orders/{orderId}",
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            orderResponse,
            HttpStatusCode.OK,
            "Get final order");

        var finalOrder =
            await orderResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

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
            .ShouldBe("Paid");

        // ============================================================
        // 12. Verify inventory is committed
        // ============================================================

        var finalAvailableStock =
            await GetAvailableStockAsync(
                variantId);

        finalAvailableStock
            .ShouldBe(
                originalAvailableStock - 1);

        var finalReservation =
            await GetReservationInfoAsync(
                orderId,
                variantId);

        finalReservation
            .ShouldNotBeNull();

        finalReservation!
            .Status
            .ShouldBe("Committed");

        // ============================================================
        // 13. Verify cart was cleared
        // ============================================================

        var finalCartResponse =
            await client.GetAsync(
                "/api/cart/",
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            finalCartResponse,
            HttpStatusCode.OK,
            "Get cleared cart");

        var finalCart =
            await finalCartResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        finalCart
            .GetProperty("items")
            .GetArrayLength()
            .ShouldBe(0);
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
                method.TryGetProperty(
                    "id",
                    out var idProperty) &&
                method.TryGetProperty(
                    "isActive",
                    out var activeProperty) &&
                activeProperty.GetBoolean())
            {
                var id =
                    idProperty.GetGuid();

                if (
                    id != Guid.Empty)
                {
                    return id;
                }
            }
        }

        /*
         * The isolated integration environment currently does not seed
         * shipping methods. Create one through the real API instead of
         * making the Launch smoke test depend on external seed data.
         */
        var createRequest =
            new
            {
                code =
                    $"TEST-{Guid.NewGuid():N}".ToUpperInvariant(),

                name =
                    "Integration Test Shipping",

                carrier =
                    "Integration Test Carrier",

                price =
                    25_000m,

                sortOrder =
                    1,
            };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/shipping-methods/",
                createRequest,
                TestContext.Current.CancellationToken);

        await AssertExpectedStatusAsync(
            createResponse,
            HttpStatusCode.Created,
            "Create test shipping method");

        var createdMethod =
            await createResponse.Content
                .ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);

        var createdId =
            createdMethod
                .GetProperty("id")
                .GetGuid();

        createdId
            .ShouldNotBe(Guid.Empty);

        return createdId;
    }

    private async Task<ReservationInfo?>
        GetReservationInfoAsync(
            Guid orderId,
            Guid variantId)
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
                        x.Id == orderId &&
                        x.TenantId == TenantId,
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
                        variantId);

        if (reservation is null)
        {
            return null;
        }

        return new ReservationInfo(
            reservation.ReservationKey,
            reservation.Quantity,
            reservation.Status.ToString(),
            reservation.ExpiresAt);
    }

    private static async Task
        AssertExpectedStatusAsync(
            HttpResponseMessage response,
            HttpStatusCode expected,
            string operation)
    {
        if (
            response.StatusCode ==
            expected)
        {
            return;
        }

        var body =
            await response.Content
                .ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

        throw new Xunit.Sdk.XunitException(
            $"{operation} failed. " +
            $"Expected {(int)expected} {expected}, " +
            $"received {(int)response.StatusCode} " +
            $"{response.StatusCode}. " +
            $"Body: {body}");
    }

    private sealed record ReservationInfo(
        string ReservationKey,
        int Quantity,
        string Status,
        DateTimeOffset ExpiresAt);
}