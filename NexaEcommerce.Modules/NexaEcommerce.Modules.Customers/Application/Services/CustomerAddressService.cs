using NexaEcommerce.Modules.Customers.Application.DTOs;
using NexaEcommerce.Modules.Customers.Domain.Entities;
using NexaEcommerce.Modules.Customers.Infrastructure.Repositories;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaEcommerce.Modules.Customers.Application.Services;

public sealed class CustomerAddressService(
    ICustomerAddressRepository repository,
    ICustomerUnitOfWork unitOfWork)
    : ICustomerAddressService
{
    public async Task<IReadOnlyList<CustomerAddressDto>>
        GetAllAsync(
            string tenantId,
            string userId,
            CancellationToken cancellationToken = default)
    {
        var addresses =
            await repository.GetForUserAsync(
                tenantId,
                userId,
                cancellationToken);

        return addresses
            .Select(Map)
            .ToList();
    }

    public async Task<CustomerAddressDto?>
        GetAsync(
            string tenantId,
            string userId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var address =
            await repository.GetByIdAsync(
                tenantId,
                userId,
                id,
                cancellationToken);

        return address is null
            ? null
            : Map(address);
    }

    public async Task<CustomerAddressDto> CreateAsync(
        string tenantId,
        string userId,
        CreateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing =
            await repository.GetForUserAsync(
                tenantId,
                userId,
                cancellationToken);

        var address =
            CustomerAddress.Create(
                tenantId,
                userId,
                request.Title,
                request.RecipientName,
                request.PhoneNumber,
                request.Country,
                request.Province,
                request.City,
                request.AddressLine,
                request.PostalCode,
                isDefault:
                    request.IsDefault ||
                    existing.Count == 0);

        if (address.IsDefault)
        {
            foreach (var item in existing)
                item.ClearDefault();
        }

        await repository.AddAsync(
            address,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(address);
    }

    public async Task<CustomerAddressDto?>
        UpdateAsync(
            string tenantId,
            string userId,
            Guid id,
            UpdateAddressRequest request,
            CancellationToken cancellationToken = default)
    {
        var address =
            await repository.GetByIdAsync(
                tenantId,
                userId,
                id,
                cancellationToken);

        if (address is null)
            return null;

        address.Update(
            request.Title,
            request.RecipientName,
            request.PhoneNumber,
            request.Country,
            request.Province,
            request.City,
            request.AddressLine,
            request.PostalCode);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(address);
    }

    public async Task<bool> DeleteAsync(
        string tenantId,
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var address =
            await repository.GetByIdAsync(
                tenantId,
                userId,
                id,
                cancellationToken);

        if (address is null)
            return false;

        var wasDefault =
            address.IsDefault;

        repository.Remove(address);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        if (wasDefault)
        {
            var remaining =
                await repository.GetForUserAsync(
                    tenantId,
                    userId,
                    cancellationToken);

            var next =
                remaining.FirstOrDefault();

            if (next is not null)
            {
                next.SetDefault();

                await unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
        }

        return true;
    }

    public async Task<CustomerAddressDto?>
        SetDefaultAsync(
            string tenantId,
            string userId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var address =
            await repository.GetByIdAsync(
                tenantId,
                userId,
                id,
                cancellationToken);

        if (address is null)
            return null;

        var existing =
            await repository.GetForUserAsync(
                tenantId,
                userId,
                cancellationToken);

        foreach (var item in existing)
        {
            if (item.Id == id)
                continue;

            item.ClearDefault();
        }

        address.SetDefault();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(address);
    }

    private static CustomerAddressDto Map(
        CustomerAddress address)
    {
        return new CustomerAddressDto(
            address.Id,
            address.Title,
            address.RecipientName,
            address.PhoneNumber,
            address.Country,
            address.Province,
            address.City,
            address.AddressLine,
            address.PostalCode,
            address.IsDefault);
    }
}