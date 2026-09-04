using NexaEcommerce.Modules.Orders.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class CouponRedemptionTests
{
    [Fact]
    public void Redemption_requires_positive_discount()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                CouponRedemption.Create(
                    "tenant-1",
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "user-1",
                    "SAVE10",
                    0));
    }

    [Fact]
    public void Redemption_normalizes_coupon_code()
    {
        var redemption =
            CouponRedemption.Create(
                "tenant-1",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "user-1",
                "save10",
                50000);

        redemption.CouponCode
            .ShouldBe("SAVE10");
    }
}
