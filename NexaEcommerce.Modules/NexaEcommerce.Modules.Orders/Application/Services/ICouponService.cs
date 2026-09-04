using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface ICouponService
{
    Task<IReadOnlyList<CouponDto>> GetAllAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<CouponDto?> GetAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CouponDto> CreateAsync(
        string tenantId,
        CreateCouponRequest request,
        CancellationToken cancellationToken = default);

    Task<CouponDto?> UpdateAsync(
        string tenantId,
        Guid id,
        UpdateCouponRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(
        string tenantId,
        Guid id,
        bool active,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CouponValidationResultDto> ValidateAsync(
        string tenantId,
        string userId,
        string code,
        decimal orderAmount,
        CancellationToken cancellationToken = default);

    Task<CouponRedemptionResultDto> RedeemAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string code,
        decimal orderAmount,
        CancellationToken cancellationToken = default);
}
