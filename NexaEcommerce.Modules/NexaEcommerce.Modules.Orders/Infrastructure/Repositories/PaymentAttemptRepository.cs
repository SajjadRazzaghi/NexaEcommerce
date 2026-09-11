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

    public async Task<PaymentAttempt?>
        GetByOrderIdAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) ||
            orderId == Guid.Empty)
        {
            return null;
        }

        return await context.PaymentAttempts
            .Where(
                x =>
                    x.TenantId == tenantId &&
                    x.OrderId == orderId &&
                    x.Status ==
                    PaymentAttemptStatus.Pending)
            .OrderByDescending(
                x => x.CreatedAt)
            .FirstOrDefaultAsync(
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