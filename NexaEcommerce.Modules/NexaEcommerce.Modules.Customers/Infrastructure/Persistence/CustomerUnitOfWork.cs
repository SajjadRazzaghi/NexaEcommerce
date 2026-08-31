using NexaEcommerce.Modules.Customers.Application.Services;

namespace NexaEcommerce.Modules.Customers.Infrastructure.Persistence;

public sealed class CustomerUnitOfWork(
    CustomerDbContext context)
    : ICustomerUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(
            cancellationToken);
    }
}