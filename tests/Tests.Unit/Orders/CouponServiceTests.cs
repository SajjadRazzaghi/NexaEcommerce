using NSubstitute;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class CouponServiceTests
{
    private static (
        CouponService Service,
        ICouponRepository Repository,
        ICouponRedemptionRepository Redemptions,
        IOrderUnitOfWork UnitOfWork)
        CreateService()
    {
        var repository =
            Substitute.For<ICouponRepository>();

        var redemptions =
            Substitute.For<ICouponRedemptionRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var service =
            new CouponService(
                repository,
                redemptions,
                unitOfWork);

        return (
            service,
            repository,
            redemptions,
            unitOfWork);
    }

    [Fact]
    public async Task Validate_returns_discount_for_valid_coupon()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "SAVE10",
                "Ten percent",
                CouponDiscountType.Percentage,
                10);

        var (
            service,
            repository,
            _,
            _) =
            CreateService();

        repository
            .GetByCodeAsync(
                "tenant-1",
                "SAVE10",
                Arg.Any<CancellationToken>())
            .Returns(coupon);

        var result =
            await service.ValidateAsync(
                "tenant-1",
                "user-1",
                "save10",
                500000,
                CancellationToken.None);

        result.IsValid
            .ShouldBeTrue();

        result.Code
            .ShouldBe("SAVE10");

        result.DiscountAmount
            .ShouldBe(50000);

        result.Message
            .ShouldBeNull();

        await repository
            .Received(1)
            .GetByCodeAsync(
                "tenant-1",
                "SAVE10",
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_rejects_unknown_coupon()
    {
        var (
            service,
            repository,
            _,
            _) =
            CreateService();

        repository
            .GetByCodeAsync(
                "tenant-1",
                "UNKNOWN",
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<Coupon?>(
                    null));

        var result =
            await service.ValidateAsync(
                "tenant-1",
                "user-1",
                "UNKNOWN",
                500000,
                CancellationToken.None);

        result.IsValid
            .ShouldBeFalse();

        result.DiscountAmount
            .ShouldBe(0);

        result.Code
            .ShouldBe("UNKNOWN");
    }

    [Fact]
    public async Task Validate_rejects_expired_coupon()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "EXPIRED",
                "Expired",
                CouponDiscountType.FixedAmount,
                50000,
                expiresAt:
                    DateTime.UtcNow.AddMinutes(-1));

        var (
            service,
            repository,
            _,
            _) =
            CreateService();

        repository
            .GetByCodeAsync(
                "tenant-1",
                "EXPIRED",
                Arg.Any<CancellationToken>())
            .Returns(coupon);

        var result =
            await service.ValidateAsync(
                "tenant-1",
                "user-1",
                "EXPIRED",
                500000,
                CancellationToken.None);

        result.IsValid
            .ShouldBeFalse();

        result.DiscountAmount
            .ShouldBe(0);

        result.Message
            .ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Validate_rejects_coupon_below_minimum_order()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "MIN500",
                "Minimum",
                CouponDiscountType.FixedAmount,
                50000,
                minimumOrderAmount:
                    500000);

        var (
            service,
            repository,
            _,
            _) =
            CreateService();

        repository
            .GetByCodeAsync(
                "tenant-1",
                "MIN500",
                Arg.Any<CancellationToken>())
            .Returns(coupon);

        var result =
            await service.ValidateAsync(
                "tenant-1",
                "user-1",
                "MIN500",
                100000,
                CancellationToken.None);

        result.IsValid
            .ShouldBeFalse();

        result.DiscountAmount
            .ShouldBe(0);

        result.Message
            .ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Validate_rejects_coupon_when_usage_limit_is_reached()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "LIMITED",
                "Limited coupon",
                CouponDiscountType.FixedAmount,
                50000,
                usageLimit: 2);

        var (
            service,
            repository,
            redemptions,
            _) =
            CreateService();

        repository
            .GetByCodeAsync(
                "tenant-1",
                "LIMITED",
                Arg.Any<CancellationToken>())
            .Returns(coupon);

        redemptions
            .CountByCouponIdAsync(
                "tenant-1",
                coupon.Id,
                Arg.Any<CancellationToken>())
            .Returns(2);

        var result =
            await service.ValidateAsync(
                "tenant-1",
                "user-1",
                "LIMITED",
                500000,
                CancellationToken.None);

        result.IsValid
            .ShouldBeFalse();

        result.DiscountAmount
            .ShouldBe(0);

        result.Message
            .ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Validate_allows_coupon_when_usage_limit_has_not_been_reached()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "LIMITED",
                "Limited coupon",
                CouponDiscountType.FixedAmount,
                50000,
                usageLimit: 2);

        var (
            service,
            repository,
            redemptions,
            _) =
            CreateService();

        repository
            .GetByCodeAsync(
                "tenant-1",
                "LIMITED",
                Arg.Any<CancellationToken>())
            .Returns(coupon);

        redemptions
            .CountByCouponIdAsync(
                "tenant-1",
                coupon.Id,
                Arg.Any<CancellationToken>())
            .Returns(1);

        var result =
            await service.ValidateAsync(
                "tenant-1",
                "user-1",
                "LIMITED",
                500000,
                CancellationToken.None);

        result.IsValid
            .ShouldBeTrue();

        result.DiscountAmount
            .ShouldBe(50000);
    }

    [Fact]
    public async Task Validate_uses_normalized_coupon_code()
    {
        var coupon =
            Coupon.Create(
                "tenant-1",
                "SAVE20",
                "Twenty percent",
                CouponDiscountType.Percentage,
                20);

        var (
            service,
            repository,
            _,
            _) =
            CreateService();

        repository
            .GetByCodeAsync(
                "tenant-1",
                "SAVE20",
                Arg.Any<CancellationToken>())
            .Returns(coupon);

        var result =
            await service.ValidateAsync(
                "tenant-1",
                "user-1",
                "  save20  ",
                100000,
                CancellationToken.None);

        result.IsValid
            .ShouldBeTrue();

        result.Code
            .ShouldBe("SAVE20");

        result.DiscountAmount
            .ShouldBe(20000);
    }
}
