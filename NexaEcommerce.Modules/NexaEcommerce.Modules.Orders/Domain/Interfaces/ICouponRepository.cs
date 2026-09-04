using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface ICouponRepository
{
    Task<Coupon?> GetByIdAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Coupon?> GetByCodeAsync(
        string tenantId,
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Coupon>> GetAllAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Coupon coupon,
        CancellationToken cancellationToken = default);

    void Remove(
        Coupon coupon);
}
