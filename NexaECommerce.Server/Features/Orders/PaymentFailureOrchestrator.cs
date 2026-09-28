using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentFailureOrchestrator(
    [FromServices] IPaymentAttemptService paymentAttempts,
    IPaymentAttemptRepository paymentAttemptRepository,
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IInventoryService inventory,
    IWarehouseReservationOrchestrator warehouseReservation,
    IOrderConcurrencyService orderConcurrency,
    ILogger<PaymentFailureOrchestrator> logger)
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

        var normalizedTenantId =
            tenantId.Trim();

        var normalizedUserId =
            userId.Trim();

        /*
         * First load is only used to discover OrderId.
         *
         * After acquiring the order lock the ChangeTracker is cleared,
         * and FailCoreAsync loads a completely fresh PaymentAttempt.
         */
        var initialPaymentAttempt =
            await paymentAttemptRepository.GetByIdAsync(
                normalizedTenantId,
                normalizedUserId,
                paymentAttemptId,
                cancellationToken);

        if (initialPaymentAttempt is null)
        {
            throw new KeyNotFoundException(
                "Payment attempt was not found.");
        }

        return await orderConcurrency.ExecuteAsync(
            normalizedTenantId,
            initialPaymentAttempt.OrderId,
            ct =>
                FailCoreAsync(
                    normalizedTenantId,
                    normalizedUserId,
                    paymentAttemptId,
                    failureCode,
                    failureMessage,
                    ct),
            cancellationToken);
    }

    private async Task<PaymentFailureResult>
        FailCoreAsync(
            string tenantId,
            string userId,
            Guid paymentAttemptId,
            string? failureCode,
            string? failureMessage,
            CancellationToken cancellationToken)
    {
        /*
         * Fresh PaymentAttempt AFTER acquiring the order lock.
         */
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

        /*
         * If another request already paid the order while this request
         * was waiting for the lock, do not mark this payment attempt
         * as failed as part of an unrelated lifecycle transition.
         */
        if (order.Status == OrderStatus.Paid)
        {
            throw new InvalidOperationException(
                "The order has already been paid by another payment attempt.");
        }

        var releasedCount = 0;

        /*
         * Release global inventory reservation immediately.
         */
        foreach (
            var reservation
            in order.InventoryReservations
                .Where(
                    x =>
                        x.Status ==
                        InventoryReservationStatus.Reserved)
                .OrderBy(
                    x =>
                        x.ReservationKey))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await inventory.ReleaseAsync(
                    tenantId,
                    reservation.ReservationKey,
                    cancellationToken);
            }
            catch (KeyNotFoundException ex)
            {
                /*
                 * The inventory reservation may already have been
                 * released or expired independently.
                 */
                logger.LogWarning(
                    ex,
                    "Inventory reservation {ReservationKey} was not found while failing payment attempt {PaymentAttemptId}.",
                    reservation.ReservationKey,
                    paymentAttemptId);
            }
            catch (InvalidOperationException ex)
            {
                /*
                 * Preserve payment failure processing. The inventory
                 * reconciliation worker can repair any remaining
                 * discrepancy.
                 */
                logger.LogWarning(
                    ex,
                    "Inventory reservation {ReservationKey} could not be released while failing payment attempt {PaymentAttemptId}.",
                    reservation.ReservationKey,
                    paymentAttemptId);
            }

            reservation.MarkReleased();

            releasedCount++;
        }

        /*
         * Release physical warehouse reservations.
         *
         * This remains best-effort.
         */
        try
        {
            await warehouseReservation.ReleaseAsync(
                tenantId,
                paymentAttempt.OrderId,
                cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(
                ex,
                "Warehouse reservation was not found while failing payment attempt {PaymentAttemptId} for order {OrderId}.",
                paymentAttemptId,
                paymentAttempt.OrderId);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                ex,
                "Warehouse reservation could not be released while failing payment attempt {PaymentAttemptId} for order {OrderId}.",
                paymentAttemptId,
                paymentAttempt.OrderId);
        }

        /*
         * Mark the fresh tracked payment attempt as failed.
         */
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