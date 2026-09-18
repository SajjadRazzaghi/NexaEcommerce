using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Repositories;

public sealed class WarehouseRepository(
    InventoryDbContext context)
    : IWarehouseRepository
{
    public async Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(
        string tenantId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query =
            context.Warehouses
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId);

        if (!includeInactive)
        {
            query =
                query.Where(
                    x => x.IsActive);
        }

        return await query
            .OrderByDescending(
                x => x.IsDefault)
            .ThenBy(
                x => x.Name)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<Warehouse?> GetWarehouseAsync(
        string tenantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await context.Warehouses
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == warehouseId,
                cancellationToken);
    }

    public async Task<Warehouse?> GetWarehouseByCodeAsync(
        string tenantId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return await context.Warehouses
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Code == code,
                cancellationToken);
    }

    public async Task<Warehouse?> GetDefaultWarehouseAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.Warehouses
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.IsDefault &&
                    x.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Warehouse>>
        GetDefaultCandidatesAsync(
            string tenantId,
            Guid? exceptWarehouseId = null,
            CancellationToken cancellationToken = default)
    {
        var query =
            context.Warehouses
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.IsDefault);

        if (exceptWarehouseId.HasValue)
        {
            query =
                query.Where(
                    x => x.Id != exceptWarehouseId.Value);
        }

        return await query
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddWarehouseAsync(
        Warehouse warehouse,
        CancellationToken cancellationToken = default)
    {
        await context.Warehouses.AddAsync(
            warehouse,
            cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseLocation>>
        GetLocationsAsync(
            string tenantId,
            Guid warehouseId,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
    {
        var query =
            context.WarehouseLocations
                .AsNoTracking()
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.WarehouseId == warehouseId);

        if (!includeInactive)
        {
            query =
                query.Where(
                    x => x.IsActive);
        }

        return await query
            .OrderBy(
                x => x.Code)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<WarehouseLocation?> GetLocationAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        return await context.WarehouseLocations
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.WarehouseId == warehouseId &&
                    x.Id == locationId,
                cancellationToken);
    }

    public async Task<WarehouseLocation?> GetLocationByCodeAsync(
        string tenantId,
        Guid warehouseId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return await context.WarehouseLocations
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.WarehouseId == warehouseId &&
                    x.Code == code,
                cancellationToken);
    }

    public async Task AddLocationAsync(
        WarehouseLocation location,
        CancellationToken cancellationToken = default)
    {
        await context.WarehouseLocations.AddAsync(
            location,
            cancellationToken);
    }
}