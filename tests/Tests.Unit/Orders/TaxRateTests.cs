using NexaEcommerce.Modules.Orders.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class TaxRateTests
{
    [Fact]
    public void Tax_rate_calculates_percentage()
    {
        var tax =
            TaxRate.Create(
                "tenant-1",
                "VAT",
                "Value Added Tax",
                10);

        tax.Calculate(
                500000)
            .ShouldBe(50000);
    }

    [Fact]
    public void Tax_rate_rounds_to_two_decimal_places()
    {
        var tax =
            TaxRate.Create(
                "tenant-1",
                "VAT",
                "VAT",
                9.5m);

        tax.Calculate(
                101)
            .ShouldBe(
                9.6m);
    }

    [Fact]
    public void Rate_above_100_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                TaxRate.Create(
                    "tenant-1",
                    "BAD",
                    "Bad",
                    100.01m));
    }

    [Fact]
    public void Inactive_tax_rate_returns_zero()
    {
        var tax =
            TaxRate.Create(
                "tenant-1",
                "VAT",
                "VAT",
                10);

        tax.Deactivate();

        tax.Calculate(
                500000)
            .ShouldBe(0);
    }
}
