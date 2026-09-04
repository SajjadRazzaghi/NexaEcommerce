using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface ITaxRateRepository
{
    Task<TaxRate?> GetByIdAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TaxRate?> GetByCodeAsync(
        string tenantId,
        string code,
        CancellationToken cancellationToken = default);

    Task<TaxRate?> GetDefaultAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxRate>> GetAllAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TaxRate taxRate,
        CancellationToken cancellationToken = default);

    void Remove(
        TaxRate taxRate);
}
