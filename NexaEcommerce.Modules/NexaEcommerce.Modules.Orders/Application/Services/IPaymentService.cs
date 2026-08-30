using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IPaymentService
{
    Task<CreatePaymentResultDto> CreatePaymentAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string idempotencyKey,
        string gatewayName,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    Task<PaymentAttemptDto> VerifyPaymentAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        string gatewayReference,
        CancellationToken cancellationToken = default);
}