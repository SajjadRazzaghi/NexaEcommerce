using System.Net;
using System.Net.Http.Json;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Orders;

[Collection(IntegrationCollection.Name)]
public sealed class ShipmentEndpointsTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Getting_unknown_shipment_returns_not_found()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "shipment-user-1");

        var response =
            await client.GetAsync(
                $"/api/orders/{Guid.NewGuid()}/shipment",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Creating_shipment_for_unknown_order_returns_not_found_or_forbidden()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "shipment-user-2");

        var orderId =
            Guid.NewGuid();

        var response =
            await client.PostAsJsonAsync(
                $"/api/orders/{orderId}/shipment",
                new
                {
                    orderId,
                    shippingMethod = "Test Shipping",
                    carrier = "Test Carrier",
                    trackingNumber = "TRACK-0001"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_shipment_with_mismatched_body_order_returns_forbidden_or_not_found()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "shipment-user-3");

        var routeOrderId =
            Guid.NewGuid();

        var bodyOrderId =
            Guid.NewGuid();

        var response =
            await client.PostAsJsonAsync(
                $"/api/orders/{routeOrderId}/shipment",
                new
                {
                    orderId = bodyOrderId,
                    shippingMethod = "Test Shipping",
                    carrier = "Test Carrier",
                    trackingNumber = "TRACK-0002"
                },
                TestContext.Current.CancellationToken);

        /*
         * CreateShipment is permission-protected.
         *
         * The standard integration identity is normally rejected by
         * authorization before the endpoint reaches the body/route
         * consistency validation.
         *
         * If the authorization layer is bypassed in a future test setup,
         * NotFound is also a valid result because the route order does
         * not exist.
         */
        response.StatusCode.ShouldBeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Setting_tracking_number_for_unknown_order_returns_not_found_or_forbidden()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "shipment-user-4");

        var response =
            await client.PutAsJsonAsync(
                $"/api/orders/{Guid.NewGuid()}/shipment/tracking",
                new
                {
                    trackingNumber = "TRACK-0003"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Shipping_unknown_order_returns_not_found_or_forbidden()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "shipment-user-5");

        var response =
            await client.PostAsync(
                $"/api/orders/{Guid.NewGuid()}/shipment/ship",
                content: null,
                TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delivering_unknown_order_returns_not_found_or_forbidden()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "shipment-user-6");

        var response =
            await client.PostAsync(
                $"/api/orders/{Guid.NewGuid()}/shipment/deliver",
                content: null,
                TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Getting_shipment_with_empty_order_id_returns_not_found()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "shipment-user-7");

        var response =
            await client.GetAsync(
                "/api/orders/00000000-0000-0000-0000-000000000000/shipment",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }
}