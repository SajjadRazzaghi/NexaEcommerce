using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.ShoppingCart.Domain.Entities;

public sealed class Cart : AggregateRoot
{
    private readonly List<CartItem> _items = [];

    private Cart()
    {
    }

    private Cart(
        string tenantId,
        string? userId,
        string? guestToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId) &&
            string.IsNullOrWhiteSpace(guestToken))
        {
            throw new ArgumentException(
                "Either user id or guest token is required.");
        }

        if (!string.IsNullOrWhiteSpace(userId) &&
            !string.IsNullOrWhiteSpace(guestToken))
        {
            throw new ArgumentException(
                "A cart cannot have both user id and guest token.");
        }

        TenantId =
            tenantId.Trim();

        UserId =
            string.IsNullOrWhiteSpace(userId)
                ? null
                : userId.Trim();

        GuestToken =
            string.IsNullOrWhiteSpace(guestToken)
                ? null
                : guestToken.Trim();

        /*
         * The current SQL Server schema requires UpdatedAt to be non-null.
         * A newly created cart has not been updated yet, so its creation
         * timestamp is the correct initial UpdatedAt value.
         */
        UpdatedAt =
            CreatedAt;
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

        ValidateQuantity(
            quantity);

        var existing =
            _items.FirstOrDefault(
                item =>
                    item.ProductVariantId ==
                    productVariantId);

        if (existing is not null)
        {
            existing.IncreaseQuantity(
                quantity,
                unitPrice,
                productName,
                imageUrl);

            UpdatedAt =
                DateTime.UtcNow;

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

        _items.Add(
            item);

        UpdatedAt =
            DateTime.UtcNow;

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
                item =>
                    item.ProductVariantId ==
                    productVariantId);

        if (quantity <= 0)
        {
            if (existing is not null)
            {
                _items.Remove(
                    existing);
            }

            UpdatedAt =
                DateTime.UtcNow;

            return;
        }

        ValidateQuantity(
            quantity);

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

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void RemoveItem(
        Guid productVariantId)
    {
        ValidateProductVariantId(
            productVariantId);

        var existing =
            _items.FirstOrDefault(
                item =>
                    item.ProductVariantId ==
                    productVariantId);

        if (existing is null)
        {
            return;
        }

        _items.Remove(
            existing);

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Clear()
    {
        _items.Clear();

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void MergeFrom(
        Cart source,
        IReadOnlyDictionary<Guid, int> availableQuantities)
    {
        ArgumentNullException.ThrowIfNull(
            source);

        ArgumentNullException.ThrowIfNull(
            availableQuantities);

        foreach (var sourceItem in source.Items)
        {
            availableQuantities.TryGetValue(
                sourceItem.ProductVariantId,
                out var availableQuantity);

            if (availableQuantity <= 0)
            {
                continue;
            }

            var targetItem =
                _items.FirstOrDefault(
                    item =>
                        item.ProductVariantId ==
                        sourceItem.ProductVariantId);

            if (targetItem is null)
            {
                var quantity =
                    Math.Min(
                        sourceItem.Quantity,
                        availableQuantity);

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

            var requestedTotal =
                targetItem.Quantity +
                sourceItem.Quantity;

            var finalQuantity =
                Math.Min(
                    requestedTotal,
                    availableQuantity);

            if (finalQuantity <= 0)
            {
                _items.Remove(
                    targetItem);

                continue;
            }

            targetItem.SetQuantity(
                finalQuantity,
                sourceItem.UnitPrice,
                sourceItem.ProductName,
                sourceItem.ImageUrl);
        }

        UpdatedAt =
            DateTime.UtcNow;
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