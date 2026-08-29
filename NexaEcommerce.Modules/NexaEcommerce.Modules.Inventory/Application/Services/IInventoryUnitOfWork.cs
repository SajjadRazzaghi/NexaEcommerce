namespace NexaEcommerce.Modules.Inventory.Application.Services;

public interface IInventoryUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}