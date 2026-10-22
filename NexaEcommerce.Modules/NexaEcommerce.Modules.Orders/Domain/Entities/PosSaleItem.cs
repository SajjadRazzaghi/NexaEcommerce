using System.Text.Json;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class PosSaleItem
{
    public Guid Id { get; private set; }
    public Guid PosSaleId { get; private set; }
    public PosSale? Sale { get; private set; }
    public Guid ProductVariantId { get; private set; }

    public string? Barcode { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
    public string ReservationKey { get; private set; } = string.Empty;
    public string AttributesJson { get; private set; } = "[]";

    private PosSaleItem() { }

    public PosSaleItem(
        Guid posSaleId,
        Guid productVariantId,
        string? barcode,
        string sku,
        string productName,
        int quantity,
        decimal unitPrice,
        string reservationKey,
        IReadOnlyCollection<string>? attributeLabels = null)
    {
        if (posSaleId == Guid.Empty)
            throw new ArgumentException("Sale id is required.", nameof(posSaleId));
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("Product variant id is required.", nameof(productVariantId));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));
        if (quantity is < 1 or > 999)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (string.IsNullOrWhiteSpace(reservationKey) || reservationKey.Length > 128)
            throw new ArgumentException("A valid sale reference is required.", nameof(reservationKey));
        if (barcode is not null && barcode.Trim().Length > 128)
            throw new ArgumentOutOfRangeException(nameof(barcode));

        Id = Guid.NewGuid();
        PosSaleId = posSaleId;
        ProductVariantId = productVariantId;
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        Sku = sku.Trim();
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        LineTotal = decimal.Round(UnitPrice * quantity, 2, MidpointRounding.AwayFromZero);
        ReservationKey = reservationKey.Trim();
        AttributesJson = JsonSerializer.Serialize(
            (attributeLabels ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray());
    }
}