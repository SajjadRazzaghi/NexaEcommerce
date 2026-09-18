namespace NexaEcommerce.Modules.Inventory.Application.Services;

public interface IInventoryUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
