namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IOrderUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}