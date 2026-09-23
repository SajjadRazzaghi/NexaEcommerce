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

        tenantId = tenantId.Trim();
        userId = userId.Trim();

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
         * Release the global inventory reservation immediately.
         *
         * The order reservation is the durable logical record.
         * InventoryService owns the actual StockItem reservation.
         */
        foreach (var reservation in
                 order.InventoryReservations
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
                 *
                 * The order-level state is still reconciled below.
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
                 * reconciliation worker can repair an inconsistent
                 * reservation state after the order is marked released.
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
         * Release any physical warehouse reservations.
         *
         * This is deliberately best-effort because a failed payment
         * must still be persisted as Failed even when a warehouse
         * cleanup operation encounters a transient problem.
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

