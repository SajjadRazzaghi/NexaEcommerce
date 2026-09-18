using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class InventoryReportService(
    IInventoryRepository inventoryRepository,
    IWarehouseStockRepository warehouseStockRepository)
    : IInventoryReportService
{
    public async Task<InventorySummaryDto> GetSummaryAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var stockItems =
            await inventoryRepository.GetStocksAsync(
                tenantId,
                cancellationToken);

        var warehouseStocks =
            await warehouseStockRepository.GetAllAsync(
                tenantId,
                false,
                cancellationToken);

        var stockItemCount =
            stockItems.Count;

        var warehouseCount =
            warehouseStocks
                .Select(x => x.WarehouseId)
                .Distinct()
                .Count();

        var totalAvailable =
            stockItems.Sum(x => x.AvailableQuantity);

        var totalReserved =
            stockItems.Sum(x => x.ReservedQuantity);

        var totalQuantity =
            stockItems.Sum(x => x.TotalQuantity);

        var totalOnHand =
            warehouseStocks.Sum(x => x.OnHandQuantity);

        var totalIncoming =
            warehouseStocks.Sum(x => x.IncomingQuantity);

        var totalDamaged =
            warehouseStocks.Sum(x => x.DamagedQuantity);

        return new InventorySummaryDto(
            stockItemCount,
            warehouseCount,
            totalAvailable,
            totalReserved,
            totalQuantity,
            totalOnHand,
            totalIncoming,
            totalDamaged);
    }

    public async Task<IReadOnlyList<WarehouseStockSummaryDto>>
       GetLowStockAsync(
    string tenantId,
    Guid? warehouseId,
    int skip,
    int take,
    CancellationToken cancellationToken)
    {
        var warehouseStocks =
            await warehouseStockRepository.GetAllAsync(
                tenantId,
                false,
                cancellationToken);

        return warehouseStocks
            .Where(x => x.IsLowStock)
            .OrderBy(x => x.AvailableQuantity)
            .ThenBy(x => x.ProductVariantId)
            .Select(
                x =>
                    new WarehouseStockSummaryDto(
                        x.WarehouseId,
                        x.LocationId,
                        x.ProductVariantId,
                        x.OnHandQuantity,
                        x.ReservedQuantity,
                        x.AvailableQuantity,
                        x.IncomingQuantity,
                        x.DamagedQuantity,
                        x.ReorderPoint,
                        x.IsLowStock))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryDiscrepancyDto>>
       GetDiscrepanciesAsync(
    string tenantId,
    int skip,
    int take,
    CancellationToken cancellationToken = default)
    {
        var stockItems =
            await inventoryRepository.GetStocksAsync(
                tenantId,
                cancellationToken);

        var warehouseStocks =
            await warehouseStockRepository.GetAllAsync(
                tenantId,
                false,
                cancellationToken);

        var logicalStockByVariant =
            stockItems.ToDictionary(
                x => x.ProductVariantId,
                x => x.TotalQuantity);

        var physicalStockByVariant =
            warehouseStocks
                .GroupBy(x => x.ProductVariantId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(
                        x => x.OnHandQuantity));

        var variantIds =
            logicalStockByVariant.Keys
                .Union(physicalStockByVariant.Keys)
                .Distinct();

        var result =
            new List<InventoryDiscrepancyDto>();

        foreach (var productVariantId in variantIds)
        {
            logicalStockByVariant.TryGetValue(
                productVariantId,
                out var inventoryQuantity);

            physicalStockByVariant.TryGetValue(
                productVariantId,
                out var physicalQuantity);

            var difference =
                physicalQuantity - inventoryQuantity;

            if (difference == 0)
            {
                continue;
            }

            result.Add(
                new InventoryDiscrepancyDto(
                    productVariantId,
                    inventoryQuantity,
                    physicalQuantity,
                    difference,
                    true));
        }

        return result
            .OrderByDescending(
                x => Math.Abs(x.Difference))
            .ThenBy(
                x => x.ProductVariantId)
            .ToList();
    }
}