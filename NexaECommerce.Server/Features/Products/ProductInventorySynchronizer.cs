using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Products;

public sealed class ProductInventorySynchronizer(
    IInventoryService inventoryService)
{
    public async Task SyncMissingStockAsync(
        string tenantId,
        ProductDto product,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        foreach (var variant in product.Variants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!variant.IsActive)
            {
                continue;
            }

            var existingStock =
                await inventoryService.GetStockAsync(
                    tenantId,
                    variant.Id,
                    cancellationToken);

            if (existingStock is not null)
            {
                continue;
            }

            await inventoryService.SetStockAsync(
                tenantId,
                variant.Id,
                Math.Max(0, variant.StockQuantity),
                cancellationToken);
        }
    }
}