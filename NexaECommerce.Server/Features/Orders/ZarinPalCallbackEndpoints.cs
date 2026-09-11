using Microsoft.AspNetCore.Mvc;
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

    private static async Task<IResult> HandleCallback(
        [FromQuery]
        Guid orderId,

        [FromQuery]
        string? Authority,

        [FromQuery]
        string? Status,

        [FromServices]
        ICurrentTenant tenant,

        [FromServices]
        IPaymentAttemptRepository paymentAttempts,

        [FromServices]
        IPaymentService payments,

        [FromServices]
        PaymentCompletionOrchestrator completion,

        [FromServices]
        PaymentFailureOrchestrator failure,

        [FromServices]
        IConfiguration configuration,

        CancellationToken ct)
    {
        var clientUrl =
            configuration[
                "App:ClientUrl"]?
                .Trim()
                .TrimEnd('/');

        if (orderId == Guid.Empty)
        {
            return RedirectToClient(
                clientUrl,
                "/orders",
                "payment=failed&reason=invalid-order");
        }

        var attempt =
            await paymentAttempts.GetByOrderIdAsync(
                tenant.Id,
                orderId,
                ct);

        if (attempt is null)
        {
            return RedirectToClient(
                clientUrl,
                $"/orders/payment/{orderId}",
                "payment=failed&reason=payment-attempt-not-found");
        }

        var userId =
            attempt.UserId;

        if (!string.Equals(
                Status?.Trim(),
                "OK",
                StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await failure.FailAsync(
                    tenant.Id,
                    userId,
                    attempt.Id,
                    "ZARINPAL_CANCELLED",
                    string.IsNullOrWhiteSpace(Status)
                        ? "Payment was cancelled or rejected by the customer."
                        : $"ZarinPal payment status: {Status.Trim()}",
                    ct);
            }
            catch
            {
                /*
                 * Do not block the customer redirect because a
                 * compensation write failed.
                 */
            }

            return RedirectToClient(
                clientUrl,
                $"/orders/payment/{orderId}",
                "payment=failed&reason=cancelled");
        }

        var authority =
            Authority?.Trim();

        if (string.IsNullOrWhiteSpace(authority))
        {
            return RedirectToClient(
                clientUrl,
                $"/orders/payment/{orderId}",
                "payment=failed&reason=missing-authority");
        }

        try
        {
            var verified =
                await payments.VerifyPaymentAsync(
                    tenant.Id,
                    userId,
                    attempt.Id,
                    authority,
                    ct);

            var gatewayName =
                verified.GatewayName ??
                "ZarinPal";

            var gatewayReference =
                verified.GatewayReference ??
                authority;

            await completion.CompleteAsync(
                tenant.Id,
                userId,
                verified.Id,
                gatewayName,
                gatewayReference,
                ct);

            return RedirectToClient(
                clientUrl,
                $"/orders/{orderId}",
                "payment=success");
        }
        catch (InvalidOperationException ex)
        {
            return RedirectToClient(
                clientUrl,
                $"/orders/payment/{orderId}",
                "payment=failed&reason=" +
                Uri.EscapeDataString(
                    ex.Message));
        }
        catch (KeyNotFoundException)
        {
            return RedirectToClient(
                clientUrl,
                $"/orders/payment/{orderId}",
                "payment=failed&reason=payment-not-found");
        }
    }

    private static IResult RedirectToClient(
        string? clientUrl,
        string path,
        string query)
    {
        if (string.IsNullOrWhiteSpace(clientUrl))
        {
            return Results.Text(
                "Payment processing completed, but App:ClientUrl is not configured.");
        }

        var location =
            $"{clientUrl}{path}?{query}";

        return Results.Redirect(
            location);
    }
}