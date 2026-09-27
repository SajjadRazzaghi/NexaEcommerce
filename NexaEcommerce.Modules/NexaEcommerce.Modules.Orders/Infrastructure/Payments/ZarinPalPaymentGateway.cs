using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NexaEcommerce.Modules.Orders.Application.Payments;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Payments;

public sealed class ZarinPalPaymentGateway(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<ZarinPalPaymentGateway> logger)
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

        logger.LogInformation(
            "Starting ZarinPal payment request. MerchantOrderId={MerchantOrderId}, Amount={Amount}, Currency={Currency}, Sandbox={Sandbox}",
            request.MerchantOrderId,
            amount,
            request.Currency,
            IsSandbox());

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

            logger.LogInformation(
                "ZarinPal payment request response. HttpStatus={HttpStatus}, Body={Body}",
                (int)response.StatusCode,
                body);

            ZarinPalResponse? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<ZarinPalResponse>(
                        body,
                        JsonSerializerOptions.Web);
            }
            catch (JsonException ex)
            {
                logger.LogError(
                    ex,
                    "ZarinPal returned an invalid create-payment JSON response.");

                return new PaymentGatewayCreateResult(
                    false,
                    null,
                    null,
                    "INVALID_ZARINPAL_RESPONSE",
                    "ZarinPal returned an invalid response.");
            }

            if (result?.Data is null)
            {
                var errorCode =
                    GetErrorCode(result);

                var errorMessage =
                    GetErrorMessage(
                        result,
                        response.ReasonPhrase ??
                        "ZarinPal payment request failed.");

                logger.LogWarning(
                    "ZarinPal payment request failed. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    errorCode,
                    errorMessage);

                return new PaymentGatewayCreateResult(
                    false,
                    null,
                    null,
                    errorCode,
                    errorMessage);
            }

            if (result.Data.Code != 100)
            {
                logger.LogWarning(
                    "ZarinPal payment request was rejected. Code={Code}, Message={Message}",
                    result.Data.Code,
                    result.Data.Message);

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
                logger.LogError(
                    "ZarinPal payment request succeeded but returned no authority.");

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

            logger.LogInformation(
                "ZarinPal payment created successfully. MerchantOrderId={MerchantOrderId}, Authority={Authority}",
                request.MerchantOrderId,
                MaskReference(authority));

            return new PaymentGatewayCreateResult(
                true,
                paymentUrl,
                authority,
                null,
                null);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "HTTP error while creating ZarinPal payment.");

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
            logger.LogError(
                "ZarinPal payment request timed out.");

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

        logger.LogInformation(
            "Starting ZarinPal verification. MerchantOrderId={MerchantOrderId}, Amount={Amount}, Authority={Authority}, Sandbox={Sandbox}",
            request.MerchantOrderId,
            amount,
            MaskReference(authority),
            IsSandbox());

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

            logger.LogInformation(
                "ZarinPal verification response. MerchantOrderId={MerchantOrderId}, HttpStatus={HttpStatus}, Body={Body}",
                request.MerchantOrderId,
                (int)response.StatusCode,
                body);

            ZarinPalResponse? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<ZarinPalResponse>(
                        body,
                        JsonSerializerOptions.Web);
            }
            catch (JsonException ex)
            {
                logger.LogError(
                    ex,
                    "ZarinPal returned an invalid verification JSON response. MerchantOrderId={MerchantOrderId}",
                    request.MerchantOrderId);

                return new PaymentGatewayVerifyResult(
                    false,
                    null,
                    "INVALID_ZARINPAL_RESPONSE",
                    "ZarinPal returned an invalid verification response.");
            }

            if (result?.Data is null)
            {
                var errorCode =
                    GetErrorCode(result);

                var errorMessage =
                    GetErrorMessage(
                        result,
                        response.ReasonPhrase ??
                        "ZarinPal payment verification failed.");

                logger.LogWarning(
                    "ZarinPal verification failed without data section. MerchantOrderId={MerchantOrderId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    request.MerchantOrderId,
                    errorCode,
                    errorMessage);

                return new PaymentGatewayVerifyResult(
                    false,
                    null,
                    errorCode,
                    errorMessage);
            }

            logger.LogInformation(
                "ZarinPal verification parsed. MerchantOrderId={MerchantOrderId}, Code={Code}, Message={Message}, RefId={RefId}",
                request.MerchantOrderId,
                result.Data.Code,
                result.Data.Message,
                result.Data.RefId);

            /*
             * 100 = successful verification
             * 101 = transaction was already verified
             *
             * Both are valid successful states for our flow.
             */
            if (result.Data.Code != 100 &&
                result.Data.Code != 101)
            {
                var errorCode =
                    result.Data.Code.ToString();

                var errorMessage =
                    result.Data.Message ??
                    "ZarinPal payment verification failed.";

                logger.LogWarning(
                    "ZarinPal verification was rejected. MerchantOrderId={MerchantOrderId}, Code={Code}, Message={Message}, Amount={Amount}, Authority={Authority}",
                    request.MerchantOrderId,
                    result.Data.Code,
                    errorMessage,
                    amount,
                    MaskReference(authority));

                return new PaymentGatewayVerifyResult(
                    false,
                    null,
                    errorCode,
                    errorMessage);
            }

            /*
             * The generic payment contract uses Authority
             * as GatewayReference.
             *
             * RefId is deliberately not substituted here.
             */
            logger.LogInformation(
                "ZarinPal verification succeeded. MerchantOrderId={MerchantOrderId}, Code={Code}, RefId={RefId}, Authority={Authority}",
                request.MerchantOrderId,
                result.Data.Code,
                result.Data.RefId,
                MaskReference(authority));

            return new PaymentGatewayVerifyResult(
                true,
                authority,
                null,
                null);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "HTTP error while verifying ZarinPal payment. MerchantOrderId={MerchantOrderId}, Amount={Amount}, Authority={Authority}",
                request.MerchantOrderId,
                amount,
                MaskReference(authority));

            return new PaymentGatewayVerifyResult(
                false,
                null,
                "ZARINPAL_HTTP_ERROR",
                ex.Message);
        }
        catch (TaskCanceledException) when (
            !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                "ZarinPal verification request timed out. MerchantOrderId={MerchantOrderId}, Authority={Authority}",
                request.MerchantOrderId,
                MaskReference(authority));

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

        /*
         * Fail-safe for development:
         * if the value is missing or invalid, Sandbox is used.
         */
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

    private static string MaskReference(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "***";
        }

        var normalized =
            value.Trim();

        if (normalized.Length <= 8)
        {
            return "***";
        }

        return
            normalized[..4] +
            "..." +
            normalized[^4..];
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