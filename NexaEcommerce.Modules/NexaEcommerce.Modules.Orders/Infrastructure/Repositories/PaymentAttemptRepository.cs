using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class PaymentAttemptRepository(
    OrdersDbContext context)
    : IPaymentAttemptRepository
{
    public async Task<PaymentAttempt?>
        GetByIdempotencyKeyAsync(
            string tenantId,
            string userId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
    {
        return await context.PaymentAttempts
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId &&
                    x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<PaymentAttempt?>
        GetByIdAsync(
            string tenantId,
            string userId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        return await context.PaymentAttempts
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId &&
                    x.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        PaymentAttempt paymentAttempt,
        CancellationToken cancellationToken = default)
    {
        await context.PaymentAttempts.AddAsync(
            paymentAttempt,
            cancellationToken);
    }
}