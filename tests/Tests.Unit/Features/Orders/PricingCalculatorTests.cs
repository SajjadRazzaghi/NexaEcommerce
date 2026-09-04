using NexaEcommerce.Modules.Orders.Application.Pricing;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Features.Orders;

public sealed class PricingCalculatorTests
{
    private readonly IPricingCalculator _sut =
        new PricingCalculator();

    [Fact]
    public void Calculates_total_without_discount_and_tax()
    {
        var result =
            _sut.Calculate(
                new PricingInput(
                    Subtotal: 1_000m,
                    ShippingAmount: 100m,
                    DiscountAmount: 0m,
                    TaxRatePercent: 0m));

        result.Subtotal
            .ShouldBe(1_000m);

        result.ShippingAmount
            .ShouldBe(100m);

        result.DiscountAmount
            .ShouldBe(0m);

        result.TaxableAmount
            .ShouldBe(1_000m);

        result.TaxAmount
            .ShouldBe(0m);

        result.TotalAmount
            .ShouldBe(1_100m);
    }

    [Fact]
    public void Calculates_discount_before_tax()
    {
        var result =
            _sut.Calculate(
                new PricingInput(
                    Subtotal: 1_000m,
                    ShippingAmount: 100m,
                    DiscountAmount: 200m,
                    TaxRatePercent: 10m));

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
    public void Discount_cannot_exceed_subtotal()
    {
        var result =
            _sut.Calculate(
                new PricingInput(
                    Subtotal: 500m,
                    ShippingAmount: 100m,
                    DiscountAmount: 900m,
                    TaxRatePercent: 10m));

        result.DiscountAmount
            .ShouldBe(500m);

        result.TaxableAmount
            .ShouldBe(0m);

        result.TaxAmount
            .ShouldBe(0m);

        result.TotalAmount
            .ShouldBe(100m);
    }

    [Fact]
    public void Zero_subtotal_is_supported()
    {
        var result =
            _sut.Calculate(
                new PricingInput(
                    Subtotal: 0m,
                    ShippingAmount: 100m,
                    DiscountAmount: 0m,
                    TaxRatePercent: 10m));

        result.TaxableAmount
            .ShouldBe(0m);

        result.TaxAmount
            .ShouldBe(0m);

        result.TotalAmount
            .ShouldBe(100m);
    }

    [Fact]
    public void Tax_rate_of_one_hundred_percent_is_supported()
    {
        var result =
            _sut.Calculate(
                new PricingInput(
                    Subtotal: 1_000m,
                    ShippingAmount: 0m,
                    DiscountAmount: 0m,
                    TaxRatePercent: 100m));

        result.TaxAmount
            .ShouldBe(1_000m);

        result.TotalAmount
            .ShouldBe(2_000m);
    }

    [Fact]
    public void Money_is_rounded_to_two_decimal_places()
    {
        var result =
            _sut.Calculate(
                new PricingInput(
                    Subtotal: 100.01m,
                    ShippingAmount: 10.02m,
                    DiscountAmount: 3.33m,
                    TaxRatePercent: 9.5m));

        result.TaxableAmount
            .ShouldBe(96.68m);

        result.TaxAmount
            .ShouldBe(9.18m);

        result.TotalAmount
            .ShouldBe(115.88m);
    }

    [Fact]
    public void Negative_subtotal_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                _sut.Calculate(
                    new PricingInput(
                        Subtotal: -1m,
                        ShippingAmount: 0m,
                        DiscountAmount: 0m,
                        TaxRatePercent: 0m)));
    }

    [Fact]
    public void Negative_shipping_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                _sut.Calculate(
                    new PricingInput(
                        Subtotal: 100m,
                        ShippingAmount: -1m,
                        DiscountAmount: 0m,
                        TaxRatePercent: 0m)));
    }

    [Fact]
    public void Negative_discount_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                _sut.Calculate(
                    new PricingInput(
                        Subtotal: 100m,
                        ShippingAmount: 0m,
                        DiscountAmount: -1m,
                        TaxRatePercent: 0m)));
    }

    [Fact]
    public void Tax_rate_above_one_hundred_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                _sut.Calculate(
                    new PricingInput(
                        Subtotal: 100m,
                        ShippingAmount: 0m,
                        DiscountAmount: 0m,
                        TaxRatePercent: 100.01m)));
    }

    [Fact]
    public void Negative_tax_rate_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                _sut.Calculate(
                    new PricingInput(
                        Subtotal: 100m,
                        ShippingAmount: 0m,
                        DiscountAmount: 0m,
                        TaxRatePercent: -0.01m)));
    }

    [Fact]
    public void Discount_is_applied_only_to_subtotal()
    {
        var result =
            _sut.Calculate(
                new PricingInput(
                    Subtotal: 1_000m,
                    ShippingAmount: 500m,
                    DiscountAmount: 100m,
                    TaxRatePercent: 10m));

        result.TaxableAmount
            .ShouldBe(900m);

        result.TaxAmount
            .ShouldBe(90m);

        result.TotalAmount
            .ShouldBe(1_490m);
    }
}