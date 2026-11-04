
namespace NexaEcommerce.SharedKernel.Domain.ValueObjects;

public static class IranianCurrency
{
    public const string Toman = "IRT";
    public const string Rial = "IRR";

    public static long ToRial(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        var multiplier = currency.Trim().ToUpperInvariant() switch
        {
            Toman => 10m,
            Rial => 1m,
            _ => throw new NotSupportedException(
                $"Currency '{currency}' is not supported for Iranian bank payments.")
        };

        var rialAmount = decimal.Round(
            checked(amount * multiplier),
            0,
            MidpointRounding.AwayFromZero);

        return checked((long)rialAmount);
    }
}
