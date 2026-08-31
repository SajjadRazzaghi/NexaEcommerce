using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class OrderCancellationOrchestrator(
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IInventoryService inventory)
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

        var order =
            await orderRepository.GetByIdAsync(
                tenantId,
                orderId,
                userId,
                cancellationToken);

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
         * Only inventory reservations which are still Reserved
         * must be released.
         *
         * Committed reservations belong to already-paid stock and
         * must not be returned to available inventory by cancellation.
         */
        foreach (var reservation in
                 order.InventoryReservations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reservation.Status !=
                InventoryReservationStatus.Reserved)
            {
                continue;
            }

            await inventory.ReleaseAsync(
                tenantId,
                reservation.ReservationKey,
                cancellationToken);
        }

        /*
         * Order.Cancel() updates the aggregate-side reservation state
         * from Reserved to Released and changes the order status.
         */
        order.Cancel();

        await orderUnitOfWork.SaveChangesAsync(
            cancellationToken);
    }
}
