using NexaEcommerce.Modules.Inventory.Application.DTOs;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(
        string tenantId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<WarehouseDto> CreateWarehouseAsync(
        string tenantId,
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseDto> UpdateWarehouseAsync(
        string tenantId,
        Guid warehouseId,
        UpdateWarehouseRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseDto> SetWarehouseStatusAsync(
        string tenantId,
        Guid warehouseId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<WarehouseDto> SetDefaultWarehouseAsync(
        string tenantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseLocationDto>> GetLocationsAsync(
        string tenantId,
        Guid warehouseId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<WarehouseLocationDto> CreateLocationAsync(
        string tenantId,
        CreateWarehouseLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseLocationDto> UpdateLocationAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        UpdateWarehouseLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseLocationDto> SetLocationStatusAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        bool isActive,
        CancellationToken cancellationToken = default);
}