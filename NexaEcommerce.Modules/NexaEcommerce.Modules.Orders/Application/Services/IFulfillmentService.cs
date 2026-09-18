using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IFulfillmentService
{
    Task<FulfillmentDto?> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<FulfillmentDto> CreateForOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FulfillmentQueueItemDto>>
        GetQueueAsync(
            string tenantId,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);

    Task<FulfillmentDto> AssignWarehouseAsync(
        string tenantId,
        Guid orderId,
        Guid warehouseId,
        Guid? pickingLocationId,
        CancellationToken cancellationToken = default);

    Task<FulfillmentDto> StartPickingAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<FulfillmentDto> MarkPickedAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<FulfillmentDto> StartPackingAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<FulfillmentDto> MarkPackedAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<FulfillmentDto> MarkReadyToShipAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
