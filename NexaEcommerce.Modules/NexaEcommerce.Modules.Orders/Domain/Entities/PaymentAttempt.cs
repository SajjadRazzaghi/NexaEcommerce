namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class PaymentAttempt
{
    private PaymentAttempt()
    {
    }

private PaymentAttempt(
    Guid orderId,
    string tenantId,
    string userId,
    string idempotencyKey,
    decimal amount,
    string currency)
    {
        Id = Guid.NewGuid();

        OrderId = orderId;
        TenantId = tenantId;
        UserId = userId;
        IdempotencyKey = idempotencyKey;
        Amount = amount;
        Currency = currency;

        Status =
            PaymentAttemptStatus.Pending;

        CreatedAt =
            DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string TenantId { get; private set; } = null!;

    public string UserId { get; private set; } = null!;

    public string IdempotencyKey { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    public PaymentAttemptStatus Status { get; private set; }

    public string? GatewayName { get; private set; }

    public string? GatewayReference { get; private set; }

    public string? FailureCode { get; private set; }

    public string? FailureMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static PaymentAttempt Create(
        Guid orderId,
        string tenantId,
        string userId,
        string idempotencyKey,
        decimal amount,
        string currency)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                nameof(orderId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                nameof(idempotencyKey));
        }

        if (idempotencyKey.Trim().Length > 128)
        {
            throw new ArgumentException(
                "Payment idempotency key cannot exceed 128 characters.",
                nameof(idempotencyKey));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                nameof(currency));
        }

        return new PaymentAttempt(
            orderId,
            tenantId.Trim(),
            userId.Trim(),
            idempotencyKey.Trim(),
            amount,
            currency.Trim());
    }

    public void MarkGatewayCreated(
        string gatewayName,
        string gatewayReference)
    {
        if (Status !=
            PaymentAttemptStatus.Pending)
        {
            if (Status ==
                PaymentAttemptStatus.Succeeded)
            {
                return;
            }

            throw new InvalidOperationException(
                "Only pending payment attempts can be initialized.");
        }

        if (string.IsNullOrWhiteSpace(gatewayName))
        {
            throw new ArgumentException(
                nameof(gatewayName));
        }

        if (string.IsNullOrWhiteSpace(gatewayReference))
        {
            throw new ArgumentException(
                nameof(gatewayReference));
        }

        GatewayName =
            gatewayName.Trim();

        GatewayReference =
            gatewayReference.Trim();

        FailureCode = null;
        FailureMessage = null;
    }

    public void MarkSucceeded(
        string gatewayName,
        string gatewayReference)
    {
        if (Status ==
            PaymentAttemptStatus.Succeeded)
        {
            return;
        }

        if (Status !=
            PaymentAttemptStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending payment attempts can succeed.");
        }

        if (string.IsNullOrWhiteSpace(gatewayName))
        {
            throw new ArgumentException(
                nameof(gatewayName));
        }

        if (string.IsNullOrWhiteSpace(gatewayReference))
        {
            throw new ArgumentException(
                nameof(gatewayReference));
        }

        GatewayName =
            gatewayName.Trim();

        GatewayReference =
            gatewayReference.Trim();

        FailureCode = null;
        FailureMessage = null;

        Status =
            PaymentAttemptStatus.Succeeded;

        CompletedAt =
            DateTimeOffset.UtcNow;
    }

    public void MarkFailed(
        string? failureCode,
        string? failureMessage)
    {
        if (Status ==
            PaymentAttemptStatus.Failed)
        {
            return;
        }

        if (Status ==
            PaymentAttemptStatus.Succeeded)
        {
            throw new InvalidOperationException(
                "A successful payment attempt cannot be marked as failed.");
        }

        FailureCode =
            string.IsNullOrWhiteSpace(failureCode)
                ? null
                : failureCode.Trim();

        FailureMessage =
            string.IsNullOrWhiteSpace(failureMessage)
                ? null
                : failureMessage.Trim();

        Status =
            PaymentAttemptStatus.Failed;

        CompletedAt =
            DateTimeOffset.UtcNow;
    }


}

public enum PaymentAttemptStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3
}
