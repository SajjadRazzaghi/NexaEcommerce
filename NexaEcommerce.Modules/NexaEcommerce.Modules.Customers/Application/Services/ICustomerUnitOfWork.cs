namespace NexaEcommerce.Modules.Customers.Application.Services;

public interface ICustomerUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}