using NexaEcommerce.Modules.Inventory.Application.DTOs;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public interface IWarehouseTransferService
{
    Task<WarehouseTransferDto> TransferAsync(
        string tenantId,
        CreateWarehouseTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseTransferDto>> GetTransfersAsync(
        string tenantId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStockMovementDto>>
        GetStockMovementsAsync(
            string tenantId,
            Guid warehouseId,
            Guid locationId,
            Guid productVariantId,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
}
