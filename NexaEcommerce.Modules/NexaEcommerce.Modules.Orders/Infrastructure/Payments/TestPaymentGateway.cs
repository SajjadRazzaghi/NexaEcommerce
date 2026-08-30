using System.Security.Cryptography;
using System.Text;
using NexaEcommerce.Modules.Orders.Application.Payments;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Payments;

public sealed class TestPaymentGateway
    : IPaymentGateway
{
    public const string GatewayName =
        "TestGateway";

    public string Name =>
        GatewayName;

    public Task<PaymentGatewayCreateResult> CreateAsync(
        PaymentGatewayCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Amount <= 0)
        {
            return Task.FromResult(
                new PaymentGatewayCreateResult(
                    false,
                    null,
                    null,
                    "INVALID_AMOUNT",
                    "Payment amount must be greater than zero."));
        }

        var reference =
            Convert.ToHexString(
                RandomNumberGenerator.GetBytes(12));

        var payload =
            $"{request.MerchantOrderId}:{reference}";

        var token =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(payload));

        var paymentUrl =
            $"/api/orders/payment/test/{Uri.EscapeDataString(token)}";

        return Task.FromResult(
            new PaymentGatewayCreateResult(
                true,
                paymentUrl,
                reference,
                null,
                null));
    }

    public Task<PaymentGatewayVerifyResult> VerifyAsync(
        PaymentGatewayVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(
                request.GatewayReference))
        {
            return Task.FromResult(
                new PaymentGatewayVerifyResult(
                    false,
                    null,
                    "MISSING_REFERENCE",
                    "Gateway reference is required."));
        }

        return Task.FromResult(
            new PaymentGatewayVerifyResult(
                true,
                request.GatewayReference.Trim(),
                null,
                null));
    }
}