namespace NexaEcommerce.Modules.Inventory.Application.DTOs;

public sealed record ReserveWarehouseStockRequest(
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    Guid OrderId,
    int Quantity,
    int DurationMinutes = 30);

public sealed record ReleaseWarehouseStockRequest(
    Guid ReservationId);

public sealed record CommitWarehouseStockRequest(
    Guid ReservationId);

public sealed record WarehouseStockReservationDto(
    Guid Id,
    Guid WarehouseId,
    Guid LocationId,
    Guid ProductVariantId,
    Guid OrderId,
    int Quantity,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReleasedAt,
    DateTimeOffset? CommittedAt);