using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

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
}