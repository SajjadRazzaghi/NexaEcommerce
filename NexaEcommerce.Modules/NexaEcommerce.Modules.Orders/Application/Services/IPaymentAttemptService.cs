using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IPaymentAttemptService
{
    Task<PaymentAttemptDto> CreateAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentAttemptDto?> GetAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        CancellationToken cancellationToken = default);

    Task<PaymentAttemptDto> MarkSucceededAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        string gatewayName,
        string gatewayReference,
        CancellationToken cancellationToken = default);

    Task<PaymentAttemptDto> MarkFailedAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        string? failureCode,
        string? failureMessage,
        CancellationToken cancellationToken = default);
}