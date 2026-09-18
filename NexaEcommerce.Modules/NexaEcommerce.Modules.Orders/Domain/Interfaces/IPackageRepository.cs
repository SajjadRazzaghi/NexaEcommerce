using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface IPackageRepository
{
    Task<Package?> GetByIdAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Package>> GetByOrderIdAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<int> GetNextPackageNumberAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Package package,
        CancellationToken cancellationToken = default);
}
