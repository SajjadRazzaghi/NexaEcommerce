using System.Net;
using System.Net.Http.Json;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Orders;

[Collection(IntegrationCollection.Name)]
public sealed class PaymentCompletionTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Completing_unknown_payment_attempt_returns_not_found()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "payment-completion-user-1");

        var response =
            await client.PostAsJsonAsync(
                "/api/orders/payment/complete",
                new
                {
                    paymentAttemptId =
                        Guid.NewGuid(),
                    gatewayName =
                        "TestGateway",
                    gatewayReference =
                        "REF-001"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Completing_without_payment_attempt_id_returns_bad_request()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "payment-completion-user-2");

        var response =
            await client.PostAsJsonAsync(
                "/api/orders/payment/complete",
                new
                {
                    paymentAttemptId =
                        Guid.Empty,
                    gatewayName =
                        "TestGateway",
                    gatewayReference =
                        "REF-002"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Completing_without_gateway_name_returns_bad_request()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "payment-completion-user-3");

        var response =
            await client.PostAsJsonAsync(
                "/api/orders/payment/complete",
                new
                {
                    paymentAttemptId =
                        Guid.NewGuid(),
                    gatewayName =
                        "",
                    gatewayReference =
                        "REF-003"
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Completing_without_gateway_reference_returns_bad_request()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "payment-completion-user-4");

        var response =
            await client.PostAsJsonAsync(
                "/api/orders/payment/complete",
                new
                {
                    paymentAttemptId =
                        Guid.NewGuid(),
                    gatewayName =
                        "TestGateway",
                    gatewayReference =
                        ""
                },
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Creating_payment_attempt_without_idempotency_key_returns_bad_request()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "payment-completion-user-5");

        var response =
            await client.PostAsJsonAsync(
                "/api/orders/payment-attempts",
                new
                {
                    orderId =
                        Guid.NewGuid()
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
                "payment-completion-user-6");

        var response =
            await client.GetAsync(
                $"/api/orders/payment-attempts/{Guid.NewGuid()}",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }
}