using NexaEcommerce.Modules.Inventory.Application.DTOs;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public interface IInventoryReportService
{
    Task<InventorySummaryDto> GetSummaryAsync(
    string tenantId,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<WarehouseStockSummaryDto>> GetLowStockAsync(
    string tenantId,
    Guid? warehouseId,
    int skip,
    int take,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryDiscrepancyDto>> GetDiscrepanciesAsync(
        string tenantId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

}
