using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IShipmentService
{
    Task<ShipmentDto?> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task<ShipmentDto> CreateAsync(
        string tenantId,
        Guid orderId,
        string shippingMethod,
        string carrier,
        string? trackingNumber,
        CancellationToken cancellationToken = default);

    Task<ShipmentDto> SetTrackingNumberAsync(
        string tenantId,
        Guid orderId,
        string trackingNumber,
        CancellationToken cancellationToken = default);

    Task<ShipmentDto> ShipAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<ShipmentDto> DeliverAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
