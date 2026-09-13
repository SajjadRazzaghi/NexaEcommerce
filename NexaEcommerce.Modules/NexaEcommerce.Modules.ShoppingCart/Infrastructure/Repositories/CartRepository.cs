
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;

namespace NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

public sealed class CartRepository : ICartRepository
{
    private readonly ShoppingCartDbContext _dbContext;

    public CartRepository(
        ShoppingCartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Cart?> GetByUserAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Carts
            .Include(
                cart => cart.Items)
            .FirstOrDefaultAsync(
                cart =>
                    cart.TenantId == tenantId &&
                    cart.UserId == userId,
                cancellationToken);
    }

    public async Task<Cart?> GetByGuestTokenAsync(
        string tenantId,
        string guestToken,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Carts
            .Include(
                cart => cart.Items)
            .FirstOrDefaultAsync(
                cart =>
                    cart.TenantId == tenantId &&
                    cart.GuestToken == guestToken,
                cancellationToken);
    }

    public async Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken)
    {
        await _dbContext.Carts.AddAsync(
            cart,
            cancellationToken);
    }

    public void Update(
        Cart cart)
    {
        _dbContext.Carts.Update(
            cart);
    }

    public void Remove(
        Cart cart)
    {
        _dbContext.Carts.Remove(
            cart);
    }

    public async Task<int> UpdateExistingItemDirectAsync(
        Guid cartId,
        Guid cartItemId,
        int quantity,
        decimal unitPrice,
        string productName,
        string? imageUrl,
        DateTime cartUpdatedAt,
        CancellationToken cancellationToken = default)
    {
        var affectedRows =
            await _dbContext.CartItems
                .Where(
                    item =>
                        item.Id == cartItemId &&
                        item.CartId == cartId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                item =>
                                    item.Quantity,
                                quantity)
                            .SetProperty(
                                item =>
                                    item.UnitPrice,
                                unitPrice)
                            .SetProperty(
                                item =>
                                    item.ProductName,
                                productName)
                            .SetProperty(
                                item =>
                                    item.ImageUrl,
                                imageUrl)
                            .SetProperty(
                                item =>
                                    item.UpdatedAt,
                                cartUpdatedAt),
                    cancellationToken);

        if (affectedRows == 1)
        {
            await _dbContext.Carts
                .Where(
                    cart =>
                        cart.Id == cartId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            cart =>
                                cart.UpdatedAt,
                            cartUpdatedAt),
                    cancellationToken);
        }

        return affectedRows;
    }

    public void Detach(
        object entity)
    {
        _dbContext.Entry(
            entity).State =
            EntityState.Detached;
    }

    public void AcceptUpdatedEntities(
        Cart cart,
        CartItem item)
    {
        _dbContext.Entry(
            item).State =
            EntityState.Unchanged;

        _dbContext.Entry(
            cart).State =
            EntityState.Unchanged;
    }
}
