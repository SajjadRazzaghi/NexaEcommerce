namespace NexaEcommerce.Modules.Orders.Application.Pricing;

public sealed class PricingCalculator : IPricingCalculator
{
    private const decimal MaxTaxRatePercent = 100m;

    public PricingResult Calculate(
        PricingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Validate(input);

        var subtotal =
            RoundMoney(input.Subtotal);

        var shipping =
            RoundMoney(input.ShippingAmount);

        var discount =
            RoundMoney(
                Math.Min(
                    input.DiscountAmount,
                    subtotal));

        var taxableAmount =
            RoundMoney(
                Math.Max(
                    0m,
                    subtotal - discount));

        var taxAmount =
            RoundMoney(
                taxableAmount *
                input.TaxRatePercent /
                100m);

        var totalAmount =
            RoundMoney(
                subtotal +
                shipping -
                discount +
                taxAmount);

        return new PricingResult(
            subtotal,
            shipping,
            discount,
            taxableAmount,
            input.TaxRatePercent,
            taxAmount,
            totalAmount);
    }

    private static void Validate(
        PricingInput input)
    {
        if (input.Subtotal < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.Subtotal));
        }

        if (input.ShippingAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.ShippingAmount));
        }

        if (input.DiscountAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.DiscountAmount));
        }

        if (input.TaxRatePercent < 0m ||
            input.TaxRatePercent > MaxTaxRatePercent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.TaxRatePercent));
        }
    }

    private static decimal RoundMoney(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }
}