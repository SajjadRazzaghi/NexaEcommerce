using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class CouponService(
    ICouponRepository repository,
    ICouponRedemptionRepository redemptions,
    IOrderUnitOfWork unitOfWork)
    : ICouponService
{
    public async Task<IReadOnlyList<CouponDto>> GetAllAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var coupons =
            await repository.GetAllAsync(
                tenantId,
                cancellationToken);

        return coupons
            .Select(Map)
            .ToList();
    }

    public async Task<CouponDto?> GetAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (id == Guid.Empty)
            return null;

        var coupon =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        return coupon is null
            ? null
            : Map(coupon);
    }

    public async Task<CouponDto> CreateAsync(
        string tenantId,
        CreateCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var code =
            NormalizeCode(
                request.Code);

        var existing =
            await repository.GetByCodeAsync(
                tenantId,
                code,
                cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Coupon code '{code}' already exists.");
        }

        var coupon =
            Coupon.Create(
                tenantId,
                code,
                request.Name,
                request.DiscountType,
                request.DiscountValue,
                request.MinimumOrderAmount,
                request.MaximumDiscountAmount,
                request.StartsAt,
                request.ExpiresAt,
                request.UsageLimit);

        await repository.AddAsync(
            coupon,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(coupon);
    }

    public async Task<CouponDto?> UpdateAsync(
        string tenantId,
        Guid id,
        UpdateCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var coupon =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (coupon is null)
            return null;

        coupon.Update(
            request.Name,
            request.DiscountType,
            request.DiscountValue,
            request.MinimumOrderAmount,
            request.MaximumDiscountAmount,
            request.StartsAt,
            request.ExpiresAt,
            request.UsageLimit);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(coupon);
    }

    public async Task<bool> SetActiveAsync(
        string tenantId,
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var coupon =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (coupon is null)
            return false;

        if (active)
            coupon.Activate();
        else
            coupon.Deactivate();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var coupon =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (coupon is null)
            return false;

        repository.Remove(
            coupon);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<CouponValidationResultDto> ValidateAsync(
        string tenantId,
        string userId,
        string code,
        decimal orderAmount,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        ValidateUser(
            userId);

        if (orderAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderAmount));
        }

        var normalizedCode =
            NormalizeCode(
                code);

        var coupon =
            await repository.GetByCodeAsync(
                tenantId,
                normalizedCode,
                cancellationToken);

        if (coupon is null)
        {
            return Invalid(
                normalizedCode,
                "Coupon was not found.");
        }

        if (!coupon.IsCurrentlyValid(
                DateTime.UtcNow))
        {
            return Invalid(
                normalizedCode,
                "Coupon is not active or has expired.");
        }

        var discount =
            coupon.CalculateDiscount(
                orderAmount);

        if (discount <= 0)
        {
            return Invalid(
                normalizedCode,
                "Coupon is not applicable to this order.");
        }

        if (coupon.UsageLimit.HasValue)
        {
            var usageCount =
                await redemptions.CountByCouponIdAsync(
                    tenantId,
                    coupon.Id,
                    cancellationToken);

            if (usageCount >=
                coupon.UsageLimit.Value)
            {
                return Invalid(
                    normalizedCode,
                    "Coupon usage limit has been reached.");
            }
        }

        return new CouponValidationResultDto(
            normalizedCode,
            true,
            discount,
            null);
    }

    public async Task<CouponRedemptionResultDto> RedeemAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string code,
        decimal orderAmount,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        ValidateUser(
            userId);

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        var normalizedCode =
            NormalizeCode(
                code);

        var existing =
            await redemptions.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (existing is not null)
        {
            return new CouponRedemptionResultDto(
                existing.CouponCode,
                true,
                existing.DiscountAmount,
                true,
                null);
        }

        var coupon =
            await repository.GetByCodeAsync(
                tenantId,
                normalizedCode,
                cancellationToken);

        if (coupon is null)
        {
            throw new InvalidOperationException(
                "Coupon was not found.");
        }

        if (!coupon.IsCurrentlyValid(
                DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                "Coupon is no longer valid.");
        }

        if (coupon.UsageLimit.HasValue)
        {
            var totalUsage =
                await redemptions.CountByCouponIdAsync(
                    tenantId,
                    coupon.Id,
                    cancellationToken);

            if (totalUsage >=
                coupon.UsageLimit.Value)
            {
                throw new InvalidOperationException(
                    "Coupon usage limit has been reached.");
            }
        }

        var discount =
            coupon.CalculateDiscount(
                orderAmount);

        if (discount <= 0)
        {
            throw new InvalidOperationException(
                "Coupon is not applicable to this order.");
        }

        var redemption =
            CouponRedemption.Create(
                tenantId,
                coupon.Id,
                orderId,
                userId,
                coupon.Code,
                discount);

        await redemptions.AddAsync(
            redemption,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new CouponRedemptionResultDto(
            coupon.Code,
            true,
            discount,
            false,
            null);
    }

    private static CouponValidationResultDto Invalid(
        string code,
        string message)
    {
        return new CouponValidationResultDto(
            code,
            false,
            0,
            message);
    }

    private static CouponDto Map(
        Coupon coupon)
    {
        return new CouponDto(
            coupon.Id,
            coupon.Code,
            coupon.Name,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.MinimumOrderAmount,
            coupon.MaximumDiscountAmount,
            coupon.StartsAt,
            coupon.ExpiresAt,
            coupon.UsageLimit,
            coupon.IsActive);
    }

    private static string NormalizeCode(
        string code)
    {
        if (string.IsNullOrWhiteSpace(
                code))
        {
            throw new ArgumentException(
                "Coupon code is required.",
                nameof(code));
        }

        var normalized =
            code.Trim()
                .ToUpperInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException(
                "Coupon code cannot exceed 64 characters.",
                nameof(code));
        }

        return normalized;
    }

    private static void ValidateTenant(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(
                tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }

    private static void ValidateUser(
        string userId)
    {
        if (string.IsNullOrWhiteSpace(
                userId))
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }
    }
}
