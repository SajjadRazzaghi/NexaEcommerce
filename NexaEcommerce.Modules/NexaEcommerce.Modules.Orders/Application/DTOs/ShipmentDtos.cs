namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record CreateShipmentRequest(
    Guid OrderId,
    string ShippingMethod,
    string Carrier,
    string? TrackingNumber);

public sealed record UpdateTrackingNumberRequest(
    string TrackingNumber);

public sealed record ShipmentDto(
    Guid Id,
    Guid OrderId,
    string ShippingMethod,
    string Carrier,
    string? TrackingNumber,
    string Status,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt);

