using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;

namespace NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

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
        CancellationToken cancellationToken)
    {
        // ✅ حیاتی: حذف AsNoTracking و اضافه کردن Include برای ردیابی صحیح آیتم‌ها
        return await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.UserId == userId,
                cancellationToken);
    }

    public async Task<Cart?> GetByGuestTokenAsync(
        string tenantId,
        string guestToken,
        CancellationToken cancellationToken)
    {
        // ✅ حیاتی: حذف AsNoTracking و اضافه کردن Include
        return await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.GuestToken == guestToken,
                cancellationToken);
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken)
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
}