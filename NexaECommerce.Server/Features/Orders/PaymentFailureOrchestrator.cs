using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentFailureOrchestrator(
    IPaymentAttemptService paymentAttempts,
    IPaymentAttemptRepository paymentAttemptRepository,
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IInventoryService inventory)
{
    public async Task<PaymentFailureResult> FailAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        string? failureCode,
        string? failureMessage,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(
            tenantId,
            userId,
            paymentAttemptId);

        var paymentAttempt =
            await paymentAttemptRepository.GetByIdAsync(
                tenantId,
                userId,
                paymentAttemptId,
                cancellationToken);

        if (paymentAttempt is null)
        {
            throw new KeyNotFoundException(
                "Payment attempt was not found.");
        }

        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Succeeded)
        {
            throw new InvalidOperationException(
                "A successful payment attempt cannot be marked as failed.");
        }

        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Failed)
        {
            return new PaymentFailureResult(
                paymentAttempt.Id,
                paymentAttempt.OrderId,
                "Failed",
                true,
                0);
        }

        var order =
            await orderRepository.GetByIdAsync(
                tenantId,
                paymentAttempt.OrderId,
                userId,
                cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException(
                "Order was not found.");
        }

        if (order.Status is
            OrderStatus.Cancelled or
            OrderStatus.Shipped or
            OrderStatus.Delivered)
        {
            throw new InvalidOperationException(
                "The order is no longer eligible for payment failure handling.");
        }

        var releasedCount = 0;

        /*
         * Release every active inventory reservation belonging
         * to this order before allowing another payment attempt.
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

            try
            {
                await inventory.ReleaseAsync(
                    tenantId,
                    reservation.ReservationKey,
                    cancellationToken);

                reservation.MarkReleased();

                releasedCount++;
            }
            catch (KeyNotFoundException)
            {
                /*
                 * Inventory may already have released the reservation
                 * through an expiration/compensation process.
                 *
                 * Keep the Order aggregate synchronized.
                 */
                reservation.MarkReleased();

                releasedCount++;
            }
        }

        await paymentAttempts.MarkFailedAsync(
            tenantId,
            userId,
            paymentAttemptId,
            failureCode,
            failureMessage,
            cancellationToken);

        await orderUnitOfWork.SaveChangesAsync(
            cancellationToken);

        return new PaymentFailureResult(
            paymentAttempt.Id,
            paymentAttempt.OrderId,
            "Failed",
            false,
            releasedCount);
    }

    private static void ValidateInput(
        string tenantId,
        string userId,
        Guid paymentAttemptId)
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

        if (paymentAttemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment attempt id is required.",
                nameof(paymentAttemptId));
        }
    }
}

public sealed record PaymentFailureResult(
    Guid PaymentAttemptId,
    Guid OrderId,
    string Status,
    bool AlreadyCompleted,
    int ReleasedReservations);