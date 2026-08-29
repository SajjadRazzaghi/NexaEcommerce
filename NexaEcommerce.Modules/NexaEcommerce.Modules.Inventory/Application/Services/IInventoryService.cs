using NexaEcommerce.Modules.Inventory.Application.DTOs;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public interface IInventoryService
{
    Task<StockDto?> GetStockAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<StockDto> SetStockAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<StockDto> AdjustStockAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<StockReservationDto> ReserveAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        string reservationKey,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    Task<StockReservationDto> ReleaseAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default);

    Task<StockReservationDto> CommitAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default);
}