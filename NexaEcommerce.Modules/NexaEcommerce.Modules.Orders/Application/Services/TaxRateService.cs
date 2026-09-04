using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class TaxRateService(
    ITaxRateRepository repository,
    IOrderUnitOfWork unitOfWork)
    : ITaxRateService
{
    public async Task<IReadOnlyList<TaxRateDto>> GetAllAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var rates =
            await repository.GetAllAsync(
                tenantId,
                cancellationToken);

        return rates
            .Select(Map)
            .ToList();
    }

    public async Task<TaxRateDto?> GetAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var rate =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        return rate is null
            ? null
            : Map(rate);
    }

    public async Task<TaxRateDto> CreateAsync(
        string tenantId,
        CreateTaxRateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var code =
            NormalizeCode(
                request.Code);

        var existing =
            await repository.GetByCodeAsync(
                tenantId,
                code,
                cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Tax rate code '{code}' already exists.");
        }

        if (request.IsDefault)
        {
            var currentDefault =
                await repository.GetDefaultAsync(
                    tenantId,
                    cancellationToken);

            currentDefault?.SetDefault(
                false);
        }

        var rate =
            TaxRate.Create(
                tenantId,
                code,
                request.Name,
                request.RatePercent,
                request.IsDefault);

        await repository.AddAsync(
            rate,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(rate);
    }

    public async Task<TaxRateDto?> UpdateAsync(
        string tenantId,
        Guid id,
        UpdateTaxRateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var rate =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (rate is null)
            return null;

        rate.Update(
            request.Name,
            request.RatePercent);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(rate);
    }

    public async Task<bool> SetActiveAsync(
        string tenantId,
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var rate =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (rate is null)
            return false;

        if (active)
            rate.Activate();
        else
            rate.Deactivate();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<bool> SetDefaultAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var rate =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (rate is null)
            return false;

        if (!rate.IsActive)
        {
            throw new InvalidOperationException(
                "An inactive tax rate cannot be default.");
        }

        var currentDefault =
            await repository.GetDefaultAsync(
                tenantId,
                cancellationToken);

        if (currentDefault?.Id != rate.Id)
        {
            currentDefault?.SetDefault(
                false);

            rate.SetDefault(
                true);
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var rate =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (rate is null)
            return false;

        repository.Remove(
            rate);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<TaxCalculationDto?> CalculateDefaultAsync(
        string tenantId,
        decimal taxableAmount,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (taxableAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxableAmount));
        }

        var rate =
            await repository.GetDefaultAsync(
                tenantId,
                cancellationToken);

        if (rate is null)
            return null;

        return new TaxCalculationDto(
            rate.Id,
            rate.Code,
            rate.RatePercent,
            taxableAmount,
            rate.Calculate(
                taxableAmount));
    }

    private static string NormalizeCode(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Tax rate code is required.",
                nameof(value));
        }

        return value
            .Trim()
            .ToUpperInvariant();
    }

    private static TaxRateDto Map(
        TaxRate rate)
    {
        return new TaxRateDto(
            rate.Id,
            rate.Code,
            rate.Name,
            rate.RatePercent,
            rate.IsDefault,
            rate.IsActive);
    }

    private static void ValidateTenant(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(
                tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }
}
