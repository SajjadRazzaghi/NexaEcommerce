namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IOrderConcurrencyService
{
    Task<T> ExecuteAsync<T>(
        string tenantId,
        Guid orderId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task ExecuteAsync(
        string tenantId,
        Guid orderId,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}