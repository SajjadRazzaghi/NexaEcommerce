using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Domain.Interfaces;

public interface IShippingMethodRepository
{
    Task<ShippingMethod?> GetByIdAsync(
        string tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ShippingMethod?> GetByCodeAsync(
        string tenantId,
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingMethod>>
        GetActiveAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingMethod>>
        GetAllAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

    Task AddAsync(
        ShippingMethod shippingMethod,
        CancellationToken cancellationToken = default);

    void Remove(
        ShippingMethod shippingMethod);
}

