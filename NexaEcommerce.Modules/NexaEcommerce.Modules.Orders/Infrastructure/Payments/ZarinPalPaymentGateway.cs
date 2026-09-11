using Microsoft.Extensions.Configuration;
using NexaEcommerce.Modules.Orders.Application.Payments;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Payments;

public sealed class ZarinPalPaymentGateway(
    HttpClient httpClient,
    IConfiguration configuration)
    : IPaymentGateway
{
    public const string GatewayName = "ZarinPal";

    public string Name =>
        GatewayName;

    public async Task<PaymentGatewayCreateResult> CreateAsync(
        PaymentGatewayCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var merchantId =
            GetMerchantId();

        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return new PaymentGatewayCreateResult(
                false,
                null,
                null,
                "ZARINPAL_MERCHANT_NOT_CONFIGURED",
                "ZarinPal MerchantId is not configured.");
        }

        if (request.Amount <= 0)
        {
            return new PaymentGatewayCreateResult(
                false,
                null,
                null,
                "INVALID_AMOUNT",
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            return new PaymentGatewayCreateResult(
                false,
                null,
                null,
                "INVALID_CALLBACK_URL",
                "Payment callback URL is required.");
        }

        var amount =
            ConvertToRial(request.Amount);

        if (amount <= 0)
        {
            return new PaymentGatewayCreateResult(
                false,
                null,
                null,
                "INVALID_AMOUNT",
                "Payment amount must be greater than zero.");
        }

        var endpoint =
            GetApiBaseUrl() +
            "/pg/v4/payment/request.json";

        var payload =
            new
            {
                merchant_id = merchantId,
                amount,
                callback_url = request.CallbackUrl.Trim(),
                description =
                    $"NexaECommerce order {request.MerchantOrderId}",
            };

        try
        {
            using var response =
                await httpClient.PostAsJsonAsync(
                    endpoint,
                    payload,
                    cancellationToken);

            var body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            ZarinPalResponse? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<ZarinPalResponse>(
                        body,
                        JsonSerializerOptions.Web);
            }
            catch (JsonException)
            {
                return new PaymentGatewayCreateResult(
                    false,
                    null,
                    null,
                    "INVALID_ZARINPAL_RESPONSE",
                    "ZarinPal returned an invalid response.");
            }

            if (result?.Data is null)
            {
                return new PaymentGatewayCreateResult(
                    false,
                    null,
                    null,
                    GetErrorCode(result),
                    GetErrorMessage(
                        result,
                        response.ReasonPhrase ??
                        "ZarinPal payment request failed."));
            }

            if (result.Data.Code != 100)
            {
                return new PaymentGatewayCreateResult(
                    false,
                    null,
                    null,
                    result.Data.Code.ToString(),
                    result.Data.Message ??
                    "ZarinPal payment request failed.");
            }

            if (string.IsNullOrWhiteSpace(
                    result.Data.Authority))
            {
                return new PaymentGatewayCreateResult(
                    false,
                    null,
                    null,
                    "MISSING_AUTHORITY",
                    "ZarinPal did not return an authority.");
            }

            var authority =
                result.Data.Authority.Trim();

            var paymentUrl =
                GetPaymentBaseUrl() +
                authority;

            return new PaymentGatewayCreateResult(
                true,
                paymentUrl,
                authority,
                null,
                null);
        }
        catch (HttpRequestException ex)
        {
            return new PaymentGatewayCreateResult(
                false,
                null,
                null,
                "ZARINPAL_HTTP_ERROR",
                ex.Message);
        }
        catch (TaskCanceledException) when (
            !cancellationToken.IsCancellationRequested)
        {
            return new PaymentGatewayCreateResult(
                false,
                null,
                null,
                "ZARINPAL_TIMEOUT",
                "Connection to ZarinPal timed out.");
        }
    }

    public async Task<PaymentGatewayVerifyResult> VerifyAsync(
        PaymentGatewayVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var merchantId =
            GetMerchantId();

        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return new PaymentGatewayVerifyResult(
                false,
                null,
                "ZARINPAL_MERCHANT_NOT_CONFIGURED",
                "ZarinPal MerchantId is not configured.");
        }

        if (request.Amount <= 0)
        {
            return new PaymentGatewayVerifyResult(
                false,
                null,
                "INVALID_AMOUNT",
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(
                request.GatewayReference))
        {
            return new PaymentGatewayVerifyResult(
                false,
                null,
                "MISSING_AUTHORITY",
                "ZarinPal authority is required.");
        }

        var authority =
            request.GatewayReference.Trim();

        var amount =
            ConvertToRial(request.Amount);

        if (amount <= 0)
        {
            return new PaymentGatewayVerifyResult(
                false,
                null,
                "INVALID_AMOUNT",
                "Payment amount must be greater than zero.");
        }

        var endpoint =
            GetApiBaseUrl() +
            "/pg/v4/payment/verify.json";

        var payload =
            new
            {
                merchant_id = merchantId,
                amount,
                authority,
            };

        try
        {
            using var response =
                await httpClient.PostAsJsonAsync(
                    endpoint,
                    payload,
                    cancellationToken);

            var body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            ZarinPalResponse? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<ZarinPalResponse>(
                        body,
                        JsonSerializerOptions.Web);
            }
            catch (JsonException)
            {
                return new PaymentGatewayVerifyResult(
                    false,
                    null,
                    "INVALID_ZARINPAL_RESPONSE",
                    "ZarinPal returned an invalid verification response.");
            }

            if (result?.Data is null)
            {
                return new PaymentGatewayVerifyResult(
                    false,
                    null,
                    GetErrorCode(result),
                    GetErrorMessage(
                        result,
                        response.ReasonPhrase ??
                        "ZarinPal payment verification failed."));
            }

            if (result.Data.Code != 100 &&
                result.Data.Code != 101)
            {
                return new PaymentGatewayVerifyResult(
                    false,
                    null,
                    result.Data.Code.ToString(),
                    result.Data.Message ??
                    "ZarinPal payment verification failed.");
            }

            /*
             * GatewayReference stays equal to Authority.
             * RefId is currently not part of the generic PaymentGateway
             * contract and therefore is intentionally not used as the
             * reference here.
             */
            return new PaymentGatewayVerifyResult(
                true,
                authority,
                null,
                null);
        }
        catch (HttpRequestException ex)
        {
            return new PaymentGatewayVerifyResult(
                false,
                null,
                "ZARINPAL_HTTP_ERROR",
                ex.Message);
        }
        catch (TaskCanceledException) when (
            !cancellationToken.IsCancellationRequested)
        {
            return new PaymentGatewayVerifyResult(
                false,
                null,
                "ZARINPAL_TIMEOUT",
                "Connection to ZarinPal timed out.");
        }
    }

    private string GetMerchantId()
    {
        return configuration[
                   "ZarinPal:MerchantId"]?
               .Trim()
               ?? string.Empty;
    }

    private bool IsSandbox()
    {
        var value =
            configuration["ZarinPal:Sandbox"];

        return !bool.TryParse(
            value,
            out var sandbox)
            || sandbox;
    }

    private string GetApiBaseUrl()
    {
        return IsSandbox()
            ? "https://sandbox.zarinpal.com"
            : "https://api.zarinpal.com";
    }

    private string GetPaymentBaseUrl()
    {
        return IsSandbox()
            ? "https://sandbox.zarinpal.com/pg/StartPay/"
            : "https://www.zarinpal.com/pg/StartPay/";
    }

    private static long ConvertToRial(
        decimal amount)
    {
        if (amount <= 0m)
        {
            return 0;
        }

        return checked(
            (long)Math.Round(
                amount,
                0,
                MidpointRounding.AwayFromZero));
    }

    private static string GetErrorCode(
        ZarinPalResponse? response)
    {
        if (response?.Errors is not
            {
                ValueKind: JsonValueKind.Object
            })
        {
            return "ZARINPAL_ERROR";
        }

        if (response.Errors.TryGetProperty(
                "code",
                out var code))
        {
            return code.ToString();
        }

        return "ZARINPAL_ERROR";
    }

    private static string GetErrorMessage(
        ZarinPalResponse? response,
        string fallback)
    {
        if (response?.Errors is
            {
                ValueKind: JsonValueKind.Object
            } &&
            response.Errors.TryGetProperty(
                "message",
                out var message))
        {
            var value =
                message.GetString();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return fallback;
    }

    private sealed class ZarinPalResponse
    {
        [JsonPropertyName("data")]
        public ZarinPalData? Data { get; init; }

        [JsonPropertyName("errors")]
        public JsonElement Errors { get; init; }
    }

    private sealed class ZarinPalData
    {
        [JsonPropertyName("code")]
        public int Code { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("authority")]
        public string? Authority { get; init; }

        [JsonPropertyName("ref_id")]
        public long? RefId { get; init; }
    }
}