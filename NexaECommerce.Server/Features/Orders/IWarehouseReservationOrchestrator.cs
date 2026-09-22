namespace NexaECommerce.Server.Features.Orders;

public interface IWarehouseReservationOrchestrator
{
    Task<WarehouseReservationResultDto> ReserveAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<WarehouseReleaseResultDto> ReleaseAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}