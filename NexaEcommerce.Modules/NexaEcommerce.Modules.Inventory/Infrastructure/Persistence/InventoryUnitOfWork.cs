using Microsoft.EntityFrameworkCore;
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

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var result =
                await action(
                    cancellationToken);

            await context.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }
}
