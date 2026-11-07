using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

/// <summary>
/// One historical favorite action for a product. A row is active while the customer
/// currently likes the product. Removing and adding again creates a new historical row.
/// UserId is an Identity key from the server database; it intentionally has no cross-DB FK.
/// GuestKey is a random, HttpOnly browser identity issued by the Favorites API.
/// </summary>
public sealed class ProductFavorite : BaseEntity
{
    public string TenantId { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public string? UserId { get; private set; }
    public string? GuestKey { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? RemovedAt { get; private set; }

    public Product Product { get; private set; } = null!;

    private ProductFavorite()
    {
    }

    public ProductFavorite(
        string tenantId,
        Guid productId,
        string? userId,
        string? guestKey)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));

        var hasUser = !string.IsNullOrWhiteSpace(userId);
        var hasGuest = !string.IsNullOrWhiteSpace(guestKey);
        if (hasUser == hasGuest)
            throw new ArgumentException("Exactly one of UserId or GuestKey must be provided.");

        TenantId = tenantId.Trim();
        ProductId = productId;
        UserId = hasUser ? userId!.Trim() : null;
        GuestKey = hasGuest ? guestKey!.Trim() : null;
        IsActive = true;
    }

    public void AssignToUser(string userId)
    {
        if (!IsActive)
            throw new InvalidOperationException("An inactive favorite cannot be transferred.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required.", nameof(userId));

        UserId = userId.Trim();
        GuestKey = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Remove()
    {
        if (!IsActive)
            return;

        IsActive = false;
        RemovedAt = DateTime.UtcNow;
        UpdatedAt = RemovedAt;
    }
}
