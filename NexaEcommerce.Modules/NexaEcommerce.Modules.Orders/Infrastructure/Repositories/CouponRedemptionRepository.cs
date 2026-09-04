using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class CouponRedemptionRepository(
    OrdersDbContext context)
    : ICouponRedemptionRepository
{
    public async Task<CouponRedemption?> GetByOrderIdAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await context.CouponRedemptions
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.OrderId == orderId,
                cancellationToken);
    }

    public async Task<int> CountByCouponIdAsync(
        string tenantId,
        Guid couponId,
        CancellationToken cancellationToken = default)
    {
        return await context.CouponRedemptions
            .CountAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.CouponId == couponId,
                cancellationToken);
    }

    public async Task<int> CountByCouponAndUserAsync(
        string tenantId,
        Guid couponId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.CouponRedemptions
            .CountAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.CouponId == couponId &&
                    x.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        CouponRedemption redemption,
        CancellationToken cancellationToken = default)
    {
        await context.CouponRedemptions.AddAsync(
            redemption,
            cancellationToken);
    }
}
