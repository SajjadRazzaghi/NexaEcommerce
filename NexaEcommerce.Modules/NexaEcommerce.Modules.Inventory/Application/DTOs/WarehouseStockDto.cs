namespace NexaEcommerce.Modules.Inventory.Application.DTOs;

public sealed record WarehouseStockDto(
    Guid Id,
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int IncomingQuantity,
    int DamagedQuantity,
    int ReorderPoint,
    bool IsLowStock,
    int Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateWarehouseStockRequest(
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    int OnHandQuantity = 0,
    int ReservedQuantity = 0,
    int IncomingQuantity = 0,
    int DamagedQuantity = 0,
    int ReorderPoint = 0);

public sealed record SetWarehouseStockRequest(
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    int OnHandQuantity,
    int ReservedQuantity,
    int IncomingQuantity,
    int DamagedQuantity,
    int ReorderPoint);

public sealed record AdjustWarehouseStockRequest(
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    int Quantity,
    string? Reason = null);

public sealed record ReceiveWarehouseStockRequest(
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    int Quantity,
    string? Reason = null);

public sealed record DamageWarehouseStockRequest(
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    int Quantity,
    string? Reason = null);

public sealed record SetWarehouseReorderPointRequest(
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    int ReorderPoint);
public sealed record WarehouseTransferDto(
    Guid Id,
    Guid SourceWarehouseId,
    Guid SourceLocationId,
    Guid DestinationWarehouseId,
    Guid DestinationLocationId,
    Guid ProductVariantId,
    int Quantity,
    string Status,
    string? Reason,
    DateTime RequestedAt,
    DateTime? CompletedAt);

public sealed record CreateWarehouseTransferRequest(
    Guid SourceWarehouseId,
    Guid SourceLocationId,
    Guid DestinationWarehouseId,
    Guid DestinationLocationId,
    Guid ProductVariantId,
    int Quantity,
    string? Reason = null);

public sealed record WarehouseStockMovementDto(
    Guid Id,
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    string Type,
    int QuantityDelta,
    int BalanceAfter,
    string? ReferenceType,
    string? ReferenceId,
    string? Reason,
    DateTime OccurredAt);
