using NexaEcommerce.Modules.Inventory.Application.DTOs;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public interface IWarehouseStockReservationService
{
    Task<WarehouseStockReservationDto> ReserveAsync(
        string tenantId,
        ReserveWarehouseStockRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseStockReservationDto> ReleaseAsync(
        string tenantId,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<WarehouseStockReservationDto> CommitAsync(
        string tenantId,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<int> ExpirePendingAsync(
        CancellationToken cancellationToken = default);
}