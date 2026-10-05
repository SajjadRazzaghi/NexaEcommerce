namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface IProductStockReader
{
    Task<IReadOnlyDictionary<Guid, int>>
        GetAvailableQuantitiesAsync(
            IReadOnlyCollection<Guid> productVariantIds,
            CancellationToken cancellationToken = default);
}