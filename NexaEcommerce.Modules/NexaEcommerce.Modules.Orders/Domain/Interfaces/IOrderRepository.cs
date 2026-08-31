using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        string tenantId,
        Guid id,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByOrderNumberAsync(
        string tenantId,
        string orderNumber,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdempotencyKeyAsync(
        string tenantId,
        string userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetUserOrdersAsync(
        string tenantId,
        string userId,
        int page,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetTenantOrdersAsync(
        string tenantId,
        int page,
        int pageSize,
        string? status = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<int> CountUserOrdersAsync(
        string tenantId,
        string userId,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<int> CountTenantOrdersAsync(
        string tenantId,
        string? status = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);
}