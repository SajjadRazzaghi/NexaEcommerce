using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class TaxRateRepository(
    OrdersDbContext context)
    : ITaxRateRepository
{
    public async Task<TaxRate?> GetByIdAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.TaxRates
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == id,
                cancellationToken);
    }

    public async Task<TaxRate?> GetByCodeAsync(
        string tenantId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalized =
            code.Trim()
                .ToUpperInvariant();

        return await context.TaxRates
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Code == normalized,
                cancellationToken);
    }

    public async Task<TaxRate?> GetDefaultAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.TaxRates
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.IsActive &&
                    x.IsDefault,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TaxRate>> GetAllAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.TaxRates
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId)
            .OrderByDescending(
                x => x.IsDefault)
            .ThenBy(
                x => x.Code)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        TaxRate taxRate,
        CancellationToken cancellationToken = default)
    {
        await context.TaxRates.AddAsync(
            taxRate,
            cancellationToken);
    }

    public void Remove(
        TaxRate taxRate)
    {
        context.TaxRates.Remove(
            taxRate);
    }
}
