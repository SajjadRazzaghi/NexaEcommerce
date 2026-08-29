using NexaEcommerce.Modules.Inventory.Application.Services;

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryUnitOfWork(
    InventoryDbContext context)
    : IInventoryUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(
            cancellationToken);
    }
}