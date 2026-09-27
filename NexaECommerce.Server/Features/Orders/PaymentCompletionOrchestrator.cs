using NexaEcommerce.Modules.Orders.Application.Payments;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Application.Services;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentCompletionOrchestrator(
    IPaymentAttemptRepository paymentAttemptRepository,
    IOrderRepository orderRepository,
    IOrderUnitOfWork unitOfWork,
    IInventoryService inventory,
    PaymentGatewayService gateways,
    PaymentReservationOrchestrator paymentReservations)
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

        var normalizedTenantId =
            tenantId.Trim();

        var normalizedUserId =
            userId.Trim();

        var normalizedGatewayName =
            gatewayName.Trim();

        var normalizedGatewayReference =
            gatewayReference.Trim();

        var paymentAttempt =
            await paymentAttemptRepository.GetByIdAsync(
                normalizedTenantId,
                normalizedUserId,
                paymentAttemptId,
                cancellationToken);

        if (paymentAttempt is null)
        {
            throw new KeyNotFoundException(
                "Payment attempt was not found.");
        }

        var order =
            await orderRepository.GetByIdAsync(
                normalizedTenantId,
                paymentAttempt.OrderId,
                normalizedUserId,
                cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException(
                "Order was not found.");
        }

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

        if (order.Status is
            OrderStatus.Processing or
            OrderStatus.Shipped or
            OrderStatus.Delivered)
        {
            throw new InvalidOperationException(
                "The order is already in a post-payment lifecycle state.");
        }

        /*
         * IMPORTANT:
         *
         * A Paid order must NOT cause a different pending payment
         * attempt to become successful.
         *
         * This prevents payment attempt B from being marked succeeded
         * merely because payment attempt A already paid the order.
         */
        if (order.Status ==
            OrderStatus.Paid)
        {
            throw new InvalidOperationException(
                "The order has already been paid by another payment attempt.");
        }

        if (order.Status !=
            OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "The order is not in a valid state for payment completion.");
        }

        /*
         * Always validate the persisted payment attempt before
         * touching inventory.
         */
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

        if (string.IsNullOrWhiteSpace(
                paymentAttempt.GatewayReference))
        {
            throw new InvalidOperationException(
                "Payment attempt does not contain a gateway reference.");
        }

        if (!string.Equals(
                paymentAttempt.GatewayReference.Trim(),
                normalizedGatewayReference,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Gateway reference does not match the payment attempt.");
        }

        /*
         * The checkout reservation may have expired before the user
         * returns from the gateway.
         *
         * Recover/recreate the reservation before completing payment.
         */
        await paymentReservations.EnsureForCompletionAsync(
            normalizedTenantId,
            normalizedUserId,
            paymentAttempt.OrderId,
            paymentAttempt.Id,
            cancellationToken);

        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Pending)
        {
            var gateway =
                gateways.Get(
                    paymentAttempt.GatewayName);

            var verification =
                await gateway.VerifyAsync(
                    new PaymentGatewayVerifyRequest(
                        order.OrderNumber,
                        paymentAttempt.Amount,
                        normalizedGatewayReference),
                    cancellationToken);

            if (!verification.Succeeded)
            {
                throw new PaymentVerificationException(
                    verification.ErrorMessage ??
                    "Payment gateway verification failed.",
                    verification.ErrorCode);
            }

            var verifiedReference =
                string.IsNullOrWhiteSpace(
                    verification.GatewayReference)
                    ? normalizedGatewayReference
                    : verification.GatewayReference.Trim();

            if (!string.Equals(
                    verifiedReference,
                    normalizedGatewayReference,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The gateway verification reference does not match the requested payment reference.");
            }

            paymentAttempt.MarkSucceeded(
                gateway.Name,
                verifiedReference);
        }
        else if (paymentAttempt.Status !=
                 PaymentAttemptStatus.Succeeded)
        {
            throw new InvalidOperationException(
                "Payment attempt is not in a valid state for completion.");
        }

        /*
         * Global inventory was reserved during checkout/payment recovery.
         *
         * Payment commits those reservations.
         */
        foreach (var orderReservation in
                 order.InventoryReservations
                     .Where(
                         x =>
                             x.Status is
                                 InventoryReservationStatus.Reserved or
                                 InventoryReservationStatus.Committed)
                     .OrderBy(
                         x => x.ReservationKey))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await inventory.CommitAsync(
                normalizedTenantId,
                orderReservation.ReservationKey,
                cancellationToken);
        }

        order.MarkInventoryReservationsCommitted();

        if (paymentAttempt.Status !=
            PaymentAttemptStatus.Succeeded)
        {
            paymentAttempt.MarkSucceeded(
                normalizedGatewayName,
                normalizedGatewayReference);
        }

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

public sealed class PaymentVerificationException
    : InvalidOperationException
{
    public PaymentVerificationException(
        string message,
        string? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string? ErrorCode
    {
        get;
    }
}

public sealed record PaymentCompletionResult(
    Guid PaymentAttemptId,
    Guid OrderId,
    string Status,
    bool AlreadyCompleted);