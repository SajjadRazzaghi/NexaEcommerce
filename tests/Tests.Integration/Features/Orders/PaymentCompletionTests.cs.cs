//using System.Net;
//using System.Net.Http.Json;
//using NexaECommerce.Tests.Integration.Fixtures;
//using Shouldly;

//namespace NexaECommerce.Tests.Integration.Features.Orders;

//[Collection(IntegrationCollection.Name)]
//public sealed class PaymentCompletionTests(
//    CustomWebApplicationFactory factory)
//{
//    [Fact]
//    public async Task Missing_payment_attempt_returns_not_found()
//    {
//        var client =
//            factory.CreateAuthenticatedClient(
//                "payment-completion-user-1");

//        var response =
//            await client.PostAsJsonAsync(
//                "/api/orders/payment/complete",
//                new
//                {
//                    paymentAttemptId =
//                        Guid.NewGuid(),
//                    gatewayName =
//                        "TestGateway",
//                    gatewayReference =
//                        "REF-1"
//                });

//        response.StatusCode
//            .ShouldBe(HttpStatusCode.NotFound);
//    }
//}