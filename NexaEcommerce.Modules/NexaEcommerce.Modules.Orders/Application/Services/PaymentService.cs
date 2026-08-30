using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Payments;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Payments;

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

        if (order.Status !=
            OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "Only orders pending payment can start payment.");
        }

        var paymentAttempt =
            await paymentAttempts
                .GetByIdempotencyKeyAsync(
                    tenantId,
                    userId,
                    idempotencyKey,
                    cancellationToken);

        if (paymentAttempt is null)
        {
            var created =
                await paymentAttemptService.CreateAsync(
                    tenantId,
                    userId,
                    orderId,
                    idempotencyKey,
                    cancellationToken);

            paymentAttempt =
                await paymentAttempts.GetByIdAsync(
                    tenantId,
                    userId,
                    created.Id,
                    cancellationToken);

            if (paymentAttempt is null)
            {
                throw new InvalidOperationException(
                    "Payment attempt could not be loaded after creation.");
            }
        }

        var gateway =
            gateways.Get(gatewayName);

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

        /*
         * The current PaymentAttempt aggregate does not expose a separate
         * "Gateway initiated" state. The gateway reference is therefore
         * retained only after successful verification in this iteration.
         */
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

        if (attempt.Status ==
            PaymentAttemptStatus.Failed)
        {
            throw new InvalidOperationException(
                "A failed payment attempt cannot be verified.");
        }

        var gatewayName =
            attempt.GatewayName ??
            TestPaymentGateway.GatewayName;

        var gateway =
            gateways.Get(gatewayName);

        var result =
            await gateway.VerifyAsync(
                new PaymentGatewayVerifyRequest(
                    attempt.OrderId.ToString("N"),
                    attempt.Amount,
                    gatewayReference),
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

        /*
         * Completion/Inventory commit remains behind the dedicated
         * PaymentCompletionOrchestrator. This keeps gateway verification
         * separate from fulfillment.
         */
        return await paymentAttemptService.MarkSucceededAsync(
            tenantId,
            userId,
            paymentAttemptId,
            gateway.Name,
            result.GatewayReference ??
                gatewayReference,
            cancellationToken);
    }
}