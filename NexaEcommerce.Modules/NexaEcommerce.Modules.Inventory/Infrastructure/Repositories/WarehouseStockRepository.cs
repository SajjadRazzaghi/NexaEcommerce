using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Repositories;

public sealed class WarehouseStockRepository(
    InventoryDbContext context)
    : IWarehouseStockRepository
{
  public async Task<IReadOnlyList<WarehouseStock>> GetAllAsync(
    string tenantId,
    bool includeZeroStock = true,
    CancellationToken cancellationToken = default)
    {
        var query =
            context.WarehouseStocks
                .AsNoTracking()
                .Where(
                    x => x.TenantId == tenantId);

        if (!includeZeroStock)
        {
            query =
                query.Where(
                    x =>
                        x.OnHandQuantity > 0 ||
                        x.ReservedQuantity > 0 ||
                        x.IncomingQuantity > 0 ||
                        x.DamagedQuantity > 0);
        }

        return await query
            .OrderBy(
                x => x.WarehouseId)
            .ThenBy(
                x => x.LocationId)
            .ThenBy(
                x => x.ProductVariantId)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<int> GetCountAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.WarehouseStocks
            .CountAsync(
                x => x.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<WarehouseStock?> GetAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        return await context.WarehouseStocks
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.WarehouseId == warehouseId &&
                    x.LocationId == locationId &&
                    x.ProductVariantId == productVariantId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseStock>>
        GetByWarehouseAsync(
            string tenantId,
            Guid warehouseId,
            Guid? locationId = null,
            Guid? productVariantId = null,
            bool includeZeroStock = true,
            CancellationToken cancellationToken = default)
    {
        var query =
            context.WarehouseStocks
                .AsNoTracking()
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.WarehouseId == warehouseId);

        if (locationId.HasValue)
        {
            query =
                query.Where(
                    x => x.LocationId == locationId.Value);
        }

        if (productVariantId.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.ProductVariantId ==
                        productVariantId.Value);
        }

        if (!includeZeroStock)
        {
            query =
                query.Where(
                    x =>
                        x.OnHandQuantity > 0 ||
                        x.ReservedQuantity > 0 ||
                        x.IncomingQuantity > 0 ||
                        x.DamagedQuantity > 0);
        }

        return await query
            .OrderBy(
                x => x.ProductVariantId)
            .ThenBy(
                x => x.LocationId)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        WarehouseStock stock,
        CancellationToken cancellationToken = default)
    {
        await context.WarehouseStocks.AddAsync(
            stock,
            cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseStockMovement>>
        GetMovementsAsync(
            string tenantId,
            Guid warehouseId,
            Guid locationId,
            Guid productVariantId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        return await context.WarehouseStockMovements
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId &&
                    x.WarehouseId == warehouseId &&
                    x.LocationId == locationId &&
                    x.ProductVariantId == productVariantId)
            .OrderByDescending(
                x => x.OccurredAt)
            .ThenByDescending(
                x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddMovementAsync(
        WarehouseStockMovement movement,
        CancellationToken cancellationToken = default)
    {
        await context.WarehouseStockMovements.AddAsync(
            movement,
            cancellationToken);
    }

    public async Task AddTransferAsync(
        WarehouseTransfer transfer,
        CancellationToken cancellationToken = default)
    {
        await context.WarehouseTransfers.AddAsync(
            transfer,
            cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseTransfer>>
        GetTransfersAsync(
            string tenantId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        return await context.WarehouseTransfers
            .AsNoTracking()
            .Where(
                x => x.TenantId == tenantId)
            .OrderByDescending(
                x => x.RequestedAt)
            .ThenByDescending(
                x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(
                cancellationToken);
    }
}
