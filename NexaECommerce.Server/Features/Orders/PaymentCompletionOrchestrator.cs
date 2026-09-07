using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.Payments;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentCompletionOrchestrator(
    IPaymentAttemptRepository paymentAttemptRepository,
    IOrderRepository orderRepository,
    IInventoryService inventory,
    IOrderUnitOfWork unitOfWork,
    PaymentGatewayService gateways)
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

        /*
         * Fully completed payment is terminal and idempotent.
         */
        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Succeeded &&
            order.Status ==
            OrderStatus.Paid)
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

        /*
         * If the order was already marked Paid but the attempt did not
         * reach Succeeded, repair the payment attempt state.
         */
        if (order.Status ==
            OrderStatus.Paid)
        {
            if (paymentAttempt.Status !=
                PaymentAttemptStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Payment attempt is not in a recoverable state.");
            }

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

        if (order.InventoryReservations.Count == 0)
        {
            throw new InvalidOperationException(
                "The order does not contain inventory reservations.");
        }

        var normalizedGatewayName =
            gatewayName.Trim();

        var normalizedGatewayReference =
            gatewayReference.Trim();

        /*
         * For a normal Pending payment attempt, Completion performs
         * its own gateway verification.
         *
         * This prevents a caller from making an order Paid merely
         * by posting an arbitrary gateway reference to /complete.
         *
         * When the attempt is already Succeeded but the Order is still
         * PendingPayment, we treat that as a recoverable legacy/partial
         * state and continue with finalization.
         */
        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Pending)
        {
            if (string.IsNullOrWhiteSpace(
                    paymentAttempt.GatewayName))
            {
                throw new InvalidOperationException(
                    "Payment gateway has not been initialized for this payment attempt.");
            }

            if (!string.Equals(
                    paymentAttempt.GatewayName,
                    normalizedGatewayName,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Gateway name does not match the payment attempt.");
            }

            if (!string.IsNullOrWhiteSpace(
                    paymentAttempt.GatewayReference) &&
                !string.Equals(
                    paymentAttempt.GatewayReference,
                    normalizedGatewayReference,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Gateway reference does not match the payment attempt.");
            }

            var gateway =
                gateways.Get(
                    paymentAttempt.GatewayName);

            var gatewayVerification =
                await gateway.VerifyAsync(
                    new PaymentGatewayVerifyRequest(
                        order.OrderNumber,
                        paymentAttempt.Amount,
                        normalizedGatewayReference),
                    cancellationToken);

            if (!gatewayVerification.Succeeded)
            {
                throw new InvalidOperationException(
                    gatewayVerification.ErrorMessage ??
                    "Payment gateway verification failed.");
            }

            var verifiedReference =
                string.IsNullOrWhiteSpace(
                    gatewayVerification.GatewayReference)
                    ? normalizedGatewayReference
                    : gatewayVerification.GatewayReference.Trim();

            if (!string.Equals(
                    verifiedReference,
                    normalizedGatewayReference,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The gateway verification reference does not match the requested payment reference.");
            }

            /*
             * Keep the gateway identity/reference persisted while the
             * payment attempt remains Pending. The Paid transition is
             * still performed only below after inventory is committed.
             */
            if (string.IsNullOrWhiteSpace(
                    paymentAttempt.GatewayReference))
            {
                paymentAttempt.MarkGatewayCreated(
                    gateway.Name,
                    verifiedReference);

                await unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
        }
        else if (paymentAttempt.Status !=
                 PaymentAttemptStatus.Succeeded)
        {
            throw new InvalidOperationException(
                "Payment attempt is not in a valid state for completion.");
        }

        /*
         * Commit every reservation.
         *
         * This operation is intentionally retry-safe:
         * already-committed reservations are skipped by Inventory.
         *
         * If a later reservation fails, an earlier committed reservation
         * remains committed. A subsequent completion retry continues from
         * the remaining active reservations.
         */
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
         * At this point inventory is committed.
         *
         * Now finalize the payment attempt and the order in the Orders
         * unit of work.
         */
        paymentAttempt.MarkSucceeded(
            string.IsNullOrWhiteSpace(
                paymentAttempt.GatewayName)
                ? normalizedGatewayName
                : paymentAttempt.GatewayName,
            string.IsNullOrWhiteSpace(
                paymentAttempt.GatewayReference)
                ? normalizedGatewayReference
                : paymentAttempt.GatewayReference);

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
        if (string.IsNullOrWhiteSpace(
                tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(
                userId))
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

        if (string.IsNullOrWhiteSpace(
                gatewayName))
        {
            throw new ArgumentException(
                "Gateway name is required.",
                nameof(gatewayName));
        }

        if (string.IsNullOrWhiteSpace(
                gatewayReference))
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