using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class InventoryReportService(
IInventoryRepository inventoryRepository,
IWarehouseStockRepository warehouseStockRepository)
: IInventoryReportService
{
    private const int MaxPageSize = 200;


public async Task<InventorySummaryDto> GetSummaryAsync(
    string tenantId,
    CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);

        var stockItems =
            await inventoryRepository.GetStocksAsync(
                tenantId,
                cancellationToken);

        var warehouseStocks =
            await warehouseStockRepository.GetAllAsync(
                tenantId,
                true,
                cancellationToken);

        var warehouseCount =
            warehouseStocks
                .Select(x => x.WarehouseId)
                .Distinct()
                .Count();

        var locationCount =
            warehouseStocks
                .Select(x => x.LocationId)
                .Distinct()
                .Count();

        var stockRecordCount =
            stockItems.Count;

        var lowStockCount =
            warehouseStocks.Count(
                x => x.IsLowStock);

        var totalOnHandQuantity =
            warehouseStocks.Sum(
                x => x.OnHandQuantity);

        var totalReservedQuantity =
            stockItems.Sum(
                x => x.ReservedQuantity);

        var totalIncomingQuantity =
            warehouseStocks.Sum(
                x => x.IncomingQuantity);

        var totalDamagedQuantity =
            warehouseStocks.Sum(
                x => x.DamagedQuantity);

        return new InventorySummaryDto(
            warehouseCount,
            locationCount,
            stockRecordCount,
            lowStockCount,
            totalOnHandQuantity,
            totalReservedQuantity,
            totalIncomingQuantity,
            totalDamagedQuantity);
    }

    public async Task<IReadOnlyList<WarehouseStockSummaryDto>>
        GetLowStockAsync(
            string tenantId,
            Guid? warehouseId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        ValidatePaging(skip, take);

        var warehouseStocks =
            await warehouseStockRepository.GetAllAsync(
                tenantId,
                true,
                cancellationToken);

        var pageSize =
            Math.Min(
                take,
                MaxPageSize);

        var query =
            warehouseStocks
                .Where(x => x.IsLowStock);

        if (warehouseId.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.WarehouseId ==
                        warehouseId.Value);
        }

        return query
            .OrderBy(
                x => x.AvailableQuantity)
            .ThenBy(
                x => x.ProductVariantId)
            .Skip(skip)
            .Take(pageSize)
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
        ValidateTenantId(tenantId);
        ValidatePaging(skip, take);

        var stockItems =
            await inventoryRepository.GetStocksAsync(
                tenantId,
                cancellationToken);

        var warehouseStocks =
            await warehouseStockRepository.GetAllAsync(
                tenantId,
                true,
                cancellationToken);

        var logicalStockByVariant =
            stockItems
                .GroupBy(
                    x => x.ProductVariantId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(
                        x => x.TotalQuantity));

        var physicalStockByVariant =
            warehouseStocks
                .GroupBy(
                    x => x.ProductVariantId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(
                        x => x.OnHandQuantity));

        var variantIds =
            logicalStockByVariant.Keys
                .Union(
                    physicalStockByVariant.Keys)
                .Distinct();

        var discrepancies =
            new List<InventoryDiscrepancyDto>();

        foreach (var productVariantId in variantIds)
        {
            logicalStockByVariant.TryGetValue(
                productVariantId,
                out var globalQuantity);

            physicalStockByVariant.TryGetValue(
                productVariantId,
                out var physicalQuantity);

            var difference =
                physicalQuantity -
                globalQuantity;

            if (difference == 0)
            {
                continue;
            }

            discrepancies.Add(
                new InventoryDiscrepancyDto(
                    productVariantId,
                    globalQuantity,
                    physicalQuantity,
                    difference,
                    true));
        }

        var pageSize =
            Math.Min(
                take,
                MaxPageSize);

        return discrepancies
            .OrderByDescending(
                x => Math.Abs(x.Difference))
            .ThenBy(
                x => x.ProductVariantId)
            .Skip(skip)
            .Take(pageSize)
            .ToList();
    }

    private static void ValidateTenantId(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }

    private static void ValidatePaging(
        int skip,
        int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skip));
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take));
        }
    }


}
