using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class OrderRepository(
    OrdersDbContext context)
    : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(
        string tenantId,
        Guid id,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query =
            context.Orders
                .Include(x => x.Items)
                .Include(x => x.InventoryReservations)
                .Where(
                    x =>
                        x.Id == id &&
                        x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query =
                query.Where(
                    x =>
                        x.UserId == userId);
        }

        return await query.FirstOrDefaultAsync(
            cancellationToken);
    }

    public async Task<Order?> GetByOrderNumberAsync(
        string tenantId,
        string orderNumber,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query =
            context.Orders
                .Include(x => x.Items)
                .Include(x => x.InventoryReservations)
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.OrderNumber == orderNumber);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query =
                query.Where(
                    x =>
                        x.UserId == userId);
        }

        return await query.FirstOrDefaultAsync(
            cancellationToken);
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(
        string tenantId,
        string userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .Include(x => x.Items)
            .Include(x => x.InventoryReservations)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId &&
                    x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await context.Orders.AddAsync(
            order,
            cancellationToken);
    }
}