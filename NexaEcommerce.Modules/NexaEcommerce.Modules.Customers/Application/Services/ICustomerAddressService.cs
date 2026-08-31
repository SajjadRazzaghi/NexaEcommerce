using NexaEcommerce.Modules.Customers.Application.DTOs;

namespace NexaEcommerce.Modules.Customers.Application.Services;

public interface ICustomerAddressService
{
    Task<IReadOnlyList<CustomerAddressDto>> GetAllAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto?> GetAsync(
        string tenantId,
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto> CreateAsync(
        string tenantId,
        string userId,
        CreateAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto?> UpdateAsync(
        string tenantId,
        string userId,
        Guid id,
        UpdateAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string tenantId,
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto?> SetDefaultAsync(
        string tenantId,
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
