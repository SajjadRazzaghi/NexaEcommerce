using NexaEcommerce.Modules.Orders.Application.Pricing;
using Shouldly;

namespace NexaEcommerce.Tests.Unit.Features.Orders;

public sealed class OrderFinancialIntegrityTests
{
    [Fact]
    public void Pricing_calculator_should_apply_discount_before_tax()
    {
        var calculator =
            new PricingCalculator();

        var result =
            calculator.Calculate(
                new PricingInput(
                    Subtotal: 1_000m,
                    ShippingAmount: 100m,
                    DiscountAmount: 200m,
                    TaxRatePercent: 10m));

        result.Subtotal
            .ShouldBe(1_000m);

        result.ShippingAmount
            .ShouldBe(100m);

        result.DiscountAmount
            .ShouldBe(200m);

        result.TaxableAmount
            .ShouldBe(800m);

        result.TaxAmount
            .ShouldBe(80m);

        result.TotalAmount
            .ShouldBe(980m);
    }

    [Fact]
    public void Pricing_calculator_should_never_allow_discount_above_subtotal()
    {
        var calculator =
            new PricingCalculator();

        var result =
            calculator.Calculate(
                new PricingInput(
                    Subtotal: 500m,
                    ShippingAmount: 50m,
                    DiscountAmount: 900m,
                    TaxRatePercent: 10m));

        result.DiscountAmount
            .ShouldBe(500m);

        result.TaxableAmount
            .ShouldBe(0m);

        result.TaxAmount
            .ShouldBe(0m);

        result.TotalAmount
            .ShouldBe(50m);
    }

    [Fact]
    public void Order_total_should_include_shipping_after_items_are_added()
    {
        var order =
            NexaEcommerce.Modules.Orders.Domain.Entities.Order.Create(
                "tenant-1",
                "user-1",
                "NX-TEST-001",
                "idem-financial-001",
                "IRR",
                0m,
                100m,
                0m,
                "Test User",
                "09120000000",
                "Test Address",
                "Tehran",
                "1234567890");

        order.AddItem(
            Guid.NewGuid(),
            "SKU-001",
            "Test Product",
            500m,
            2);

        order.TotalAmount
            .ShouldBe(1_100m);
    }

    [Fact]
    public void Order_recalculate_should_preserve_tax_in_total()
    {
        var order =
            NexaEcommerce.Modules.Orders.Domain.Entities.Order.Create(
                "tenant-1",
                "user-1",
                "NX-TEST-002",
                "idem-financial-002",
                "IRR",
                0m,
                100m,
                50m,
                "Test User",
                "09120000000",
                "Test Address",
                "Tehran",
                "1234567890");

        order.AddItem(
            Guid.NewGuid(),
            "SKU-002",
            "Test Product",
            1_000m,
            1);

        order.ApplyPricing(
            subtotal: 1_000m,
            shippingAmount: 100m,
            discountAmount: 50m,
            taxableAmount: 950m,
            taxRatePercent: 10m,
            taxAmount: 95m,
            totalAmount: 1_145m,
            couponCode: "TEST10");

        order.Subtotal
            .ShouldBe(1_000m);

        order.TaxableAmount
            .ShouldBe(950m);

        order.TaxRatePercent
            .ShouldBe(10m);

        order.TaxAmount
            .ShouldBe(95m);

        order.TotalAmount
            .ShouldBe(1_145m);

        order.CouponCode
            .ShouldBe("TEST10");
    }
}