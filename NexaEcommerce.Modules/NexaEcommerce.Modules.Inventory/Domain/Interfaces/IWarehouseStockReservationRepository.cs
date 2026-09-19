using NexaEcommerce.Modules.Inventory.Domain.Entities;

namespace NexaEcommerce.Modules.Inventory.Domain.Interfaces;

public interface IWarehouseStockReservationRepository
{
    Task<IReadOnlyList<WarehouseStockReservation>> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStockReservation>>
        GetActiveByOrderAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken = default);

    Task AddAsync(
        WarehouseStockReservation reservation,
        CancellationToken cancellationToken = default);
}