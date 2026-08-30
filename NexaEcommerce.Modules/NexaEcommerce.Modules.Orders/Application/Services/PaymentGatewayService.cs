using NexaEcommerce.Modules.Orders.Application.Payments;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class PaymentGatewayService(
    IEnumerable<IPaymentGateway> gateways)
{
    public IPaymentGateway Get(
        string gatewayName)
    {
        if (string.IsNullOrWhiteSpace(gatewayName))
        {
            throw new ArgumentException(
                "Gateway name is required.",
                nameof(gatewayName));
        }

        var gateway =
            gateways.FirstOrDefault(
                x =>
                    string.Equals(
                        x.Name,
                        gatewayName.Trim(),
                        StringComparison.OrdinalIgnoreCase));

        return gateway
            ?? throw new InvalidOperationException(
                $"Payment gateway '{gatewayName}' is not configured.");
    }
}