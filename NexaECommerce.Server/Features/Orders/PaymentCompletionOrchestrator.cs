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

        if (!order.HasActiveInventoryReservations &&
            !order.HasCommittedInventoryReservations)
        {
            throw new InvalidOperationException(
                "The order has no active inventory reservation.");
        }

        var normalizedGatewayName =
            gatewayName.Trim();

        var normalizedGatewayReference =
            gatewayReference.Trim();

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

            var verification =
                await gateway.VerifyAsync(
                    new PaymentGatewayVerifyRequest(
                        order.OrderNumber,
                        paymentAttempt.Amount,
                        normalizedGatewayReference),
                    cancellationToken);

            if (!verification.Succeeded)
            {
                throw new InvalidOperationException(
                    verification.ErrorMessage ??
                    "Payment gateway verification failed.");
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
         * IMPORTANT:
         *
         * Physical stock has already been reserved in WarehouseStock
         * during checkout.
         *
         * Payment does NOT reserve or commit physical stock again.
         *
         * It only commits the logical order reservations.
         */
        /*
    * Global inventory was reserved during checkout.
    *
    * Payment commits the global StockItem reservation.
    * Physical WarehouseStock remains reserved until fulfillment.
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
                tenantId,
                orderReservation.ReservationKey,
                cancellationToken);
        }

        order.MarkInventoryReservationsCommitted();

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