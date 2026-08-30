using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.Services;
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
         * Callback/webhook retries must be harmless.
         *
         * A completed payment attempt is already authoritative.
         */
        if (paymentAttempt.Status ==
            NexaEcommerce.Modules.Orders.Domain.Entities
                .PaymentAttemptStatus.Succeeded)
        {
            return new PaymentCompletionResult(
                paymentAttempt.Id,
                paymentAttempt.OrderId,
                "Succeeded",
                true);
        }

        if (paymentAttempt.Status ==
            NexaEcommerce.Modules.Orders.Domain.Entities
                .PaymentAttemptStatus.Failed)
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
            NexaEcommerce.Modules.Orders.Domain.Entities
                .OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "A cancelled order cannot be paid.");
        }

        if (order.Status ==
            NexaEcommerce.Modules.Orders.Domain.Entities
                .OrderStatus.Paid)
        {
            paymentAttempt.MarkSucceeded(
                gatewayName,
                gatewayReference);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new PaymentCompletionResult(
                paymentAttempt.Id,
                paymentAttempt.OrderId,
                "Succeeded",
                true);
        }

        if (order.Status !=
            NexaEcommerce.Modules.Orders.Domain.Entities
                .OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "The order is not in a valid state for payment completion.");
        }

        /*
         * Inventory is the source of truth for physical stock.
         *
         * We commit every reservation recorded on the order.
         *
         * Inventory Commit is itself idempotent, therefore a webhook
         * retry is safe even if the gateway calls us twice.
         */
        foreach (var reservation
                 in order.InventoryReservations)
        {
            if (reservation.Status ==
                NexaEcommerce.Modules.Orders.Domain.Entities
                    .InventoryReservationStatus.Committed)
            {
                continue;
            }

            if (reservation.Status !=
                NexaEcommerce.Modules.Orders.Domain.Entities
                    .InventoryReservationStatus.Reserved)
            {
                throw new InvalidOperationException(
                    $"Inventory reservation '{reservation.ReservationKey}' " +
                    "is no longer available for payment completion.");
            }

            await inventory.CommitAsync(
                tenantId,
                reservation.ReservationKey,
                cancellationToken);

            reservation.MarkCommitted();
        }

        paymentAttempt.MarkSucceeded(
            gatewayName,
            gatewayReference);

        order.MarkPaid();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new PaymentCompletionResult(
            paymentAttempt.Id,
            paymentAttempt.OrderId,
            "Succeeded",
            false);
    }
}

public sealed record PaymentCompletionResult(
    Guid PaymentAttemptId,
    Guid OrderId,
    string Status,
    bool AlreadyCompleted);