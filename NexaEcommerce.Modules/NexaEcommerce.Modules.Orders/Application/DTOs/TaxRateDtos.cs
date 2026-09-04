namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record TaxRateDto(
    Guid Id,
    string Code,
    string Name,
    decimal RatePercent,
    bool IsDefault,
    bool IsActive);

public sealed record CreateTaxRateRequest(
    string Code,
    string Name,
    decimal RatePercent,
    bool IsDefault = false);

public sealed record UpdateTaxRateRequest(
    string Name,
    decimal RatePercent);

public sealed record TaxCalculationDto(
    Guid TaxRateId,
    string Code,
    decimal RatePercent,
    decimal TaxableAmount,
    decimal TaxAmount);
