using NexaEcommerce.Modules.Orders.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class CouponTests
{
    [Fact]
    public void Percentage_coupon_calculates_discount()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "SAVE10",
                "Ten percent off",
                CouponDiscountType.Percentage,
                10);

        var discount =
            coupon.CalculateDiscount(
                500000);

        discount
            .ShouldBe(50000);
    }

    [Fact]
    public void Fixed_coupon_calculates_discount()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "SAVE50",
                "Fixed discount",
                CouponDiscountType.FixedAmount,
                50000);

        coupon.CalculateDiscount(
                300000)
            .ShouldBe(50000);
    }

    [Fact]
    public void Fixed_discount_cannot_exceed_order_amount()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "SAVE999",
                "Large discount",
                CouponDiscountType.FixedAmount,
                999999);

        coupon.CalculateDiscount(
                100000)
            .ShouldBe(100000);
    }

    [Fact]
    public void Maximum_discount_amount_is_respected()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "SAVE20",
                "Twenty percent",
                CouponDiscountType.Percentage,
                20,
                maximumDiscountAmount: 50000);

        coupon.CalculateDiscount(
                500000)
            .ShouldBe(50000);
    }

    [Fact]
    public void Minimum_order_amount_is_enforced()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "MIN500",
                "Minimum order",
                CouponDiscountType.FixedAmount,
                50000,
                minimumOrderAmount: 500000);

        coupon.CalculateDiscount(
                400000)
            .ShouldBe(0);
    }

    [Fact]
    public void Percentage_above_100_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                Coupon.Create(
                    "tenant-1",
                    "INVALID",
                    "Invalid",
                    CouponDiscountType.Percentage,
                    101));
    }

    [Fact]
    public void Code_is_normalized_to_uppercase()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "save10",
                "Ten percent",
                CouponDiscountType.Percentage,
                10);

        coupon.Code
            .ShouldBe("SAVE10");
    }

    [Fact]
    public void Expired_coupon_is_invalid()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "EXPIRED",
                "Expired",
                CouponDiscountType.FixedAmount,
                10000,
                expiresAt:
                    DateTime.UtcNow.AddMinutes(-1));

        coupon.IsCurrentlyValid(
                DateTime.UtcNow)
            .ShouldBeFalse();
    }

    [Fact]
    public void Future_coupon_is_invalid_before_start()
    {
        var startsAt =
            DateTime.UtcNow.AddMinutes(10);

        var coupon =
            Coupon.Create(
                "tenant-1",
                "FUTURE",
                "Future",
                CouponDiscountType.FixedAmount,
                10000,
                startsAt: startsAt);

        coupon.IsCurrentlyValid(
                DateTime.UtcNow)
            .ShouldBeFalse();
    }
}
