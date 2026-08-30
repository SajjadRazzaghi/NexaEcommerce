using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface IPaymentAttemptRepository
{
    Task<PaymentAttempt?> GetByIdempotencyKeyAsync(
        string tenantId,
        string userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentAttempt?> GetByIdAsync(
        string tenantId,
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PaymentAttempt paymentAttempt,
        CancellationToken cancellationToken = default);
}