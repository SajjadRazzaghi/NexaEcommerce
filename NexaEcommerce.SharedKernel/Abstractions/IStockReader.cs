namespace NexaEcommerce.SharedKernel.Abstractions;

public interface IStockReader
{
    Task<int?> GetAvailableQuantityAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<bool> IsInStockAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}