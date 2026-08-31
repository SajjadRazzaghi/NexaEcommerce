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
        IQueryable<Order> query =
            context.Orders
                .Include(x => x.Items)
                .Include(x => x.InventoryReservations)
                .Where(
                    x =>
                        x.Id == id &&
                        x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(
                x => x.UserId == userId);
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
        IQueryable<Order> query =
            context.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.InventoryReservations)
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.OrderNumber == orderNumber);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(
                x => x.UserId == userId);
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
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.InventoryReservations)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId &&
                    x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetUserOrdersAsync(
        string tenantId,
        string userId,
        int page,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query =
            context.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.InventoryReservations)
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OrderStatus>(
                status,
                true,
                out var parsedStatus))
        {
            query = query.Where(
                x => x.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetTenantOrdersAsync(
        string tenantId,
        int page,
        int pageSize,
        string? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query =
            context.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.InventoryReservations)
                .Where(
                    x =>
                        x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OrderStatus>(
                status,
                true,
                out var parsedStatus))
        {
            query = query.Where(
                x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term =
                search.Trim();

            query = query.Where(
                x =>
                    x.OrderNumber.Contains(term) ||
                    x.UserId.Contains(term) ||
                    x.ShippingFullName.Contains(term) ||
                    x.ShippingPhone.Contains(term));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<int> CountUserOrdersAsync(
        string tenantId,
        string userId,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query =
            context.Orders.Where(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OrderStatus>(
                status,
                true,
                out var parsedStatus))
        {
            query = query.Where(
                x => x.Status == parsedStatus);
        }

        return await query.CountAsync(
            cancellationToken);
    }

    public async Task<int> CountTenantOrdersAsync(
        string tenantId,
        string? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query =
            context.Orders.Where(
                x =>
                    x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OrderStatus>(
                status,
                true,
                out var parsedStatus))
        {
            query = query.Where(
                x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term =
                search.Trim();

            query = query.Where(
                x =>
                    x.OrderNumber.Contains(term) ||
                    x.UserId.Contains(term) ||
                    x.ShippingFullName.Contains(term) ||
                    x.ShippingPhone.Contains(term));
        }

        return await query.CountAsync(
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