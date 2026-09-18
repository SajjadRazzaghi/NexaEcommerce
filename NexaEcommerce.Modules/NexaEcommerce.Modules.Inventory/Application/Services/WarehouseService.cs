using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class WarehouseService(
    IWarehouseRepository repository,
    IInventoryUnitOfWork unitOfWork)
    : IWarehouseService
{
    public async Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(
        string tenantId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var warehouses =
            await repository.GetWarehousesAsync(
                tenantId.Trim(),
                includeInactive,
                cancellationToken);

        return warehouses
            .Select(Map)
            .ToList();
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(
        string tenantId,
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        ArgumentNullException.ThrowIfNull(request);

        var normalizedTenantId =
            tenantId.Trim();

        ValidateRequiredText(
            request.Code,
            "Warehouse code");

        ValidateRequiredText(
            request.Name,
            "Warehouse name");

        var normalizedCode =
            request.Code.Trim().ToUpperInvariant();

        var duplicate =
            await repository.GetWarehouseByCodeAsync(
                normalizedTenantId,
                normalizedCode,
                cancellationToken);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                "A warehouse with the specified code already exists.");
        }

        var warehouse =
            Warehouse.Create(
                normalizedTenantId,
                normalizedCode,
                request.Name,
                request.AddressLine,
                request.City,
                request.PostalCode,
                request.Phone,
                request.IsDefault);

        if (request.IsDefault)
        {
            var currentDefaults =
                await repository.GetDefaultCandidatesAsync(
                    normalizedTenantId,
                    null,
                    cancellationToken);

            foreach (var currentDefault in currentDefaults)
            {
                currentDefault.SetDefault(false);
            }
        }

        await repository.AddWarehouseAsync(
            warehouse,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(warehouse);
    }

    public async Task<WarehouseDto> UpdateWarehouseAsync(
        string tenantId,
        Guid warehouseId,
        UpdateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        ArgumentNullException.ThrowIfNull(request);

        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse id is required.",
                nameof(warehouseId));
        }

        var normalizedTenantId =
            tenantId.Trim();

        var warehouse =
            await repository.GetWarehouseAsync(
                normalizedTenantId,
                warehouseId,
                cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException(
                "Warehouse was not found.");
        }

        ValidateRequiredText(
            request.Code,
            "Warehouse code");

        ValidateRequiredText(
            request.Name,
            "Warehouse name");

        var normalizedCode =
            request.Code.Trim().ToUpperInvariant();

        var duplicate =
            await repository.GetWarehouseByCodeAsync(
                normalizedTenantId,
                normalizedCode,
                cancellationToken);

        if (duplicate is not null &&
            duplicate.Id != warehouse.Id)
        {
            throw new InvalidOperationException(
                "A warehouse with the specified code already exists.");
        }

        warehouse.Update(
            normalizedCode,
            request.Name,
            request.AddressLine,
            request.City,
            request.PostalCode,
            request.Phone);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(warehouse);
    }

    public async Task<WarehouseDto> SetWarehouseStatusAsync(
        string tenantId,
        Guid warehouseId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var normalizedTenantId =
            tenantId.Trim();

        var warehouse =
            await repository.GetWarehouseAsync(
                normalizedTenantId,
                warehouseId,
                cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException(
                "Warehouse was not found.");
        }

        if (!isActive &&
            warehouse.IsDefault)
        {
            throw new InvalidOperationException(
                "The default warehouse cannot be deactivated. Set another warehouse as default first.");
        }

        warehouse.SetActive(
            isActive);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(warehouse);
    }

    public async Task<WarehouseDto> SetDefaultWarehouseAsync(
        string tenantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var normalizedTenantId =
            tenantId.Trim();

        var warehouse =
            await repository.GetWarehouseAsync(
                normalizedTenantId,
                warehouseId,
                cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException(
                "Warehouse was not found.");
        }

        if (!warehouse.IsActive)
        {
            throw new InvalidOperationException(
                "Only an active warehouse can be the default warehouse.");
        }

        var currentDefaults =
            await repository.GetDefaultCandidatesAsync(
                normalizedTenantId,
                warehouse.Id,
                cancellationToken);

        foreach (var currentDefault in currentDefaults)
        {
            currentDefault.SetDefault(false);
        }

        warehouse.SetDefault(true);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(warehouse);
    }

    public async Task<IReadOnlyList<WarehouseLocationDto>>
        GetLocationsAsync(
            string tenantId,
            Guid warehouseId,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse id is required.",
                nameof(warehouseId));
        }

        var normalizedTenantId =
            tenantId.Trim();

        var warehouse =
            await repository.GetWarehouseAsync(
                normalizedTenantId,
                warehouseId,
                cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException(
                "Warehouse was not found.");
        }

        var locations =
            await repository.GetLocationsAsync(
                normalizedTenantId,
                warehouseId,
                includeInactive,
                cancellationToken);

        return locations
            .Select(Map)
            .ToList();
    }

    public async Task<WarehouseLocationDto> CreateLocationAsync(
        string tenantId,
        CreateWarehouseLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        ArgumentNullException.ThrowIfNull(request);

        if (request.WarehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse id is required.",
                nameof(request.WarehouseId));
        }

        ValidateRequiredText(
            request.Code,
            "Location code");

        ValidateRequiredText(
            request.Name,
            "Location name");

        var normalizedTenantId =
            tenantId.Trim();

        var warehouse =
            await repository.GetWarehouseAsync(
                normalizedTenantId,
                request.WarehouseId,
                cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException(
                "Warehouse was not found.");
        }

        if (!warehouse.IsActive)
        {
            throw new InvalidOperationException(
                "Cannot create a location inside an inactive warehouse.");
        }

        var normalizedCode =
            request.Code.Trim().ToUpperInvariant();

        var duplicate =
            await repository.GetLocationByCodeAsync(
                normalizedTenantId,
                request.WarehouseId,
                normalizedCode,
                cancellationToken);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                "A location with the specified code already exists in this warehouse.");
        }

        var location =
            WarehouseLocation.Create(
                normalizedTenantId,
                request.WarehouseId,
                normalizedCode,
                request.Name,
                request.Zone,
                request.Rack,
                request.Shelf,
                request.Bin);

        await repository.AddLocationAsync(
            location,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(location);
    }

    public async Task<WarehouseLocationDto> UpdateLocationAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        UpdateWarehouseLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        ArgumentNullException.ThrowIfNull(request);

        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse id is required.",
                nameof(warehouseId));
        }

        if (locationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Location id is required.",
                nameof(locationId));
        }

        ValidateRequiredText(
            request.Code,
            "Location code");

        ValidateRequiredText(
            request.Name,
            "Location name");

        var normalizedTenantId =
            tenantId.Trim();

        var warehouse =
            await repository.GetWarehouseAsync(
                normalizedTenantId,
                warehouseId,
                cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException(
                "Warehouse was not found.");
        }

        var location =
            await repository.GetLocationAsync(
                normalizedTenantId,
                warehouseId,
                locationId,
                cancellationToken);

        if (location is null)
        {
            throw new KeyNotFoundException(
                "Location was not found.");
        }

        var normalizedCode =
            request.Code.Trim().ToUpperInvariant();

        var duplicate =
            await repository.GetLocationByCodeAsync(
                normalizedTenantId,
                warehouseId,
                normalizedCode,
                cancellationToken);

        if (duplicate is not null &&
            duplicate.Id != location.Id)
        {
            throw new InvalidOperationException(
                "A location with the specified code already exists in this warehouse.");
        }

        location.Update(
            normalizedCode,
            request.Name,
            request.Zone,
            request.Rack,
            request.Shelf,
            request.Bin);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(location);
    }

    public async Task<WarehouseLocationDto>
        SetLocationStatusAsync(
            string tenantId,
            Guid warehouseId,
            Guid locationId,
            bool isActive,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var normalizedTenantId =
            tenantId.Trim();

        var location =
            await repository.GetLocationAsync(
                normalizedTenantId,
                warehouseId,
                locationId,
                cancellationToken);

        if (location is null)
        {
            throw new KeyNotFoundException(
                "Location was not found.");
        }

        location.SetActive(
            isActive);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(location);
    }

    private static void ValidateTenant(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }

    private static void ValidateRequiredText(
        string? value,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{fieldName} is required.");
        }
    }

    private static WarehouseDto Map(
        Warehouse warehouse)
    {
        return new WarehouseDto(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.AddressLine,
            warehouse.City,
            warehouse.PostalCode,
            warehouse.Phone,
            warehouse.IsDefault,
            warehouse.IsActive,
            warehouse.CreatedAt,
            warehouse.UpdatedAt);
    }

    private static WarehouseLocationDto Map(
        WarehouseLocation location)
    {
        return new WarehouseLocationDto(
            location.Id,
            location.WarehouseId,
            location.Code,
            location.Name,
            location.Zone,
            location.Rack,
            location.Shelf,
            location.Bin,
            location.IsActive,
            location.CreatedAt,
            location.UpdatedAt);
    }
}