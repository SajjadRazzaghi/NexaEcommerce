using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using NexaEcommerce.Modules.Customers.Application.DTOs;
using NexaEcommerce.Modules.Customers.Domain.Entities;
using NexaEcommerce.Modules.Customers.Infrastructure.Persistence;
using NexaEcommerce.Modules.Customers.Infrastructure.Repositories;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.Utilities.Zlib;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using static Azure.Core.HttpHeader;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.Mime.MediaTypeNames;

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
        ValidateScope(tenantId, userId);

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
        ValidateScope(tenantId, userId);

        if (id == Guid.Empty)
            return null;

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

    public async Task<CustomerAddressDto>
        CreateAsync(
            string tenantId,
            string userId,
            CreateAddressRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateScope(tenantId, userId);
        ValidateRequest(request);

        var existing =
            await repository.GetForUserAsync(
                tenantId,
                userId,
                cancellationToken);

        var shouldBeDefault =
            request.IsDefault ||
            existing.Count == 0;

        if (shouldBeDefault)
        {
            foreach (var item in existing)
                item.ClearDefault();
        }

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
                shouldBeDefault);

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
        ValidateScope(tenantId, userId);
        ValidateRequest(request);

        if (id == Guid.Empty)
            return null;

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

        if (request.IsDefault)
        {
            foreach (var item in existing)
            {
                if (item.Id != id)
                    item.ClearDefault();
            }

            address.SetDefault();
        }
        else if (address.IsDefault)
        {
            var anotherAddressExists =
                existing.Any(x =>
                    x.Id != id);

            if (!anotherAddressExists)
            {
                address.SetDefault();
            }
            else
            {
                address.ClearDefault();

                var replacement =
                    existing
                        .Where(x => x.Id != id)
                        .OrderByDescending(x => x.IsDefault)
                        .ThenBy(x => x.Title)
                        .First();

                replacement.SetDefault();
            }
        }

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

    public async Task<bool>
        DeleteAsync(
            string tenantId,
            string userId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        ValidateScope(tenantId, userId);

        if (id == Guid.Empty)
            return false;

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

        var remaining =
            await repository.GetForUserAsync(
                tenantId,
                userId,
                cancellationToken);

        repository.Remove(address);

        if (wasDefault)
        {
            var replacement =
                remaining
                    .Where(x => x.Id != id)
                    .OrderBy(x => x.Title)
                    .FirstOrDefault();

            if (replacement is not null)
                replacement.SetDefault();
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<CustomerAddressDto?>
        SetDefaultAsync(
            string tenantId,
            string userId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        ValidateScope(tenantId, userId);

        if (id == Guid.Empty)
            return null;

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
            if (item.Id != id)
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

    private static void ValidateScope(
        string tenantId,
        string userId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
    }

    private static void ValidateRequest(
        CreateAddressRequest request)
    {
        ValidateText(request.Title, nameof(request.Title), 100);
        ValidateText(request.RecipientName, nameof(request.RecipientName), 200);
        ValidateText(request.PhoneNumber, nameof(request.PhoneNumber), 50);
        ValidateText(request.Country, nameof(request.Country), 100);
        ValidateText(request.Province, nameof(request.Province), 100);
        ValidateText(request.City, nameof(request.City), 100);
        ValidateText(request.AddressLine, nameof(request.AddressLine), 1000);

        if (request.PostalCode?.Length > 30)
        {
            throw new ArgumentException(
                "Postal code cannot exceed 30 characters.",
                nameof(request.PostalCode));
        }
    }

    private static void ValidateRequest(
        UpdateAddressRequest request)
    {
        ValidateText(request.Title, nameof(request.Title), 100);
        ValidateText(request.RecipientName, nameof(request.RecipientName), 200);
        ValidateText(request.PhoneNumber, nameof(request.PhoneNumber), 50);
        ValidateText(request.Country, nameof(request.Country), 100);
        ValidateText(request.Province, nameof(request.Province), 100);
        ValidateText(request.City, nameof(request.City), 100);
        ValidateText(request.AddressLine, nameof(request.AddressLine), 1000);

        if (request.PostalCode?.Length > 30)
        {
            throw new ArgumentException(
                "Postal code cannot exceed 30 characters.",
                nameof(request.PostalCode));
        }
    }

    private static void ValidateText(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }
    }
}
