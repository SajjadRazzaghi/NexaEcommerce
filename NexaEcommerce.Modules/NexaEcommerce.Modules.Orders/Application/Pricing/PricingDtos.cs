namespace NexaEcommerce.Modules.Orders.Application.Pricing;

public sealed record PricingInput(
    decimal Subtotal,
    decimal ShippingAmount,
    decimal DiscountAmount,
    decimal TaxRatePercent);

public sealed record PricingResult(
    decimal Subtotal,
    decimal ShippingAmount,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal TotalAmount);