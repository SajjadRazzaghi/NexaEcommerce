using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentRetryOrchestrator(
    PaymentReservationOrchestrator paymentReservations,
    IPaymentAttemptService paymentAttempts)
{
    public async Task<PaymentAttemptDto> RetryAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(
            tenantId,
            userId,
            orderId,
            idempotencyKey);

        var normalizedTenantId =
            tenantId.Trim();

        var normalizedUserId =
            userId.Trim();

        var normalizedIdempotencyKey =
            idempotencyKey.Trim();

        /*
         * Reservation management is centralized here.
         *
         * We do NOT create a second copy of the reservation logic.
         */
        await paymentReservations.EnsureForStartAsync(
            normalizedTenantId,
            normalizedUserId,
            orderId,
            normalizedIdempotencyKey,
            cancellationToken);

        /*
         * PaymentAttempt creation remains idempotent.
         */
        return await paymentAttempts.CreateAsync(
            normalizedTenantId,
            normalizedUserId,
            orderId,
            normalizedIdempotencyKey,
            cancellationToken);
    }

    private static void ValidateInput(
        string tenantId,
        string userId,
        Guid orderId,
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

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));
        }

        if (idempotencyKey.Trim().Length > 128)
        {
            throw new ArgumentException(
                "Idempotency key cannot exceed 128 characters.",
                nameof(idempotencyKey));
        }
    }
}