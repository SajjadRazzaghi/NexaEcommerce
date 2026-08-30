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

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);
}