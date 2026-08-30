using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class InventoryStockReader(
    IInventoryRepository repository)
    : IInventoryStockReader
{
    public async Task<int?> GetAvailableQuantityAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var stock =
            await repository.GetStockAsync(
                tenantId,
                productVariantId,
                cancellationToken);

        return stock?.AvailableQuantity;
    }

    public async Task<bool> IsInStockAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var quantity =
            await GetAvailableQuantityAsync(
                tenantId,
                productVariantId,
                cancellationToken);

        return quantity is > 0;
    }

    public async Task<IReadOnlyDictionary<Guid, int>>
        GetAvailableQuantitiesAsync(
            string tenantId,
            IEnumerable<Guid> productVariantIds,
            CancellationToken cancellationToken = default)
    {
        var result =
            new Dictionary<Guid, int>();

        foreach (
            var productVariantId in
            productVariantIds
                .Where(x => x != Guid.Empty)
                .Distinct())
        {
            var quantity =
                await GetAvailableQuantityAsync(
                    tenantId,
                    productVariantId,
                    cancellationToken);

            if (quantity.HasValue)
            {
                result[productVariantId] =
                    quantity.Value;
            }
        }

        return result;
    }
}

