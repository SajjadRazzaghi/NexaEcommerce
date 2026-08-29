using NexaEcommerce.Modules.Inventory.Domain.Entities;

namespace NexaEcommerce.Modules.Inventory.Domain.Interfaces;

public interface IInventoryRepository
{
    Task<StockItem?> GetStockAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<StockItem?> GetStockByIdAsync(
        string tenantId,
        Guid stockItemId,
        CancellationToken cancellationToken = default);

    Task<StockReservation?> GetReservationAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default);

    Task AddStockAsync(
        StockItem stockItem,
        CancellationToken cancellationToken = default);

    Task AddReservationAsync(
        StockReservation reservation,
        CancellationToken cancellationToken = default);
}