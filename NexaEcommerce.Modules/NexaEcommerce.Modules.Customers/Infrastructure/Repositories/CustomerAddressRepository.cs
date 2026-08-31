using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Customers.Domain.Entities;
using NexaEcommerce.Modules.Customers.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Customers.Infrastructure.Repositories;

public sealed class CustomerAddressRepository(
    CustomerDbContext context)
    : ICustomerAddressRepository
{
    public async Task<IReadOnlyList<CustomerAddress>>
        GetForUserAsync(
            string tenantId,
            string userId,
            CancellationToken cancellationToken = default)
    {
        return await context.CustomerAddresses
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId)
            .OrderByDescending(
                x => x.IsDefault)
            .ThenBy(
                x => x.Title)
            .ToListAsync(
                cancellationToken);
    }

    public Task<CustomerAddress?> GetByIdAsync(
        string tenantId,
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return context.CustomerAddresses
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId &&
                    x.Id == id,
                cancellationToken);
    }

    public Task<CustomerAddress?> GetDefaultAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return context.CustomerAddresses
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId &&
                    x.IsDefault,
                cancellationToken);
    }

    public async Task AddAsync(
        CustomerAddress address,
        CancellationToken cancellationToken = default)
    {
        await context.CustomerAddresses
            .AddAsync(
                address,
                cancellationToken);
    }

    public void Remove(
        CustomerAddress address)
    {
        context.CustomerAddresses.Remove(
            address);
    }
}

