using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class CouponRepository(
    OrdersDbContext context)
    : ICouponRepository
{
    public async Task<Coupon?> GetByIdAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.Coupons
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<Coupon?> GetByCodeAsync(
        string tenantId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode =
            code.Trim()
                .ToUpperInvariant();

        return await context.Coupons
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Code == normalizedCode,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Coupon>> GetAllAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.Coupons
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId)
            .OrderBy(
                x => x.Code)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        Coupon coupon,
        CancellationToken cancellationToken = default)
    {
        await context.Coupons.AddAsync(
            coupon,
            cancellationToken);
    }

    public void Remove(
        Coupon coupon)
    {
        context.Coupons.Remove(
            coupon);
    }
}
