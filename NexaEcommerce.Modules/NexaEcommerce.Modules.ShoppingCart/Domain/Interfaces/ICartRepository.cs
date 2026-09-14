using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;

namespace NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<Cart?> GetByGuestTokenAsync(
        string tenantId,
        string guestToken,
        CancellationToken cancellationToken = default);

    Task<CartItem?> GetItemAsync(
        Guid cartId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default);

    void Update(
        Cart cart);

    void Remove(
        Cart cart);

    Task<int> UpdateExistingItemDirectAsync(
        Guid cartId,
        Guid cartItemId,
        int quantity,
        decimal unitPrice,
        string productName,
        string? imageUrl,
        DateTime cartUpdatedAt,
        CancellationToken cancellationToken = default);

    void ClearTracking();
}