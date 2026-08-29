namespace NexaEcommerce.Modules.Inventory.Application.DTOs;

public sealed record StockDto(
    Guid ProductVariantId,
    int AvailableQuantity,
    int ReservedQuantity,
    int TotalQuantity);

public sealed record SetStockRequest(
    Guid ProductVariantId,
    int Quantity);

public sealed record AdjustStockRequest(
    Guid ProductVariantId,
    int Quantity);

public sealed record ReserveStockRequest(
    Guid ProductVariantId,
    int Quantity,
    string ReservationKey,
    int ExpirationMinutes = 15);

public sealed record StockReservationDto(
    string ReservationKey,
    Guid ProductVariantId,
    int Quantity,
    string Status,
    DateTimeOffset ExpiresAt);