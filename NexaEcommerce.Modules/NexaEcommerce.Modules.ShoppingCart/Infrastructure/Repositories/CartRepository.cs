using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.ShoppingCart.Infrastructure.Repositories;

public sealed class CartRepository(
    ShoppingCartDbContext context)
    : ICartRepository
{
    public async Task<Cart?> GetByUserAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == userId,
                cancellationToken);
    }

    public void Remove(Cart cart)
    {
        context.Carts.Remove(cart);
    }
    public async Task<Cart?> GetByGuestTokenAsync(
        string tenantId,
        string guestToken,
        CancellationToken cancellationToken = default)
    {
        return await context.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.UserId == null &&
                    x.GuestToken == guestToken,
                cancellationToken);
    }

    public async Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        await context.Carts.AddAsync(
            cart,
            cancellationToken);
    }

    public void Update(
        Cart cart)
    {
        context.Carts.Update(
            cart);
    }
}