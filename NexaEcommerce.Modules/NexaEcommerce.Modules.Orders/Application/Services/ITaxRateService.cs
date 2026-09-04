using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface ITaxRateService
{
    Task<IReadOnlyList<TaxRateDto>> GetAllAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<TaxRateDto?> GetAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TaxRateDto> CreateAsync(
        string tenantId,
        CreateTaxRateRequest request,
        CancellationToken cancellationToken = default);

    Task<TaxRateDto?> UpdateAsync(
        string tenantId,
        Guid id,
        UpdateTaxRateRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(
        string tenantId,
        Guid id,
        bool active,
        CancellationToken cancellationToken = default);

    Task<bool> SetDefaultAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TaxCalculationDto?> CalculateDefaultAsync(
        string tenantId,
        decimal taxableAmount,
        CancellationToken cancellationToken = default);
}
