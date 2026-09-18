using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class FulfillmentRepository(
    OrdersDbContext context)
    : IFulfillmentRepository
{
    public async Task<Fulfillment?> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await context.Fulfillments
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.OrderId == orderId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Fulfillment>>
        GetByStatusesAsync(
            string tenantId,
            IReadOnlyCollection<FulfillmentStatus> statuses,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        return await context.Fulfillments
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId &&
                    statuses.Contains(x.Status))
            .OrderBy(
                x => x.CreatedAt)
            .ThenBy(
                x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        Fulfillment fulfillment,
        CancellationToken cancellationToken = default)
    {
        await context.Fulfillments.AddAsync(
            fulfillment,
            cancellationToken);
    }
}
