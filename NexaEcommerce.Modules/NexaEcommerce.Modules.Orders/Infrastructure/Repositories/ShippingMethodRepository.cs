using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class ShippingMethodRepository(
    OrdersDbContext context)
    : IShippingMethodRepository
{
    public async Task<ShippingMethod?> GetByIdAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.ShippingMethods
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<ShippingMethod?> GetByCodeAsync(
        string tenantId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode =
            code.Trim();

        return await context.ShippingMethods
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Code == normalizedCode,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingMethod>>
        GetActiveAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
    {
        return await context.ShippingMethods
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId &&
                    x.IsActive)
            .OrderBy(
                x => x.SortOrder)
            .ThenBy(
                x => x.Name)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingMethod>>
        GetAllAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
    {
        return await context.ShippingMethods
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId)
            .OrderBy(
                x => x.SortOrder)
            .ThenBy(
                x => x.Name)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        ShippingMethod shippingMethod,
        CancellationToken cancellationToken = default)
    {
        await context.ShippingMethods.AddAsync(
            shippingMethod,
            cancellationToken);
    }

    public void Remove(
        ShippingMethod shippingMethod)
    {
        context.ShippingMethods.Remove(
            shippingMethod);
    }
}
