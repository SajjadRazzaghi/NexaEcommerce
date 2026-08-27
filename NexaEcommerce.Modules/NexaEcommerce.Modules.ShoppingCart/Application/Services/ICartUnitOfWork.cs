namespace NexaEcommerce.Modules.ShoppingCart.Application.Services;

public interface ICartUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}