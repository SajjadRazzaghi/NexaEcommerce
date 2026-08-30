using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IOrderService
{
    Task<OrderDto> CreateFromCheckoutAsync(
        string tenantId,
        string userId,
        string idempotencyKey,
        CheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDto?> GetAsync(
        string tenantId,
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        string tenantId,
        Guid orderId,
        string userId,
        CancellationToken cancellationToken = default);
}