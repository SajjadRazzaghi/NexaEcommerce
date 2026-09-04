namespace NexaEcommerce.Modules.Orders.Application.Pricing;

public interface IPricingCalculator
{
    PricingResult Calculate(
        PricingInput input);
}