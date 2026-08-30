using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

public sealed class OrderUnitOfWork(
    OrdersDbContext context)
    : IOrderUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(
            cancellationToken);
    }
}