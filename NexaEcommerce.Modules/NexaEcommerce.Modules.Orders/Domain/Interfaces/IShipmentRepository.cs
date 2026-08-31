using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(
        string tenantId,
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    Task<Shipment?> GetByOrderIdAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Shipment shipment,
        CancellationToken cancellationToken = default);
}
