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
        // --------------------------------------------------
        // Validate input
        // --------------------------------------------------

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

        var normalizedGatewayName =
            gatewayName.Trim();

        // --------------------------------------------------
        // Load order
        // --------------------------------------------------

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

        // --------------------------------------------------
        // Find existing payment attempt
        // --------------------------------------------------

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

        // --------------------------------------------------
        // Validate order and payment attempt
        // --------------------------------------------------

        if (!string.Equals(
            paymentAttempt.Currency,
            order.Currency,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Payment currency does not match order currency. Please check the order before starting payment.");
        }

        if (paymentAttempt.Amount != order.TotalAmount)
        {
            throw new InvalidOperationException(
                "Payment amount does not match order total. Please check the order before starting payment.");
        }

        // اگر تلاش قبلی به درگاه دیگری تعلق دارد،
        // نباید آن را با درگاه جدید ادامه بدهیم.
        if (!string.IsNullOrWhiteSpace(
                paymentAttempt.GatewayName) &&
            !string.Equals(
                paymentAttempt.GatewayName,
                normalizedGatewayName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The payment attempt is already associated with another payment gateway.");
        }

        // --------------------------------------------------
        // Resolve gateway
        // --------------------------------------------------

        var gateway =
            gateways.Get(
                normalizedGatewayName);

        // --------------------------------------------------
        // Already succeeded
        // --------------------------------------------------

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

        // --------------------------------------------------
        // Failed attempt cannot be reused
        // --------------------------------------------------

        if (paymentAttempt.Status ==
            PaymentAttemptStatus.Failed)
        {
            throw new InvalidOperationException(
                "The payment attempt has already failed. Use a new idempotency key.");
        }

        // --------------------------------------------------
        // Existing gateway reference
        // Do not create a duplicate gateway request
        // --------------------------------------------------

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

        // --------------------------------------------------
        // Create payment at gateway
        // --------------------------------------------------

        var gatewayResult =
            await gateway.CreateAsync(
                new PaymentGatewayCreateRequest(
                    order.OrderNumber,
                    paymentAttempt.Amount,
                    paymentAttempt.Currency,
                    callbackUrl.Trim()),
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

        // --------------------------------------------------
        // Validate gateway response
        // --------------------------------------------------

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

        // --------------------------------------------------
        // Save gateway information
        // --------------------------------------------------

        paymentAttempt.MarkGatewayCreated(
            gateway.Name,
            gatewayResult.GatewayReference);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        // --------------------------------------------------
        // Return payment result
        // --------------------------------------------------

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

        if (string.IsNullOrWhiteSpace(gatewayReference))
        {
            throw new ArgumentException(
                "Gateway reference is required.",
                nameof(gatewayReference));
        }

        // --------------------------------------------------
        // Load payment attempt
        // --------------------------------------------------

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

        // پرداخت قبلاً با موفقیت تأیید شده است.
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

        // --------------------------------------------------
        // Load related order
        // --------------------------------------------------

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

        // --------------------------------------------------
        // Validate payment amount and currency
        // --------------------------------------------------

        if (!string.Equals(
            attempt.Currency,
            order.Currency,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Payment currency does not match order currency.");
        }

        if (attempt.Amount != order.TotalAmount)
        {
            throw new InvalidOperationException(
                "Payment amount does not match order total.");
        }

        // --------------------------------------------------
        // Validate gateway reference
        // --------------------------------------------------

        var normalizedReference =
            gatewayReference.Trim();

        if (!string.IsNullOrWhiteSpace(
                attempt.GatewayReference) &&
            !string.Equals(
                attempt.GatewayReference.Trim(),
                normalizedReference,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Gateway reference does not match the payment attempt.");
        }

        // --------------------------------------------------
        // Verify payment with gateway
        // --------------------------------------------------

        var gateway =
            gateways.Get(
                attempt.GatewayName);

        var result =
            await gateway.VerifyAsync(
                new PaymentGatewayVerifyRequest(
                    attempt.OrderId.ToString("N"),
                    attempt.Amount,
                    normalizedReference,
                    attempt.Currency),
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

        // --------------------------------------------------
        // Normalize verified gateway reference
        // --------------------------------------------------

        var verifiedReference =
            string.IsNullOrWhiteSpace(
                result.GatewayReference)
                ? normalizedReference
                : result.GatewayReference.Trim();

        if (!string.Equals(
            verifiedReference,
            normalizedReference,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Verified gateway reference does not match the payment attempt.");
        }

        // --------------------------------------------------
        // Save gateway reference if not already stored
        // --------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                attempt.GatewayReference))
        {
            attempt.MarkGatewayCreated(
                gateway.Name,
                verifiedReference);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        /*
         * IMPORTANT:
         *
         * Verification only confirms the gateway transaction.
         *
         * It does NOT mark the order as Paid.
         *
         * Final payment completion and inventory commitment
         * belong exclusively to PaymentCompletionOrchestrator.
         */

        return Map(attempt);
    }

    private static void ValidateIdentity(
        string tenantId,
        string userId,
        string idempotencyKey)
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

        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
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