﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using System.Security.Claims;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentEndpoints
    : IFeatureEndpoints
{
    private const string PaymentIdempotencyHeader =
        "Idempotency-Key";

    private const string ZarinPalCallbackPath =
        "/api/orders/payment/zarinpal/callback";

    public void Map(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/orders")
                .WithTags("Payments")
                .RequireAuthorization();

        group.MapPost(
            "/payment-attempts",
            CreatePaymentAttempt);

        group.MapPost(
            "/payment/start",
            StartPayment);

        group.MapGet(
            "/payment-attempts/{id:guid}",
            GetPaymentAttempt);

        group.MapPost(
            "/payment/verify",
            VerifyPayment);

        group.MapPost(
            "/payment/complete",
            CompletePayment);

        group.MapPost(
            "/payment/fail",
            FailPayment);

        group.MapPost(
            "/payment/retry",
            RetryPayment);
    }

    private static async Task<IResult>
        CreatePaymentAttempt(
            [FromBody]
            CreatePaymentAttemptRequest request,

            [FromServices]
            IPaymentAttemptService paymentAttempts,

            [FromServices]
            ICurrentTenant tenant,

            HttpContext http,

            CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var idempotencyKey =
            GetIdempotencyKey(http);

        if (idempotencyKey is null)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{PaymentIdempotencyHeader} header is required."
                });
        }

        if (idempotencyKey.Length > 128)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{PaymentIdempotencyHeader} cannot exceed 128 characters."
                });
        }

        if (request.OrderId == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Order id is required."
                });
        }

        try
        {
            var result =
                await paymentAttempts.CreateAsync(
                    tenant.Id,
                    userId,
                    request.OrderId,
                    idempotencyKey,
                    ct);

            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult>
        StartPayment(
            [FromBody]
            StartPaymentRequest request,

            [FromServices]
            IPaymentService payments,

            [FromServices]
            ICurrentTenant tenant,

            HttpContext http,

            CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var idempotencyKey =
            GetIdempotencyKey(http);

        if (idempotencyKey is null)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{PaymentIdempotencyHeader} header is required."
                });
        }

        if (idempotencyKey.Length > 128)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{PaymentIdempotencyHeader} cannot exceed 128 characters."
                });
        }

        if (request.OrderId == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Order id is required."
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.GatewayName))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Gateway name is required."
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.CallbackUrl))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Callback URL is required."
                });
        }

        if (!Uri.TryCreate(
                request.CallbackUrl.Trim(),
                UriKind.Absolute,
                out var callbackUri) ||
            (callbackUri.Scheme != Uri.UriSchemeHttp &&
             callbackUri.Scheme != Uri.UriSchemeHttps))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Callback URL must be an absolute HTTP or HTTPS URL."
                });
        }

        /*
         * The callback URL is not trusted merely because it comes
         * from an authenticated client.
         *
         * It must resolve to the server's own ZarinPal callback route
         * and contain exactly this order id.
         *
         * After validation we pass the server-generated expected URL
         * to the payment service instead of passing the client value.
         */
        if (!IsTrustedZarinPalCallbackUrl(
                callbackUri,
                http,
                request.OrderId))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Callback URL is not valid for this payment."
                });
        }

        var trustedCallbackUrl =
            BuildZarinPalCallbackUrl(
                http,
                request.OrderId);

        try
        {
            var result =
                await payments.CreatePaymentAsync(
                    tenant.Id,
                    userId,
                    request.OrderId,
                    idempotencyKey,
                    request.GatewayName.Trim(),
                    trustedCallbackUrl,
                    ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult>
        VerifyPayment(
            [FromBody]
            VerifyPaymentRequest request,

            [FromServices]
            IPaymentService payments,

            [FromServices]
            ICurrentTenant tenant,

            HttpContext http,

            CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (request.PaymentAttemptId == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Payment attempt id is required."
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.GatewayReference))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Gateway reference is required."
                });
        }

        try
        {
            var result =
                await payments.VerifyPaymentAsync(
                    tenant.Id,
                    userId,
                    request.PaymentAttemptId,
                    request.GatewayReference.Trim(),
                    ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult>
        GetPaymentAttempt(
            Guid id,

            [FromServices]
            IPaymentAttemptService paymentAttempts,

            [FromServices]
            ICurrentTenant tenant,

            HttpContext http,

            CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (id == Guid.Empty)
        {
            return Results.NotFound();
        }

        var result =
            await paymentAttempts.GetAsync(
                tenant.Id,
                userId,
                id,
                ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult>
        CompletePayment(
            [FromBody]
            CompletePaymentRequest request,

            [FromServices]
            PaymentCompletionOrchestrator completion,

            [FromServices]
            ICurrentTenant tenant,

            HttpContext http,

            CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (request.PaymentAttemptId == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Payment attempt id is required."
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.GatewayName))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Gateway name is required."
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.GatewayReference))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Gateway reference is required."
                });
        }

        try
        {
            var result =
                await completion.CompleteAsync(
                    tenant.Id,
                    userId,
                    request.PaymentAttemptId,
                    request.GatewayName.Trim(),
                    request.GatewayReference.Trim(),
                    ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult>
        FailPayment(
            [FromBody]
            FailPaymentRequest request,

            [FromServices]
            PaymentFailureOrchestrator failure,

            [FromServices]
            ICurrentTenant tenant,

            HttpContext http,

            CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (request.PaymentAttemptId ==
            Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Payment attempt id is required."
                });
        }

        try
        {
            var result =
                await failure.FailAsync(
                    tenant.Id,
                    userId,
                    request.PaymentAttemptId,
                    request.FailureCode,
                    request.FailureMessage,
                    ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult>
        RetryPayment(
            [FromBody]
            RetryPaymentRequest request,

            [FromServices]
            PaymentRetryOrchestrator retry,

            [FromServices]
            ICurrentTenant tenant,

            HttpContext http,

            CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var idempotencyKey =
            GetIdempotencyKey(http);

        if (idempotencyKey is null)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{PaymentIdempotencyHeader} header is required."
                });
        }

        if (idempotencyKey.Length > 128)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        $"{PaymentIdempotencyHeader} cannot exceed 128 characters."
                });
        }

        if (request.OrderId == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Order id is required."
                });
        }

        try
        {
            var result =
                await retry.RetryAsync(
                    tenant.Id,
                    userId,
                    request.OrderId,
                    idempotencyKey,
                    ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static string BuildZarinPalCallbackUrl(
        HttpContext http,
        Guid orderId)
    {
        var baseUrl =
            $"{http.Request.Scheme}://{http.Request.Host}";

        return QueryHelpers.AddQueryString(
            $"{baseUrl}{ZarinPalCallbackPath}",
            "orderId",
            orderId.ToString("D"));
    }

    private static bool IsTrustedZarinPalCallbackUrl(
        Uri callbackUri,
        HttpContext http,
        Guid orderId)
    {
        if (!string.IsNullOrEmpty(
                callbackUri.UserInfo))
        {
            return false;
        }

        var expectedUriText =
            BuildZarinPalCallbackUrl(
                http,
                orderId);

        if (!Uri.TryCreate(
                expectedUriText,
                UriKind.Absolute,
                out var expectedUri))
        {
            return false;
        }

        if (!string.Equals(
                callbackUri.Scheme,
                expectedUri.Scheme,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(
                callbackUri.Host,
                expectedUri.Host,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (callbackUri.Port !=
            expectedUri.Port)
        {
            return false;
        }

        if (!string.Equals(
                callbackUri.AbsolutePath,
                expectedUri.AbsolutePath,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(
                callbackUri.Query,
                expectedUri.Query,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(
                callbackUri.Fragment))
        {
            return false;
        }

        return true;
    }

    private static string? GetUserId(
        HttpContext http)
    {
        var value =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return string.IsNullOrWhiteSpace(
                value)
            ? null
            : value;
    }

    private static string? GetIdempotencyKey(
        HttpContext http)
    {
        if (!http.Request.Headers.TryGetValue(
                PaymentIdempotencyHeader,
                out var values))
        {
            return null;
        }

        var value =
            values
                .FirstOrDefault()?
                .Trim();

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }
}

public sealed record StartPaymentRequest(
    Guid OrderId,
    string GatewayName,
    string CallbackUrl);

public sealed record VerifyPaymentRequest(
    Guid PaymentAttemptId,
    string GatewayReference);

public sealed record CompletePaymentRequest(
    Guid PaymentAttemptId,
    string GatewayName,
    string GatewayReference);

public sealed record FailPaymentRequest(
    Guid PaymentAttemptId,
    string? FailureCode,
    string? FailureMessage);

public sealed record RetryPaymentRequest(
    Guid OrderId);
