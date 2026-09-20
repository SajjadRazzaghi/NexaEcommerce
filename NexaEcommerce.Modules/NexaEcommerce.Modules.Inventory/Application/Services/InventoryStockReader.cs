using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

/// <summary>
/// Reads sellable stock from physical warehouse inventory.
/// WarehouseStock is the source of truth for available product quantity.
/// </summary>
public sealed class InventoryStockReader(
    IWarehouseStockRepository repository)
    : IInventoryStockReader
{
    public async Task<int?> GetAvailableQuantityAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) ||
            productVariantId == Guid.Empty)
        {
            return null;
        }

        var stocks =
            await repository.GetAllAsync(
                tenantId.Trim(),
                includeZeroStock: true,
                cancellationToken);

        var matching =
            stocks
                .Where(x => x.ProductVariantId == productVariantId)
                .ToList();

        if (matching.Count == 0)
        {
            return null;
        }

        return matching.Sum(x => x.AvailableQuantity);
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
        var ids =
            productVariantIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

        if (string.IsNullOrWhiteSpace(tenantId) ||
            ids.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var idSet =
            ids.ToHashSet();

        var stocks =
            await repository.GetAllAsync(
                tenantId.Trim(),
                includeZeroStock: true,
                cancellationToken);

        return stocks
            .Where(x => idSet.Contains(x.ProductVariantId))
            .GroupBy(x => x.ProductVariantId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(x => x.AvailableQuantity));
    }
}
