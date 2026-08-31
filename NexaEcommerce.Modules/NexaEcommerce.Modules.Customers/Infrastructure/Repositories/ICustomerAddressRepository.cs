using NexaEcommerce.Modules.Customers.Domain.Entities;

namespace NexaEcommerce.Modules.Customers.Infrastructure.Repositories;

public interface ICustomerAddressRepository
{
    Task<IReadOnlyList<CustomerAddress>> GetForUserAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetByIdAsync(
        string tenantId,
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetDefaultAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CustomerAddress address,
        CancellationToken cancellationToken = default);

    void Remove(
        CustomerAddress address);
}
