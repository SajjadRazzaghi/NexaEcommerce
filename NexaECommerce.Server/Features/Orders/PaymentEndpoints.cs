using Microsoft.AspNetCore.Mvc;
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

        group.MapGet(
            "/payment-attempts/{id:guid}",
            GetPaymentAttempt);

        group.MapPost(
            "/payment/complete",
            CompletePayment);
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

        try
        {
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
        catch (ArgumentException)
        {
            return Results.NotFound();
        }
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

    private static string? GetUserId(
        HttpContext http)
    {
        var value =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return string.IsNullOrWhiteSpace(value)
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

public sealed record CompletePaymentRequest(
Guid PaymentAttemptId,
string GatewayName,
string GatewayReference);
