using System.Globalization;

namespace NexaEcommerce.Modules.Orders.Application.Pos;

/// <summary>
/// POS application orchestration. Product name, SKU, barcode, currency, price and stock
/// are all loaded from the server; values posted by the browser are never trusted.
/// </summary>
public sealed class PosOrchestrator : IPosOrchestrator
{
    private static readonly HashSet<string> SupportedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "Cash", "Card" };

    private readonly IPosCatalogReader _catalog;
    private readonly IPosSaleCommitter _saleCommitter;
    private readonly IPosExecutionContext _context;

    public PosOrchestrator(
        IPosCatalogReader catalog,
        IPosSaleCommitter saleCommitter,
        IPosExecutionContext context)
    {
        _catalog = catalog;
        _saleCommitter = saleCommitter;
        _context = context;
    }

    public async Task<IReadOnlyList<PosProductDto>> SearchProductsAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var normalized = search?.Trim() ?? string.Empty;
        if (normalized.Length < 2)
            throw new PosValidationException("برای جست‌وجوی کالا حداقل دو نویسه وارد کنید.");
        if (normalized.Length > 100)
            throw new PosValidationException("عبارت جست‌وجو نباید بیشتر از ۱۰۰ نویسه باشد.");

        return await _catalog.SearchProductsAsync(normalized, 30, cancellationToken);
    }

    public async Task<PosVariantDto> FindByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken)
    {
        var normalized = barcode?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new PosValidationException("بارکد را وارد کنید.");
        if (normalized.Length > 128)
            throw new PosValidationException("طول بارکد معتبر نیست.");

        var variant = await _catalog.FindSellableVariantByBarcodeAsync(normalized, cancellationToken);
        if (variant is null)
            throw new PosNotFoundException($"کالایی با بارکد «{normalized}» پیدا نشد یا قابل فروش نیست.");
        if (variant.AvailableQuantity < 1)
            throw new PosInsufficientStockException($"موجودی «{variant.ProductName}» کافی نیست.");
        return variant;
    }

    public async Task<PosSaleResultDto> CreateSaleAsync(
        PosCreateSaleRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var tenantId = _context.TenantId?.Trim();
        var sellerUserId = _context.UserId?.Trim();
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(sellerUserId))
            throw new PosConflictException("اطلاعات فروشنده یا فروشگاه از نشست کاربر قابل تشخیص نیست.");

        var idempotencyKey = request.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length is < 16 or > 128)
            throw new PosValidationException("کلید یکتای فروش معتبر نیست؛ صفحه را تازه‌سازی کنید.");

        var paymentMethod = request.PaymentMethod?.Trim();
        if (string.IsNullOrWhiteSpace(paymentMethod) || !SupportedPaymentMethods.Contains(paymentMethod))
            throw new PosValidationException("روش پرداخت فقط می‌تواند نقدی یا کارتخوان باشد؛ فروش اعتباری پشتیبانی نمی‌شود.");
        paymentMethod = char.ToUpperInvariant(paymentMethod[0]) + paymentMethod[1..].ToLowerInvariant();

        var paymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? null
            : request.PaymentReference.Trim();
        if (paymentReference?.Length > 256)
            throw new PosValidationException("شناسه پیگیری پرداخت بیش از حد طولانی است.");
        if (string.Equals(paymentMethod, "Card", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(paymentReference))
        {
            throw new PosValidationException(
                "فروش کارتخوان فقط پس از تأیید پرداخت و ثبت شناسه پیگیری مجاز است.");
        }

        if (request.Items is null || request.Items.Count == 0)
            throw new PosValidationException("سبد فروش خالی است.");
        if (request.Items.Count > 100)
            throw new PosValidationException("در هر فاکتور حداکثر ۱۰۰ ردیف کالا مجاز است.");

        // Check replay before rebuilding the sale. Committer must check again under the
        // database transaction to protect against concurrent duplicate requests.
        var prior = await _saleCommitter.FindByIdempotencyKeyAsync(
            tenantId, sellerUserId, idempotencyKey, cancellationToken);
        if (prior is not null)
            return prior;

        var quantities = new Dictionary<Guid, int>();
        foreach (var item in request.Items)
        {
            if (item.ProductVariantId == Guid.Empty)
                throw new PosValidationException("شناسه تنوع کالا معتبر نیست.");
            if (item.Quantity is < 1 or > 999)
                throw new PosValidationException("تعداد هر کالا باید بین ۱ تا ۹۹۹ باشد.");
            quantities[item.ProductVariantId] = checked(
                quantities.GetValueOrDefault(item.ProductVariantId) + item.Quantity);
            if (quantities[item.ProductVariantId] > 999)
                throw new PosValidationException("تعداد تجمیعی یک تنوع نمی‌تواند بیشتر از ۹۹۹ باشد.");
        }

        var ids = quantities.Keys.ToArray();
        var currentVariants = await _catalog.GetSellableVariantsAsync(ids, cancellationToken);
        var byId = currentVariants.ToDictionary(x => x.Id);

        if (byId.Count != ids.Length || ids.Any(id => !byId.ContainsKey(id)))
            throw new PosConflictException("یک یا چند کالا حذف، غیرفعال یا غیرقابل فروش شده‌اند. سبد را دوباره بررسی کنید.");

        var currency = currentVariants[0].Currency?.Trim();
        if (string.IsNullOrWhiteSpace(currency))
            throw new PosConflictException("واحد پول کالا مشخص نیست.");

        var lines = new List<PosSaleDraftLine>(ids.Length);
        decimal subtotal = 0m;

        foreach (var id in ids.OrderBy(x => x))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var variant = byId[id];
            var quantity = quantities[id];

            if (variant.AvailableQuantity < quantity)
            {
                throw new PosInsufficientStockException(
                    $"موجودی «{variant.ProductName}» کافی نیست. موجودی فعلی: {variant.AvailableQuantity}.");
            }
            if (variant.UnitPrice < 0m)
                throw new PosConflictException($"قیمت تنوع «{variant.ProductName}» معتبر نیست.");
            if (!string.Equals(currency, variant.Currency, StringComparison.OrdinalIgnoreCase))
                throw new PosConflictException("فروش هم‌زمان کالا با واحدهای پولی متفاوت مجاز نیست.");

            var lineTotal = decimal.Round(variant.UnitPrice * quantity, 2, MidpointRounding.AwayFromZero);
            subtotal = checked(subtotal + lineTotal);
            lines.Add(new PosSaleDraftLine(
                variant.Id,
                variant.ProductName,
                variant.Sku,
                variant.Barcode?.Trim() ?? string.Empty,
                quantity,
                variant.UnitPrice,
                lineTotal,
                variant.AttributeLabels ?? Array.Empty<string>()));
        }

        // No customer-supplied discounts/tax/price and no credit payment by design.
        // Stock must be decremented by CommitSaleAndDecrementStockAtomicallyAsync, not here.
        var draft = new PosSaleDraft(
            tenantId,
            sellerUserId,
            idempotencyKey,
            currency,
            paymentMethod,
            paymentReference,
            subtotal,
            subtotal,
            lines);

        return await _saleCommitter.CommitSaleAndDecrementStockAtomicallyAsync(
            draft, cancellationToken);
    }
}
