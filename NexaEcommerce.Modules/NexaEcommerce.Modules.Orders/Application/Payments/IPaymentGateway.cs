namespace NexaEcommerce.Modules.Orders.Application.Payments;

public interface IPaymentGateway
{
    string Name { get; }

    Task<PaymentGatewayCreateResult> CreateAsync(
        PaymentGatewayCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayVerifyResult> VerifyAsync(
        PaymentGatewayVerifyRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentGatewayCreateRequest(
    string MerchantOrderId,
    decimal Amount,
    string Currency,
    string CallbackUrl);

public sealed record PaymentGatewayCreateResult(
    bool Succeeded,
    string? PaymentUrl,
    string? GatewayReference,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record PaymentGatewayVerifyRequest(
    string MerchantOrderId,
    decimal Amount,
    string GatewayReference);

public sealed record PaymentGatewayVerifyResult(
    bool Succeeded,
    string? GatewayReference,
    string? ErrorCode,
    string? ErrorMessage);