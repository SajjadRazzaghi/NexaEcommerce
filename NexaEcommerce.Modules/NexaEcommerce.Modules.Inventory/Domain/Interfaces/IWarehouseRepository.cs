using NexaEcommerce.Modules.Inventory.Domain.Entities;

namespace NexaEcommerce.Modules.Inventory.Domain.Interfaces;

public interface IWarehouseRepository
{
    Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(
        string tenantId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<Warehouse?> GetWarehouseAsync(
        string tenantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<Warehouse?> GetWarehouseByCodeAsync(
        string tenantId,
        string code,
        CancellationToken cancellationToken = default);

    Task<Warehouse?> GetDefaultWarehouseAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Warehouse>> GetDefaultCandidatesAsync(
        string tenantId,
        Guid? exceptWarehouseId = null,
        CancellationToken cancellationToken = default);

    Task AddWarehouseAsync(
        Warehouse warehouse,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseLocation>> GetLocationsAsync(
        string tenantId,
        Guid warehouseId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<WarehouseLocation?> GetLocationAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        CancellationToken cancellationToken = default);

    Task<WarehouseLocation?> GetLocationByCodeAsync(
        string tenantId,
        Guid warehouseId,
        string code,
        CancellationToken cancellationToken = default);

    Task AddLocationAsync(
        WarehouseLocation location,
        CancellationToken cancellationToken = default);
}
