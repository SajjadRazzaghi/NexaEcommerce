﻿using Microsoft.AspNetCore.WebUtilities;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Orders;

public sealed class ZarinPalCallbackEndpoints
    : IFeatureEndpoints
{
    public void Map(
        IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/orders/payment/zarinpal/callback",
                HandleCallback)
            .WithTags("Payments")
            .AllowAnonymous();
    }

    private static async Task<IResult>
        HandleCallback(
            HttpContext http,
            ICurrentTenant tenant,
            IPaymentAttemptRepository paymentAttemptRepository,
            IOrderRepository orderRepository,
            PaymentCompletionOrchestrator completion,
            PaymentFailureOrchestrator failure,
            IConfiguration configuration,
            ILogger<ZarinPalCallbackEndpoints> logger,
            CancellationToken cancellationToken)
    {
        /*
         * Payment callbacks contain sensitive business state.
         * Do not allow intermediary/browser caches to keep them.
         */
        http.Response.Headers.CacheControl =
            "no-store, no-cache, must-revalidate";

        http.Response.Headers.Pragma =
            "no-cache";

        var orderIdValue =
            http.Request.Query["orderId"]
                .FirstOrDefault();

        var authority =
            http.Request.Query["Authority"]
                .FirstOrDefault();

        var status =
            http.Request.Query["Status"]
                .FirstOrDefault();

        if (!Guid.TryParse(
                orderIdValue,
                out var orderId) ||
            orderId == Guid.Empty)
        {
            return RedirectToPaymentResult(
                configuration,
                Guid.Empty,
                false,
                "invalid_order");
        }

        /*
         * Nothing financial is trusted from the callback itself.
         *
         * User id, order amount, currency and payment attempt
         * are resolved from our own database.
         */
        var order =
            await orderRepository.GetByIdAsync(
                tenant.Id,
                orderId,
                null,
                cancellationToken);

        if (order is null)
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "order_not_found");
        }

        var paymentAttempt =
            await paymentAttemptRepository.GetByOrderIdAsync(
                tenant.Id,
                orderId,
                cancellationToken);

        if (paymentAttempt is null)
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "payment_attempt_not_found");
        }

        /*
         * Callback can be delivered more than once.
         *
         * A completed attempt + Paid order is terminal and idempotent.
         * No inventory/payment side effects are repeated.
         */
        if (paymentAttempt.Status ==
                PaymentAttemptStatus.Succeeded &&
            order.Status ==
                OrderStatus.Paid)
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                true,
                null);
        }

        /*
         * This endpoint is exclusively for ZarinPal callbacks.
         */
        if (!string.Equals(
                paymentAttempt.GatewayName,
                "ZarinPal",
                StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "gateway_mismatch");
        }

        /*
         * Any non-OK result from ZarinPal is treated as a
         * failed/cancelled payment attempt.
         *
         * The actual reservation release is performed server-side.
         */
        if (!string.Equals(
                status,
                "OK",
                StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await failure.FailAsync(
                    tenant.Id,
                    order.UserId,
                    paymentAttempt.Id,
                    "ZARINPAL_CANCELLED",
                    "Payment returned from ZarinPal without a successful status.",
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                /*
                 * Duplicate callbacks can reach this branch after
                 * another request already failed/completed the attempt.
                 *
                 * This is expected and must not leak the internal
                 * exception back to the customer.
                 */
                logger.LogDebug(
                    ex,
                    "Ignoring non-terminal ZarinPal failure handling conflict for payment attempt {PaymentAttemptId}.",
                    paymentAttempt.Id);
            }

            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "cancelled");
        }

        if (string.IsNullOrWhiteSpace(
                authority))
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "missing_authority");
        }

        var normalizedAuthority =
            authority.Trim();

        /*
         * If an Authority is already stored for this attempt,
         * it must exactly match the callback value.
         */
        if (!string.IsNullOrWhiteSpace(
                paymentAttempt.GatewayReference) &&
            !string.Equals(
                paymentAttempt.GatewayReference.Trim(),
                normalizedAuthority,
                StringComparison.Ordinal))
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "authority_mismatch");
        }

        try
        {
            /*
             * CompleteAsync performs the real gateway verification
             * using the amount persisted in PaymentAttempt/Order.
             *
             * It also commits inventory and transitions the order
             * to Paid only after successful verification.
             */
            await completion.CompleteAsync(
                tenant.Id,
                order.UserId,
                paymentAttempt.Id,
                "ZarinPal",
                normalizedAuthority,
                cancellationToken);

            return RedirectToPaymentResult(
                configuration,
                orderId,
                true,
                null);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(
                ex,
                "ZarinPal callback payment attempt was not found during completion. PaymentAttemptId={PaymentAttemptId}, OrderId={OrderId}.",
                paymentAttempt.Id,
                orderId);

            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "payment_not_found");
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(
                ex,
                "Invalid ZarinPal callback completion request. PaymentAttemptId={PaymentAttemptId}, OrderId={OrderId}.",
                paymentAttempt.Id,
                orderId);

            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "invalid_payment");
        }
        catch (InvalidOperationException ex)
        {
            /*
             * Do not expose internal gateway/inventory/order messages
             * to the customer through the redirect URL.
             *
             * Keep the detailed diagnostic information in server logs.
             */
            logger.LogError(
                ex,
                "ZarinPal payment completion failed. PaymentAttemptId={PaymentAttemptId}, OrderId={OrderId}.",
                paymentAttempt.Id,
                orderId);

            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "verification_failed");
        }
    }

    private static IResult
        RedirectToPaymentResult(
            IConfiguration configuration,
            Guid orderId,
            bool success,
            string? reason)
    {
        var clientUrl =
            configuration["App:ClientUrl"];

        if (string.IsNullOrWhiteSpace(
                clientUrl))
        {
            clientUrl =
                "https://localhost:3000";
        }

        if (!Uri.TryCreate(
                clientUrl.Trim(),
                UriKind.Absolute,
                out var clientUri) ||
            (clientUri.Scheme != Uri.UriSchemeHttp &&
             clientUri.Scheme != Uri.UriSchemeHttps))
        {
            clientUrl =
                "https://localhost:3000";
        }
        else
        {
            clientUrl =
                clientUri
                    .GetLeftPart(
                        UriPartial.Authority)
                    .TrimEnd('/');
        }

        if (orderId == Guid.Empty)
        {
            return Results.Redirect(
                $"{clientUrl}/checkout");
        }

        var target =
            $"{clientUrl}/orders/payment/{orderId}";

        var query =
            new Dictionary<string, string?>
            {
                ["payment"] =
                    success
                        ? "success"
                        : "failed"
            };

        if (!success &&
            !string.IsNullOrWhiteSpace(
                reason))
        {
            query["reason"] =
                reason;
        }

        target =
            QueryHelpers.AddQueryString(
                target,
                query);

        return Results.Redirect(target);
    }
}
