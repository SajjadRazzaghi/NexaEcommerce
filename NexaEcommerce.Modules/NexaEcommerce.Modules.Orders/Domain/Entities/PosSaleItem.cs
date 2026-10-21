namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class PosSaleItem
{
    public Guid Id { get; private set; }

    public Guid PosSaleId { get; private set; }
    public PosSale? Sale { get; private set; }

    public Guid ProductVariantId { get; private set; }

    // اطلاعات محصول در زمان فروش ذخیره می‌شوند تا فاکتور تاریخی تغییر نکند.
    public string Barcode { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }

    public string ReservationKey { get; private set; } = string.Empty;

    private PosSaleItem()
    {
    }

    public PosSaleItem(
        Guid posSaleId,
        Guid productVariantId,
        string barcode,
        string sku,
        string productName,
        int quantity,
        decimal unitPrice,
        string reservationKey)
    {
        if (posSaleId == Guid.Empty)
            throw new ArgumentException("Sale id is required.");

        if (productVariantId == Guid.Empty)
            throw new ArgumentException("Product variant id is required.");

        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        Id = Guid.NewGuid();
        PosSaleId = posSaleId;
        ProductVariantId = productVariantId;
        Barcode = barcode.Trim();
        Sku = sku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = unitPrice * quantity;
        ReservationKey = reservationKey;
    }
}