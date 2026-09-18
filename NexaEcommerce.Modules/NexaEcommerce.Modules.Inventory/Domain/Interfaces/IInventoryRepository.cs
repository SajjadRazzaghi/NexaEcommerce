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

Task<IReadOnlyList<StockReservation>> GetExpiredReservationsAsync(
    DateTimeOffset now,
    int batchSize,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<StockItem>> GetStocksAsync(
    string tenantId,
    CancellationToken cancellationToken = default);

Task<int> GetStockCountAsync(
    string tenantId,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<InventoryMovement>> GetMovementsAsync(
    string tenantId,
    Guid productVariantId,
    int skip,
    int take,
    CancellationToken cancellationToken = default);

Task AddMovementAsync(
    InventoryMovement movement,
    CancellationToken cancellationToken = default);

Task AddStockAsync(
    StockItem stockItem,
    CancellationToken cancellationToken = default);

Task AddReservationAsync(
    StockReservation reservation,
    CancellationToken cancellationToken = default);

void ClearTracking();


}
