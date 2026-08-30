namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record CreatePaymentResultDto(
    Guid PaymentAttemptId,
    Guid OrderId,
    string GatewayName,
    string Status,
    decimal Amount,
    string Currency,
    string? PaymentUrl,
    string? GatewayReference);