using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IShippingMethodService
{
    Task<IReadOnlyList<ShippingMethodDto>>
        GetActiveAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingMethodDto>>
        GetAllAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

    Task<ShippingMethodDto?>
        GetAsync(
            string tenantId,
            Guid id,
            CancellationToken cancellationToken = default);

    Task<ShippingMethodDto>
        CreateAsync(
            string tenantId,
            CreateShippingMethodRequest request,
            CancellationToken cancellationToken = default);

    Task<ShippingMethodDto?>
        UpdateAsync(
            string tenantId,
            Guid id,
            UpdateShippingMethodRequest request,
            CancellationToken cancellationToken = default);

    Task<bool>
        SetActiveAsync(
            string tenantId,
            Guid id,
            bool active,
            CancellationToken cancellationToken = default);

    Task<bool>
        DeleteAsync(
            string tenantId,
            Guid id,
            CancellationToken cancellationToken = default);

    Task<ShippingQuoteDto>
        QuoteAsync(
            string tenantId,
            Guid shippingMethodId,
            CancellationToken cancellationToken = default);
}
