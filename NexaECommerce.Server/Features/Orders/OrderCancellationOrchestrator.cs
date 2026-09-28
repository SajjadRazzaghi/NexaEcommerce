using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class OrderCancellationOrchestrator(
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IOrderConcurrencyService orderConcurrency,
    IWarehouseReservationOrchestrator warehouseReservation)
{
    public async Task CancelAsync(
        string tenantId,
        string userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        var normalizedTenantId =
            tenantId.Trim();

        var normalizedUserId =
            userId.Trim();

        await orderConcurrency.ExecuteAsync(
            normalizedTenantId,
            orderId,
            async ct =>
            {
                /*
                 * Fresh load after the lock.
                 */
                var order =
                    await orderRepository.GetByIdAsync(
                        normalizedTenantId,
                        orderId,
                        normalizedUserId,
                        ct);

                if (order is null)
                {
                    throw new KeyNotFoundException(
                        "Order was not found.");
                }

                if (order.Status is
                    OrderStatus.Shipped or
                    OrderStatus.Delivered)
                {
                    throw new InvalidOperationException(
                        "Shipped or delivered orders cannot be cancelled.");
                }

                /*
                 * Warehouse reservation release is intentionally
                 * performed while the order lock is held so payment,
                 * cancellation and reconciliation cannot manipulate
                 * the same order simultaneously.
                 */
                await warehouseReservation.ReleaseAsync(
                    normalizedTenantId,
                    orderId,
                    ct);

                order.Cancel();

                await orderUnitOfWork.SaveChangesAsync(
                    ct);
            },
            cancellationToken);
    }
}