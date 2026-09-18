namespace NexaEcommerce.Modules.Inventory.Application.DTOs;

public sealed record InventorySummaryDto(
    int WarehouseCount,
    int LocationCount,
    int StockRecordCount,
    int LowStockCount,
    int TotalOnHandQuantity,
    int TotalReservedQuantity,
    int TotalIncomingQuantity,
    int TotalDamagedQuantity);

public sealed record WarehouseStockSummaryDto(
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int IncomingQuantity,
    int DamagedQuantity,
    int ReorderPoint,
    bool IsLowStock);

public sealed record InventoryDiscrepancyDto(
    Guid ProductVariantId,
    int GlobalTotalQuantity,
    int PhysicalOnHandQuantity,
    int Difference,
    bool HasDiscrepancy);
