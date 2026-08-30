using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.ShoppingCart.Domain.Entities;

public sealed class Cart : AggregateRoot
{
    private readonly List<CartItem> _items = [];

    private Cart()
    {
    }
 
public void MergeFrom(
    Cart source,
    IReadOnlyDictionary<Guid, int> availableQuantities)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(
            availableQuantities);

        foreach (var sourceItem in source.Items)
        {
            availableQuantities.TryGetValue(
                sourceItem.ProductVariantId,
                out var available);

            var targetItem =
                _items.FirstOrDefault(
                    x =>
                        x.ProductVariantId ==
                        sourceItem.ProductVariantId);

            if (targetItem is null)
            {
                var quantity =
                    Math.Min(
                        sourceItem.Quantity,
                        available);

                if (quantity > 0)
                {
                    AddItem(
                        sourceItem.ProductVariantId,
                        quantity,
                        sourceItem.UnitPrice,
                        sourceItem.ProductName,
                        sourceItem.ImageUrl);
                }

                continue;
            }

            var totalRequested =
                targetItem.Quantity +
                sourceItem.Quantity;

            var finalQuantity =
                Math.Min(
                    totalRequested,
                    available);

            if (finalQuantity <= 0)
            {
                _items.Remove(targetItem);
            }
            else
            {
                targetItem.SetQuantity(
                    finalQuantity,
                    sourceItem.UnitPrice,
                    sourceItem.ProductName,
                    sourceItem.ImageUrl);
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private Cart(
        string tenantId,
        string? userId,
        string? guestToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));

        if (string.IsNullOrWhiteSpace(userId) &&
            string.IsNullOrWhiteSpace(guestToken))
        {
            throw new ArgumentException(
                "Either user id or guest token is required.");
        }

        TenantId = tenantId.Trim();
        UserId = string.IsNullOrWhiteSpace(userId)
            ? null
            : userId.Trim();

        GuestToken = string.IsNullOrWhiteSpace(guestToken)
            ? null
            : guestToken.Trim();
    }

    public string TenantId { get; private set; } = null!;

    public string? UserId { get; private set; }

    public string? GuestToken { get; private set; }

    public IReadOnlyCollection<CartItem> Items =>
        _items.AsReadOnly();

    public static Cart ForUser(
        string tenantId,
        string userId)
    {
        return new Cart(
            tenantId,
            userId,
            null);
    }

    public static Cart ForGuest(
        string tenantId,
        string guestToken)
    {
        return new Cart(
            tenantId,
            null,
            guestToken);
    }

    public CartItem AddItem(
        Guid productVariantId,
        int quantity,
        decimal unitPrice,
        string productName,
        string? imageUrl)
    {
        ValidateProductVariantId(
            productVariantId);

        ValidateQuantity(quantity);

        var existing =
            _items.FirstOrDefault(
                x => x.ProductVariantId ==
                     productVariantId);

        if (existing is not null)
        {
            existing.IncreaseQuantity(
                quantity,
                unitPrice,
                productName,
                imageUrl);

            UpdatedAt = DateTime.UtcNow;

            return existing;
        }

        var item =
            new CartItem(
                Id,
                productVariantId,
                quantity,
                unitPrice,
                productName,
                imageUrl);

        _items.Add(item);

        UpdatedAt = DateTime.UtcNow;

        return item;
    }

    public void SetQuantity(
        Guid productVariantId,
        int quantity,
        decimal unitPrice,
        string productName,
        string? imageUrl)
    {
        ValidateProductVariantId(
            productVariantId);

        var existing =
            _items.FirstOrDefault(
                x => x.ProductVariantId ==
                     productVariantId);

        if (quantity <= 0)
        {
            if (existing is not null)
                _items.Remove(existing);

            UpdatedAt = DateTime.UtcNow;

            return;
        }

        ValidateQuantity(quantity);

        if (existing is null)
        {
            AddItem(
                productVariantId,
                quantity,
                unitPrice,
                productName,
                imageUrl);

            return;
        }

        existing.SetQuantity(
            quantity,
            unitPrice,
            productName,
            imageUrl);

        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(
        Guid productVariantId)
    {
        ValidateProductVariantId(
            productVariantId);

        var existing =
            _items.FirstOrDefault(
                x => x.ProductVariantId ==
                     productVariantId);

        if (existing is null)
            return;

        _items.Remove(existing);

        UpdatedAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        _items.Clear();

        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateProductVariantId(
        Guid productVariantId)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }
    }

    private static void ValidateQuantity(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }
    }
}