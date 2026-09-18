using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface IFulfillmentRepository
{
    Task<Fulfillment?> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Fulfillment>> GetByStatusesAsync(
        string tenantId,
        IReadOnlyCollection<FulfillmentStatus> statuses,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Fulfillment fulfillment,
        CancellationToken cancellationToken = default);
}
