using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.ShoppingCart.Infrastructure.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly ShoppingCartDbContext _dbContext;

    public CartRepository(ShoppingCartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Cart?> GetByUserAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // ✅ حیاتی: بدون AsNoTracking و با Include برای ردیابی صحیح تغییرات
        return await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.UserId == userId,
                cancellationToken);
    }

    public async Task<Cart?> GetByGuestTokenAsync(
        string tenantId,
        string guestToken,
        CancellationToken cancellationToken = default)
    {
        // ✅ حیاتی: بدون AsNoTracking و با Include
        return await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.GuestToken == guestToken,
                cancellationToken);
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _dbContext.Carts.AddAsync(cart, cancellationToken);
    }

    public void Update(Cart cart)
    {
        _dbContext.Carts.Update(cart);
    }

    public void Remove(Cart cart)
    {
        _dbContext.Carts.Remove(cart);
    }

    // ✅ اصلاح شده: نوع بازگشتی دقیقاً Task<int> و پارامترها دقیقاً منطبق با اینترفیس
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
        return await _dbContext.Set<CartItem>()
            .Where(x => x.Id == cartItemId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Quantity, quantity)
                .SetProperty(x => x.UnitPrice, unitPrice)
                .SetProperty(x => x.ProductName, productName)
                .SetProperty(x => x.ImageUrl, imageUrl)
                .SetProperty(x => x.UpdatedAt, cartUpdatedAt), // UpdatedAt از BaseEntity به ارث می‌رسد
            cancellationToken);
    }

    // ✅ پیاده‌سازی متد ClearTracking مطابق اینترفیس
    public void ClearTracking()
    {
        _dbContext.ChangeTracker.Clear();
    }
}