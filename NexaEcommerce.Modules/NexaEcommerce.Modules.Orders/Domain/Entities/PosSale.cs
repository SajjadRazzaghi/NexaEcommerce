namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public enum PosSaleStatus
{
    PendingPayment = 1,
    Paid = 2,
    Cancelled = 3,
    ReconciliationRequired = 4,
    PaymentInProgress = 5
}

public enum PosPaymentMethod
{
    Cash = 1,
    BankMellat = 2,
    ManualCard = 3
}

public sealed class PosSale
{
    public Guid Id { get; private set; }

    public string TenantId { get; private set; } = string.Empty;
    public string SellerUserId { get; private set; } = string.Empty;

    public string SaleNumber { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;

    public PosSaleStatus Status { get; private set; }
    public PosPaymentMethod PaymentMethod { get; private set; }

    public string Currency { get; private set; } = "IRR";

    public decimal Subtotal { get; private set; }
    public decimal TotalAmount { get; private set; }

    public string? PaymentReference { get; private set; }
    public string? PaymentGatewayResponse { get; private set; }
    public Guid? BridgeDeviceId { get; private set; }
    public DateTime? PaymentStartedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    public ICollection<PosSaleItem> Items { get; private set; }
        = new List<PosSaleItem>();

    private PosSale() { }

    public PosSale(
        string tenantId,
        string sellerUserId,
        string idempotencyKey,
        PosPaymentMethod paymentMethod,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(sellerUserId))
            throw new ArgumentException("Seller is required.", nameof(sellerUserId));
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        SellerUserId = sellerUserId;
        IdempotencyKey = idempotencyKey.Trim();
        PaymentMethod = paymentMethod;
        Currency = currency;
        Status = PosSaleStatus.PendingPayment;
        CreatedAtUtc = DateTime.UtcNow;
        SaleNumber = $"POS-{CreatedAtUtc:yyyyMMdd}-{Id.ToString("N")[..10].ToUpperInvariant()}";
    }

    public void AddItem(PosSaleItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Status != PosSaleStatus.PendingPayment)
            throw new InvalidOperationException("Items cannot be added after payment processing.");
        Items.Add(item);
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        Subtotal = Items.Sum(x => x.LineTotal);
        TotalAmount = Subtotal;
    }

    // Cash path: used only after server-side stock deduction has succeeded.
    public void MarkPaid(string? paymentReference = null)
    {
        if (Status == PosSaleStatus.Paid) return;
        if (Status != PosSaleStatus.PendingPayment)
            throw new InvalidOperationException("Only a pending sale can be marked as paid.");

        Status = PosSaleStatus.Paid;
        PaymentReference = NormalizeReference(paymentReference);
        PaidAtUtc = DateTime.UtcNow;
    }

    public void MarkPaymentInProgress(Guid bridgeDeviceId)
    {
        if (bridgeDeviceId == Guid.Empty)
            throw new ArgumentException("Bridge device id is required.", nameof(bridgeDeviceId));
        if (PaymentMethod != PosPaymentMethod.BankMellat || Status != PosSaleStatus.PendingPayment)
            throw new InvalidOperationException("Only pending bank-card sales can be claimed by a Bridge.");

        BridgeDeviceId = bridgeDeviceId;
        PaymentStartedAtUtc = DateTime.UtcNow;
        Status = PosSaleStatus.PaymentInProgress;
    }

    public void MarkGatewayPaid(string? paymentReference, string? safeResponse)
    {
        if (Status == PosSaleStatus.Paid) return;
        if (Status != PosSaleStatus.PaymentInProgress)
            throw new InvalidOperationException("Only a payment-in-progress sale can be completed as paid.");

        Status = PosSaleStatus.Paid;
        PaymentReference = NormalizeReference(paymentReference);
        PaymentGatewayResponse = SafeResponse(safeResponse);
        PaidAtUtc = DateTime.UtcNow;
    }

    public void MarkGatewayDeclined(string? safeResponse)
    {
        if (Status != PosSaleStatus.PaymentInProgress)
            throw new InvalidOperationException("Only a payment-in-progress sale can be declined.");

        Status = PosSaleStatus.Cancelled;
        PaymentGatewayResponse = SafeResponse(safeResponse);
    }

    public void MarkCancelled()
    {
        if (Status != PosSaleStatus.PendingPayment)
            throw new InvalidOperationException("Only a pending sale can be cancelled.");
        Status = PosSaleStatus.Cancelled;
    }

    public void MarkReconciliationRequired(string? reason = null)
    {
        if (Status == PosSaleStatus.Paid || Status == PosSaleStatus.Cancelled)
            throw new InvalidOperationException("A completed sale cannot require reconciliation.");
        Status = PosSaleStatus.ReconciliationRequired;
        PaymentGatewayResponse = SafeResponse(reason);
    }

    public void MarkReconciledPaid(string? paymentReference, string? reason)
    {
        if (Status != PosSaleStatus.ReconciliationRequired)
            throw new InvalidOperationException("Only a sale requiring reconciliation can be reconciled as paid.");
        Status = PosSaleStatus.Paid;
        PaymentReference = NormalizeReference(paymentReference);
        PaymentGatewayResponse = SafeResponse(reason);
        PaidAtUtc = DateTime.UtcNow;
    }

    public void MarkReconciledDeclined(string? reason)
    {
        if (Status != PosSaleStatus.ReconciliationRequired)
            throw new InvalidOperationException("Only a sale requiring reconciliation can be reconciled as declined.");
        Status = PosSaleStatus.Cancelled;
        PaymentGatewayResponse = SafeResponse(reason);
    }

    private static string? NormalizeReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var clean = new string(value.Trim().Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        if (clean.Length == 0 || clean.Length > 64 || (clean.Length >= 13 && clean.All(char.IsDigit))) return null;
        return clean;
    }

    private static string? SafeResponse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var safe = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return safe.Length <= 1000 ? safe : safe[..1000];
    }
}
