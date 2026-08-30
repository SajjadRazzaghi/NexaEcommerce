namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record CreatePaymentAttemptRequest(
    Guid OrderId);

public sealed record PaymentAttemptDto(
    Guid Id,
    Guid OrderId,
    string Status,
    decimal Amount,
    string Currency,
    string? GatewayName,
    string? GatewayReference,
    string? FailureCode,
    string? FailureMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);