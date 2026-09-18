using NexaEcommerce.Modules.Inventory.Domain.Entities;

namespace NexaEcommerce.Modules.Inventory.Domain.Interfaces;

public interface IWarehouseStockRepository
{

Task<IReadOnlyList<WarehouseStock>> GetAllAsync(
    string tenantId,
    bool includeZeroStock = true,
    CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<WarehouseStock?> GetAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStock>> GetByWarehouseAsync(
        string tenantId,
        Guid warehouseId,
        Guid? locationId = null,
        Guid? productVariantId = null,
        bool includeZeroStock = true,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        WarehouseStock stock,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStockMovement>>
        GetMovementsAsync(
            string tenantId,
            Guid warehouseId,
            Guid locationId,
            Guid productVariantId,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

    Task AddMovementAsync(
        WarehouseStockMovement movement,
        CancellationToken cancellationToken = default);

    Task AddTransferAsync(
        WarehouseTransfer transfer,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseTransfer>>
        GetTransfersAsync(
            string tenantId,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
}