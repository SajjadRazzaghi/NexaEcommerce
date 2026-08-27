using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaEcommerce.SharedKernel.Infrastructure;

namespace NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

public sealed class CartUnitOfWork(
    ShoppingCartDbContext context)
    : ICartUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(
            cancellationToken);
    }
}