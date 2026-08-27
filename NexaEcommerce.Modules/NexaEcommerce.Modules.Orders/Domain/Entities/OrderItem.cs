using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    private OrderItem()
    {
    }

    internal OrderItem(
        Guid orderId,
        Guid productVariantId,
        string sku,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException(nameof(orderId));

        if (productVariantId == Guid.Empty)
            throw new ArgumentException(nameof(productVariantId));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException(nameof(sku));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(nameof(productName));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        OrderId = orderId;
        ProductVariantId = productVariantId;
        Sku = sku.Trim();
        ProductName = productName.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid OrderId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public string Sku { get; private set; } = null!;

    public string ProductName { get; private set; } = null!;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal =>
        UnitPrice * Quantity;

    public Order Order { get; private set; } = null!;
}