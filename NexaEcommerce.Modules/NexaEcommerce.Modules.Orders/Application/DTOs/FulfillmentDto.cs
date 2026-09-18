namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record FulfillmentDto(
    Guid Id,
    Guid OrderId,
    string Status,
    Guid? WarehouseId,
    Guid? PickingLocationId,
    DateTime? PickingStartedAt,
    DateTime? PickedAt,
    DateTime? PackingStartedAt,
    DateTime? PackedAt,
    DateTime? ReadyToShipAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AssignFulfillmentWarehouseRequest(
    Guid WarehouseId,
    Guid? PickingLocationId = null);

public sealed record FulfillmentQueueItemDto(
    FulfillmentDto Fulfillment,
    Guid OrderId,
    string OrderNumber,
    string ShippingFullName,
    string ShippingPhone,
    string ShippingAddress,
    string ShippingCity,
    string? ShippingPostalCode,
    decimal TotalAmount,
    string Currency,
    int ItemCount);
