namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public enum PosSaleStatus
{
    PendingPayment = 1,
    Paid = 2,
    Cancelled = 3,
    ReconciliationRequired = 4
}

public enum PosPaymentMethod
{
    Cash = 1,
    BankMellat = 2
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

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    public ICollection<PosSaleItem> Items { get; private set; }
        = new List<PosSaleItem>();

    private PosSale()
    {
    }

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

        // شناسه کوتاه در شماره فاکتور، احتمال تداخل شماره‌ها را کم می‌کند.
        SaleNumber =
            $"POS-{CreatedAtUtc:yyyyMMdd}-{Id.ToString("N")[..10].ToUpperInvariant()}";
    }

    public void AddItem(PosSaleItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (Status != PosSaleStatus.PendingPayment)
            throw new InvalidOperationException(
                "Items cannot be added after payment processing.");

        Items.Add(item);
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        Subtotal = Items.Sum(x => x.LineTotal);
        TotalAmount = Subtotal;
    }

    public void MarkPaid(string? paymentReference = null)
    {
        if (Status == PosSaleStatus.Paid)
            return;

        if (Status != PosSaleStatus.PendingPayment)
            throw new InvalidOperationException(
                "Only a pending sale can be marked as paid.");

        Status = PosSaleStatus.Paid;
        PaymentReference = paymentReference;
        PaidAtUtc = DateTime.UtcNow;
    }

    public void MarkCancelled()
    {
        if (Status != PosSaleStatus.PendingPayment)
            throw new InvalidOperationException(
                "Only a pending sale can be cancelled.");

        Status = PosSaleStatus.Cancelled;
    }

    public void MarkReconciliationRequired()
    {
        Status = PosSaleStatus.ReconciliationRequired;
    }
}