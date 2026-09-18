using NexaEcommerce.Modules.Inventory.Application.DTOs;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public interface IWarehouseStockService
{
    Task<WarehouseStockDto?> GetAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStockDto>> GetByWarehouseAsync(
        string tenantId,
        Guid warehouseId,
        Guid? locationId = null,
        Guid? productVariantId = null,
        bool includeZeroStock = true,
        CancellationToken cancellationToken = default);

    Task<WarehouseStockDto> CreateAsync(
        string tenantId,
        CreateWarehouseStockRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseStockDto> SetAsync(
        string tenantId,
        SetWarehouseStockRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseStockDto> AdjustAsync(
        string tenantId,
        AdjustWarehouseStockRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseStockDto> ReceiveAsync(
        string tenantId,
        ReceiveWarehouseStockRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseStockDto> DamageAsync(
        string tenantId,
        DamageWarehouseStockRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseStockDto> SetReorderPointAsync(
        string tenantId,
        SetWarehouseReorderPointRequest request,
        CancellationToken cancellationToken = default);
}