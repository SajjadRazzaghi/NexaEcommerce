using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.ShoppingCart.Infrastructure.Repositories;

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
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Carts
            .Include(c => c.Items)
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(
                c =>
                    c.TenantId == tenantId &&
                    c.UserId == userId,
                cancellationToken);
    }

    public async Task<Cart?> GetByGuestTokenAsync(
        string tenantId,
        string guestToken,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Carts
            .Include(c => c.Items)
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(
                c =>
                    c.TenantId == tenantId &&
                    c.GuestToken == guestToken,
                cancellationToken);
    }

    public async Task<CartItem?> GetItemAsync(
        Guid cartId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CartItems
            .Where(item =>
                item.CartId == cartId &&
                item.ProductVariantId == productVariantId &&
                !item.IsDeleted)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Carts.AddAsync(
            cart,
            cancellationToken);
    }

    public void Update(
        Cart cart)
    {
        _dbContext.Carts.Update(cart);
    }

    public void Remove(
        Cart cart)
    {
        _dbContext.Carts.Remove(cart);
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
        return await _dbContext.CartItems
            .Where(item =>
                item.Id == cartItemId &&
                item.CartId == cartId &&
                !item.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        item => item.Quantity,
                        quantity)
                    .SetProperty(
                        item => item.UnitPrice,
                        unitPrice)
                    .SetProperty(
                        item => item.ProductName,
                        productName)
                    .SetProperty(
                        item => item.ImageUrl,
                        imageUrl)
                    .SetProperty(
                        item => item.UpdatedAt,
                        cartUpdatedAt),
                cancellationToken);
    }

    public void ClearTracking()
    {
        _dbContext.ChangeTracker.Clear();
    }
}