using System.Net;
using System.Net.Http.Json;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Orders;

[Collection(IntegrationCollection.Name)]
public sealed class OrderEndpointsTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Checkout_without_idempotency_key_returns_bad_request()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "checkout-user-1");

        var response =
            await client.PostAsJsonAsync(
                "/api/orders/checkout",
                new
                {
                    shippingFullName = "Test User",
                    shippingPhone = "09000000000",
                    shippingAddress = "Test Address",
                    shippingCity = "Tehran",
                    shippingPostalCode = "1234567890"
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_with_empty_idempotency_key_returns_bad_request()
    {
        var client =
            factory.CreateAuthenticatedClient(
                "checkout-user-2");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/orders/checkout")
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            shippingFullName =
                                "Test User",
                            shippingPhone =
                                "09000000000",
                            shippingAddress =
                                "Test Address",
                            shippingCity =
                                "Tehran",
                            shippingPostalCode =
                                "1234567890"
                        })
            };

        request.Headers.Add(
            "Idempotency-Key",
            " ");

        var response =
            await client.SendAsync(request);

        response.StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }
}