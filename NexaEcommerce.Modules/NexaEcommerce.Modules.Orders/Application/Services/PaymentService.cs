using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Payments;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class PaymentService(
IPaymentAttemptRepository paymentAttempts,
IOrderRepository orders,
IOrderUnitOfWork unitOfWork,
PaymentGatewayService gateways,
IPaymentAttemptService paymentAttemptService)
: IPaymentService
{
    public async Task<CreatePaymentResultDto> CreatePaymentAsync(
    string tenantId,
    string userId,
    Guid orderId,
    string idempotencyKey,
    string gatewayName,
    string callbackUrl,
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

        if (string.IsNullOrWhiteSpace(gatewayName))
        {
            throw new ArgumentException(
                "Gateway name is required.",
                nameof(gatewayName));
        }

        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            throw new ArgumentException(
                "Callback URL is required.",
                nameof(callbackUrl));
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

        var existing =
            await paymentAttempts.GetByIdempotencyKeyAsync(
                tenantId,
                userId,
                idempotencyKey,
                cancellationToken);

        PaymentAttempt paymentAttempt;

        if (existing is not null)
        {
            if (existing.OrderId != orderId)
            {
                throw new InvalidOperationException(
                    "The payment idempotency key is already associated with another order.");
            }

            paymentAttempt = existing;
        }
        else
        {
            if (order.Status !=
                OrderStatus.PendingPayment)
            {
                throw new InvalidOperationException(
                    "Only orders pending payment can start payment.");
            }

            await paymentAttemptService.CreateAsync(
                tenantId,
                userId,
                orderId,
                idempotencyKey,
                cancellationToken);

            paymentAttempt =
                await paymentAttempts.GetByIdempotencyKeyAsync(
                    tenantId,
                    userId,
                    idempotencyKey,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Payment attempt could not be loaded after creation.");
        }

        var gateway =
            gateways.Get(gatewayName);

        /*
         * A previously successful attempt is terminal.
         */
        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Succeeded)
        {
            return new CreatePaymentResultDto(
                paymentAttempt.Id,
                paymentAttempt.OrderId,
                paymentAttempt.GatewayName ??
                    gateway.Name,
                paymentAttempt.Status.ToString(),
                paymentAttempt.Amount,
                paymentAttempt.Currency,
                null,
                paymentAttempt.GatewayReference);
        }

        /*
         * A failed attempt must not silently become a new payment under
         * the same idempotency key.
         */
        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Failed)
        {
            throw new InvalidOperationException(
                "The payment attempt has already failed. Use a new idempotency key.");
        }

        /*
         * The gateway may already have been initialized during a retry.
         * Return the existing payment information rather than creating a
         * second gateway transaction.
         */
        if (!string.IsNullOrWhiteSpace(
                paymentAttempt.GatewayReference))
        {
            return new CreatePaymentResultDto(
                paymentAttempt.Id,
                paymentAttempt.OrderId,
                paymentAttempt.GatewayName ??
                    gateway.Name,
                paymentAttempt.Status.ToString(),
                paymentAttempt.Amount,
                paymentAttempt.Currency,
                null,
                paymentAttempt.GatewayReference);
        }

        var gatewayResult =
            await gateway.CreateAsync(
                new PaymentGatewayCreateRequest(
                    order.OrderNumber,
                    paymentAttempt.Amount,
                    paymentAttempt.Currency,
                    callbackUrl),
                cancellationToken);

        if (!gatewayResult.Succeeded)
        {
            paymentAttempt.MarkFailed(
                gatewayResult.ErrorCode,
                gatewayResult.ErrorMessage);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            throw new InvalidOperationException(
                gatewayResult.ErrorMessage ??
                "Payment gateway could not create a payment.");
        }

        if (string.IsNullOrWhiteSpace(
                gatewayResult.GatewayReference))
        {
            paymentAttempt.MarkFailed(
                "MISSING_GATEWAY_REFERENCE",
                "Payment gateway did not return a gateway reference.");

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            throw new InvalidOperationException(
                "Payment gateway did not return a gateway reference.");
        }

        paymentAttempt.MarkGatewayCreated(
            gateway.Name,
            gatewayResult.GatewayReference);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new CreatePaymentResultDto(
            paymentAttempt.Id,
            paymentAttempt.OrderId,
            gateway.Name,
            PaymentAttemptStatus.Pending.ToString(),
            paymentAttempt.Amount,
            paymentAttempt.Currency,
            gatewayResult.PaymentUrl,
            gatewayResult.GatewayReference);
    }

    public async Task<PaymentAttemptDto> VerifyPaymentAsync(
        string tenantId,
        string userId,
        Guid paymentAttemptId,
        string gatewayReference,
        CancellationToken cancellationToken = default)
    {
        if (paymentAttemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment attempt id is required.",
                nameof(paymentAttemptId));
        }

        if (string.IsNullOrWhiteSpace(gatewayReference))
        {
            throw new ArgumentException(
                "Gateway reference is required.",
                nameof(gatewayReference));
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

        if (attempt.Status ==
            PaymentAttemptStatus.Succeeded)
        {
            return Map(attempt);
        }

        if (attempt.Status ==
            PaymentAttemptStatus.Failed)
        {
            throw new InvalidOperationException(
                "A failed payment attempt cannot be verified.");
        }

        if (string.IsNullOrWhiteSpace(
                attempt.GatewayName))
        {
            throw new InvalidOperationException(
                "Payment gateway has not been initialized for this payment attempt.");
        }

        if (!string.IsNullOrWhiteSpace(
                attempt.GatewayReference) &&
            !string.Equals(
                attempt.GatewayReference,
                gatewayReference.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Gateway reference does not match the payment attempt.");
        }

        var gateway =
            gateways.Get(
                attempt.GatewayName);

        var result =
            await gateway.VerifyAsync(
                new PaymentGatewayVerifyRequest(
                    attempt.OrderId.ToString("N"),
                    attempt.Amount,
                    gatewayReference.Trim()),
                cancellationToken);

        if (!result.Succeeded)
        {
            await paymentAttemptService.MarkFailedAsync(
                tenantId,
                userId,
                paymentAttemptId,
                result.ErrorCode,
                result.ErrorMessage,
                cancellationToken);

            throw new InvalidOperationException(
                result.ErrorMessage ??
                "Payment verification failed.");
        }

        return await paymentAttemptService.MarkSucceededAsync(
            tenantId,
            userId,
            paymentAttemptId,
            gateway.Name,
            result.GatewayReference ??
                gatewayReference.Trim(),
            cancellationToken);
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
