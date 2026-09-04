using NexaEcommerce.Modules.Orders.Domain.Entities;
using Xunit;

namespace NexaEcommerce.Tests.Unit.Features.Orders;

public sealed class OrderPricingTests
{
    [Fact]
    public void ApplyPricing_ShouldReplaceCalculatedTotals()
    {
        var order =
            Order.Create(
                "tenant-1",
                "user-1",
                "NX-1000",
                "idem-1000",
                "IRR",
                0m,
                100m,
                0m,
                "Test User",
                "09120000000",
                "Test Address",
                "Tehran",
                "1234567890");

        order.ApplyPricing(
            1000m,
            100m,
            200m,
            900m);

        Assert.Equal(1000m, order.Subtotal);
        Assert.Equal(100m, order.ShippingAmount);
        Assert.Equal(200m, order.DiscountAmount);
        Assert.Equal(900m, order.TotalAmount);
    }

    [Fact]
    public void ApplyPricing_ShouldRejectNegativeSubtotal()
    {
        var order =
            CreateOrder();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                order.ApplyPricing(
                    -1m,
                    100m,
                    0m,
                    99m));
    }

    [Fact]
    public void ApplyPricing_ShouldRejectNegativeShipping()
    {
        var order =
            CreateOrder();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                order.ApplyPricing(
                    100m,
                    -1m,
                    0m,
                    99m));
    }

    [Fact]
    public void ApplyPricing_ShouldRejectNegativeDiscount()
    {
        var order =
            CreateOrder();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                order.ApplyPricing(
                    100m,
                    0m,
                    -1m,
                    99m));
    }

    [Fact]
    public void ApplyPricing_ShouldRejectNegativeTotal()
    {
        var order =
            CreateOrder();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                order.ApplyPricing(
                    100m,
                    0m,
                    0m,
                    -1m));
    }

    private static Order CreateOrder()
    {
        return Order.Create(
            "tenant-1",
            "user-1",
            "NX-1000",
            "idem-1000",
            "IRR",
            0m,
            0m,
            0m,
            "Test User",
            "09120000000",
            "Test Address",
            "Tehran",
            "1234567890");
    }
}