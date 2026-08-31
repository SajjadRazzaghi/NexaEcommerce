using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentCompletionOrchestrator(
IPaymentAttemptRepository paymentAttemptRepository,
IOrderRepository orderRepository,
IInventoryService inventory,
IOrderUnitOfWork unitOfWork)
{
    public async Task<PaymentCompletionResult> CompleteAsync(
    string tenantId,
    string userId,
    Guid paymentAttemptId,
    string gatewayName,
    string gatewayReference,
    CancellationToken cancellationToken = default)
    {
        ValidateInput(
        tenantId,
        userId,
        paymentAttemptId,
        gatewayName,
        gatewayReference);

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

        /*
         * A successful payment completion is idempotent.
         *
         * This is essential for real payment gateways because
         * callbacks/webhooks may be delivered more than once.
         */
        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Succeeded)
        {
            return new PaymentCompletionResult(
                paymentAttempt.Id,
                paymentAttempt.OrderId,
                "Succeeded",
                true);
        }

        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Failed)
        {
            throw new InvalidOperationException(
                "A failed payment attempt cannot be completed.");
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

        if (order.Status ==
            OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "A cancelled order cannot be paid.");
        }

        if (order.Status ==
            OrderStatus.Delivered ||
            order.Status ==
            OrderStatus.Shipped ||
            order.Status ==
            OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "The order is already in a post-payment lifecycle state.");
        }

        if (order.Status ==
            OrderStatus.Paid)
        {
            /*
             * Order is already paid but PaymentAttempt is still pending.
             *
             * This can only happen after a partial previous operation.
             * Completing the payment attempt restores consistency.
             */
            paymentAttempt.MarkSucceeded(
                gatewayName.Trim(),
                gatewayReference.Trim());

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new PaymentCompletionResult(
                paymentAttempt.Id,
                paymentAttempt.OrderId,
                "Succeeded",
                true);
        }

        if (order.Status !=
            OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "The order is not in a valid state for payment completion.");
        }

        /*
         * Every reservation must be committed before the order
         * becomes Paid.
         *
         * If a reservation is missing, payment completion must fail
         * rather than creating an order whose stock is not secured.
         */
        if (order.InventoryReservations.Count == 0)
        {
            throw new InvalidOperationException(
                "The order does not contain inventory reservations.");
        }

        foreach (var reservation in
                 order.InventoryReservations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reservation.Status)
            {
                case InventoryReservationStatus.Committed:
                    continue;

                case InventoryReservationStatus.Reserved:

                    await inventory.CommitAsync(
                        tenantId,
                        reservation.ReservationKey,
                        cancellationToken);

                    reservation.MarkCommitted();
                    break;

                case InventoryReservationStatus.Released:
                    throw new InvalidOperationException(
                        $"Inventory reservation '{reservation.ReservationKey}' has already been released.");

                case InventoryReservationStatus.Expired:
                    throw new InvalidOperationException(
                        $"Inventory reservation '{reservation.ReservationKey}' has expired.");

                default:
                    throw new InvalidOperationException(
                        $"Inventory reservation '{reservation.ReservationKey}' has an invalid status.");
            }
        }

        /*
         * Inventory is now committed.
         *
         * PaymentAttempt and Order are transitioned together from
         * the Orders aggregate's unit of work.
         */
        paymentAttempt.MarkSucceeded(
            gatewayName.Trim(),
            gatewayReference.Trim());

        order.MarkPaid();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new PaymentCompletionResult(
            paymentAttempt.Id,
            paymentAttempt.OrderId,
            "Succeeded",
            false);
    }

    private static void ValidateInput(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        string gatewayName,
        string gatewayReference)
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

        if (string.IsNullOrWhiteSpace(gatewayName))
        {
            throw new ArgumentException(
                "Gateway name is required.",
                nameof(gatewayName));
        }

        if (string.IsNullOrWhiteSpace(gatewayReference))
        {
            throw new ArgumentException(
                "Gateway reference is required.",
                nameof(gatewayReference));
        }
    }

}

public sealed record PaymentCompletionResult(
Guid PaymentAttemptId,
Guid OrderId,
string Status,
bool AlreadyCompleted);
