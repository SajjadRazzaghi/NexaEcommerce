using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class PackageRepository(
    OrdersDbContext context)
    : IPackageRepository
{
    public async Task<Package?> GetByIdAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        return await context.Packages
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == packageId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Package>> GetByOrderIdAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await context.Packages
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId &&
                    x.OrderId == orderId)
            .OrderBy(x => x.PackageNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextPackageNumberAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var maxNumber =
            await context.Packages
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.OrderId == orderId)
                .Select(
                    x => (int?)x.PackageNumber)
                .MaxAsync(cancellationToken);

        return (maxNumber ?? 0) + 1;
    }

    public async Task AddAsync(
        Package package,
        CancellationToken cancellationToken = default)
    {
        await context.Packages.AddAsync(
            package,
            cancellationToken);
    }
}
