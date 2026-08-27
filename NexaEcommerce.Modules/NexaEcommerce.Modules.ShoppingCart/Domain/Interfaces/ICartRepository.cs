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

    Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default);

    void Update(Cart cart);
}