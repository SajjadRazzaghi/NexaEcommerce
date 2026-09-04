using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface ICouponRedemptionRepository
{
    Task<CouponRedemption?> GetByOrderIdAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<int> CountByCouponIdAsync(
        string tenantId,
        Guid couponId,
        CancellationToken cancellationToken = default);

    Task<int> CountByCouponAndUserAsync(
        string tenantId,
        Guid couponId,
        string userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CouponRedemption redemption,
        CancellationToken cancellationToken = default);
}
