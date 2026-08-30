using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class PaymentAttemptService(
    IPaymentAttemptRepository paymentAttempts,
    IOrderRepository orders,
    IOrderUnitOfWork unitOfWork)
    : IPaymentAttemptService
{
    public async Task<PaymentAttemptDto> CreateAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(
            tenantId,
            userId,
            idempotencyKey);

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        var existing =
            await paymentAttempts.GetByIdempotencyKeyAsync(
                tenantId,
                userId,
                idempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.OrderId != orderId)
            {
                throw new InvalidOperationException(
                    "The payment idempotency key is already associated with another order.");
            }

            return Map(existing);
        }

        var order =
            await orders.GetByIdAsync(
                tenantId,
                orderId,
                userId,
                cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException(
                "Order was not found.");
        }

        if (order.Status !=
            OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "Only orders pending payment can create a payment attempt.");
        }

        var paymentAttempt =
            PaymentAttempt.Create(
                order.Id,
                tenantId,
                userId,
                idempotencyKey,
                order.TotalAmount,
                order.Currency);

        await paymentAttempts.AddAsync(
            paymentAttempt,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(paymentAttempt);
    }

    public async Task<PaymentAttemptDto?> GetAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        CancellationToken cancellationToken = default)
    {
        if (paymentAttemptId == Guid.Empty)
        {
            return null;
        }

        var attempt =
            await paymentAttempts.GetByIdAsync(
                tenantId,
                userId,
                paymentAttemptId,
                cancellationToken);

        return attempt is null
            ? null
            : Map(attempt);
    }

    public async Task<PaymentAttemptDto> MarkSucceededAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        string gatewayName,
        string gatewayReference,
        CancellationToken cancellationToken = default)
    {
        var attempt =
            await GetEntityAsync(
                tenantId,
                userId,
                paymentAttemptId,
                cancellationToken);

        if (attempt.Status ==
            PaymentAttemptStatus.Succeeded)
        {
            return Map(attempt);
        }

        if (attempt.Status ==
            PaymentAttemptStatus.Failed)
        {
            throw new InvalidOperationException(
                "A failed payment attempt cannot be marked as succeeded.");
        }

        var order =
            await orders.GetByIdAsync(
                tenantId,
                attempt.OrderId,
                userId,
                cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException(
                "Order was not found.");
        }

        if (order.Status ==
            OrderStatus.PendingPayment)
        {
            /*
             * Payment success is the business event that
             * transitions the order out of PendingPayment.
             */
            order.MarkPaid();
        }
        else if (order.Status !=
                 OrderStatus.Paid)
        {
            throw new InvalidOperationException(
                "The order is not in a valid state for successful payment.");
        }

        attempt.MarkSucceeded(
            gatewayName,
            gatewayReference);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(attempt);
    }

    public async Task<PaymentAttemptDto> MarkFailedAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        string? failureCode,
        string? failureMessage,
        CancellationToken cancellationToken = default)
    {
        var attempt =
            await GetEntityAsync(
                tenantId,
                userId,
                paymentAttemptId,
                cancellationToken);

        if (attempt.Status ==
            PaymentAttemptStatus.Failed)
        {
            return Map(attempt);
        }

        if (attempt.Status ==
            PaymentAttemptStatus.Succeeded)
        {
            throw new InvalidOperationException(
                "A successful payment attempt cannot be marked as failed.");
        }

        attempt.MarkFailed(
            failureCode,
            failureMessage);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(attempt);
    }

    private async Task<PaymentAttempt> GetEntityAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        CancellationToken cancellationToken)
    {
        if (paymentAttemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment attempt id is required.",
                nameof(paymentAttemptId));
        }

        var attempt =
            await paymentAttempts.GetByIdAsync(
                tenantId,
                userId,
                paymentAttemptId,
                cancellationToken);

        if (attempt is null)
        {
            throw new InvalidOperationException(
                "Payment attempt was not found.");
        }

        return attempt;
    }

    private static void ValidateIdentity(
        string tenantId,
        string userId,
        string idempotencyKey)
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

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Payment idempotency key is required.",
                nameof(idempotencyKey));
        }

        if (idempotencyKey.Trim().Length > 128)
        {
            throw new ArgumentException(
                "Payment idempotency key cannot exceed 128 characters.",
                nameof(idempotencyKey));
        }
    }

    private static PaymentAttemptDto Map(
        PaymentAttempt attempt)
    {
        return new PaymentAttemptDto(
            attempt.Id,
            attempt.OrderId,
            attempt.Status.ToString(),
            attempt.Amount,
            attempt.Currency,
            attempt.GatewayName,
            attempt.GatewayReference,
            attempt.FailureCode,
            attempt.FailureMessage,
            attempt.CreatedAt,
            attempt.CompletedAt);
    }
}