using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Products;

/// <summary>
/// Adapts the cross-module Inventory stock reader to the Catalog
/// module's narrow product-stock reader contract.
/// </summary>
public sealed class ProductStockReaderAdapter(
    IStockReader stockReader,
    ICurrentTenant currentTenant) : IProductStockReader
{
    public Task<IReadOnlyDictionary<Guid, int>>
        GetAvailableQuantitiesAsync(
            IReadOnlyCollection<Guid> productVariantIds,
            CancellationToken cancellationToken = default)
    {
        return stockReader.GetAvailableQuantitiesAsync(
            currentTenant.Id,
            productVariantIds,
            cancellationToken);
    }
}
