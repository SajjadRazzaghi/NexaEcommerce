using NexaEcommerce.Modules.Inventory.Domain.Entities;

namespace NexaEcommerce.Modules.Inventory.Domain.Interfaces;

public interface IWarehouseStockReservationRepository
{
    Task<WarehouseStockReservation?> GetAsync(
        string tenantId,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStockReservation>>
        GetPendingByOrderAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStockReservation>>
        GetExpiredPendingAsync(
            DateTimeOffset utcNow,
            int take,
            CancellationToken cancellationToken = default);

    Task AddAsync(
        WarehouseStockReservation reservation,
        CancellationToken cancellationToken = default);
}