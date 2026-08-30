using System.Net;
using System.Net.Http.Json;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Orders;

[Collection(IntegrationCollection.Name)]
public sealed class PaymentEndpointsTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Creating_payment_attempt_without_idempotency_key_returns_bad_request()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "payment-user-1");

        var response =
            await client.PostAsJsonAsync(
                "/api/orders/payment-attempts",
                new
                {
                    orderId = Guid.NewGuid()
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Getting_unknown_payment_attempt_returns_not_found()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "payment-user-2");

        var response =
            await client.GetAsync(
                $"/api/orders/payment-attempts/{Guid.NewGuid()}",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Completing_unknown_payment_attempt_returns_not_found()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "payment-user-3");

        var response =
            await client.PostAsJsonAsync(
                "/api/orders/payment/complete",
                new
                {
                    paymentAttemptId = Guid.NewGuid(),
                    gatewayName = "TestGateway",
                    gatewayReference = "REF-001"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }
}