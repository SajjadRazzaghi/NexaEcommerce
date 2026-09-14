using Microsoft.AspNetCore.WebUtilities;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Orders;

public sealed class ZarinPalCallbackEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/orders/payment/zarinpal/callback",
                HandleCallback)
            .WithTags("Payments")
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleCallback(
        HttpContext http,
        ICurrentTenant tenant,
        IPaymentAttemptRepository paymentAttemptRepository,
        IOrderRepository orderRepository,
        PaymentCompletionOrchestrator completion,
        PaymentFailureOrchestrator failure,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var orderIdValue =
            http.Request.Query["orderId"].FirstOrDefault();

        var authority =
            http.Request.Query["Authority"].FirstOrDefault();

        var status =
            http.Request.Query["Status"].FirstOrDefault();

        if (!Guid.TryParse(orderIdValue, out var orderId) ||
            orderId == Guid.Empty)
        {
            return RedirectToPaymentResult(
                configuration,
                Guid.Empty,
                false,
                "شناسه سفارش نامعتبر است.");
        }

        /*
         * ZarinPal بعد از بازگشت به Callback معمولاً Authority
         * و Status را به صورت QueryString ارسال می‌کند.
         *
         * در اینجا هیچ مبلغ، UserId یا PaymentAttemptId را
         * از QueryString اعتماد نمی‌کنیم؛ این اطلاعات از دیتابیس
         * خوانده می‌شوند.
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
                "سفارش پیدا نشد.");
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
                "تلاش پرداخت برای سفارش پیدا نشد.");
        }

        /*
         * Callback ممکن است چند بار دریافت شود.
         * اگر پرداخت قبلاً کامل شده، دوباره هیچ عملیات مالی انجام نمی‌دهیم.
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
         * فقط Callback مربوط به ZarinPal باید این مسیر را تکمیل کند.
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
                "درگاه پرداخت سفارش با ZarinPal مطابقت ندارد.");
        }

        /*
         * اگر کاربر در درگاه پرداخت را لغو کرده باشد،
         * پرداخت را Failed می‌کنیم و Reservationها را آزاد می‌کنیم.
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
                    string.IsNullOrWhiteSpace(status)
                        ? "پرداخت در زرین‌پال لغو شد."
                        : $"پرداخت در زرین‌پال با وضعیت '{status}' بازگشت داده شد.",
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                /*
                 * اگر PaymentAttempt قبلاً Failed/Completed شده باشد،
                 * Callback مجدد نباید باعث خطای بی‌مورد برای کاربر شود.
                 */
            }

            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "پرداخت لغو یا ناموفق بود.");
        }

        if (string.IsNullOrWhiteSpace(authority))
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                "Authority از زرین‌پال دریافت نشد.");
        }

        var normalizedAuthority =
            authority.Trim();

        /*
         * اگر Authority از قبل در PaymentAttempt ذخیره شده،
         * باید دقیقاً همان باشد.
         *
         * این بررسی جلوی استفاده از Authority مربوط به
         * یک پرداخت دیگر برای این سفارش را می‌گیرد.
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
                "شناسه پرداخت زرین‌پال با پرداخت سفارش مطابقت ندارد.");
        }

        try
        {
            /*
             * CompleteAsync خودش Verify واقعی درگاه را انجام می‌دهد.
             * مبلغ نیز از PaymentAttempt دیتابیس خوانده می‌شود، نه از Callback.
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
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                ex.Message);
        }
        catch (ArgumentException ex)
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectToPaymentResult(
                configuration,
                orderId,
                false,
                ex.Message);
        }
    }

    private static IResult RedirectToPaymentResult(
        IConfiguration configuration,
        Guid orderId,
        bool success,
        string? reason)
    {
        var clientUrl =
            configuration["App:ClientUrl"];

        if (string.IsNullOrWhiteSpace(clientUrl))
        {
            clientUrl = "https://localhost:3000";
        }

        clientUrl =
            clientUrl.Trim().TrimEnd('/');

        if (orderId == Guid.Empty)
        {
            var fallback =
                $"{clientUrl}/checkout";

            return Results.Redirect(fallback);
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
            !string.IsNullOrWhiteSpace(reason))
        {
            query["reason"] = reason;
        }

        target =
            QueryHelpers.AddQueryString(
                target,
                query);

        return Results.Redirect(target);
    }
}