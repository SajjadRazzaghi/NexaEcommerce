using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.ShoppingCart.Domain.Entities;

public sealed class CartItem : BaseEntity
{
    private CartItem()
    {
    }

    internal CartItem(
        Guid cartId,
        Guid productVariantId,
        int quantity,
        decimal unitPrice,
        string productName,
        string? imageUrl)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentException(
                "Cart id is required.",
                nameof(cartId));

        if (productVariantId == Guid.Empty)
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(
                "Product name is required.",
                nameof(productName));

        CartId = cartId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ProductName = productName.Trim();
        ImageUrl = imageUrl;
    }

    public Guid CartId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public string ProductName { get; private set; } =
        string.Empty;

    public string? ImageUrl { get; private set; }

    public Cart Cart { get; private set; } = null!;

    public decimal LineTotal =>
        UnitPrice * Quantity;

    internal void IncreaseQuantity(
        int quantity,
        decimal unitPrice,
        string productName,
        string? imageUrl)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity));

        Quantity += quantity;

        UpdateSnapshot(
            unitPrice,
            productName,
            imageUrl);
    }

    internal void SetQuantity(
        int quantity,
        decimal unitPrice,
        string productName,
        string? imageUrl)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity));

        Quantity = quantity;

        UpdateSnapshot(
            unitPrice,
            productName,
            imageUrl);
    }

    private void UpdateSnapshot(
        decimal unitPrice,
        string productName,
        string? imageUrl)
    {
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(
                "Product name is required.",
                nameof(productName));

        UnitPrice = unitPrice;
        ProductName = productName.Trim();
        ImageUrl = imageUrl;

        UpdatedAt = DateTime.UtcNow;
    }
}