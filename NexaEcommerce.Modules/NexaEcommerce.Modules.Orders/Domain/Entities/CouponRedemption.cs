using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class CouponRedemption : BaseEntity
{
    private CouponRedemption()
    {
    }

    private CouponRedemption(
        string tenantId,
        Guid couponId,
        Guid orderId,
        string userId,
        string couponCode,
        decimal discountAmount)
    {
        TenantId =
            tenantId.Trim();

        CouponId =
            couponId;

        OrderId =
            orderId;

        UserId =
            userId.Trim();

        CouponCode =
            couponCode.Trim().ToUpperInvariant();

        DiscountAmount =
            discountAmount;

        CreatedAt =
            DateTime.UtcNow;
    }

    public string TenantId { get; private set; } = null!;

    public Guid CouponId { get; private set; }

    public Guid OrderId { get; private set; }

    public string UserId { get; private set; } = null!;

    public string CouponCode { get; private set; } = null!;

    public decimal DiscountAmount { get; private set; }

    public static CouponRedemption Create(
        string tenantId,
        Guid couponId,
        Guid orderId,
        string userId,
        string couponCode,
        decimal discountAmount)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (couponId == Guid.Empty)
        {
            throw new ArgumentException(
                "Coupon id is required.",
                nameof(couponId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(couponCode))
        {
            throw new ArgumentException(
                "Coupon code is required.",
                nameof(couponCode));
        }

        if (discountAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountAmount));
        }

        return new CouponRedemption(
            tenantId,
            couponId,
            orderId,
            userId,
            couponCode,
            discountAmount);
    }
}
