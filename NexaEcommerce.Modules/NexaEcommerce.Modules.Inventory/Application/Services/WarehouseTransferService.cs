using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class WarehouseTransferService(
    IWarehouseStockRepository stockRepository,
    IWarehouseRepository warehouseRepository,
    IInventoryUnitOfWork unitOfWork)
    : IWarehouseTransferService
{
public async Task<WarehouseTransferDto> TransferAsync(
    string tenantId,
    CreateWarehouseTransferRequest request,
    CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(
            request);

        var normalizedTenantId =
            tenantId.Trim();

        if (request.SourceWarehouseId ==
                request.DestinationWarehouseId &&
            request.SourceLocationId ==
                request.DestinationLocationId)
        {
            throw new InvalidOperationException(
                "Source and destination cannot be the same location.");
        }

        await EnsureWarehouseAndLocation(
            normalizedTenantId,
            request.SourceWarehouseId,
            request.SourceLocationId,
            cancellationToken);

        await EnsureWarehouseAndLocation(
            normalizedTenantId,
            request.DestinationWarehouseId,
            request.DestinationLocationId,
            cancellationToken);

        return await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var source =
                    await stockRepository.GetAsync(
                        normalizedTenantId,
                        request.SourceWarehouseId,
                        request.SourceLocationId,
                        request.ProductVariantId,
                        ct);

                if (source is null)
                {
                    throw new InvalidOperationException(
                        "Source warehouse stock was not found.");
                }

                if (source.AvailableQuantity <
                    request.Quantity)
                {
                    throw new InvalidOperationException(
                        "Insufficient available source stock.");
                }

                var destination =
                    await stockRepository.GetAsync(
                        normalizedTenantId,
                        request.DestinationWarehouseId,
                        request.DestinationLocationId,
                        request.ProductVariantId,
                        ct);

                if (destination is null)
                {
                    destination =
                        WarehouseStock.Create(
                            normalizedTenantId,
                            request.DestinationWarehouseId,
                            request.DestinationLocationId,
                            request.ProductVariantId);

                    await stockRepository.AddAsync(
                        destination,
                        ct);
                }

                source.RemoveOnHand(
                    request.Quantity);

                destination.AddOnHand(
                    request.Quantity);

                var transfer =
                    WarehouseTransfer.Create(
                        normalizedTenantId,
                        request.SourceWarehouseId,
                        request.SourceLocationId,
                        request.DestinationWarehouseId,
                        request.DestinationLocationId,
                        request.ProductVariantId,
                        request.Quantity,
                        request.Reason);

                var transferOut =
                    WarehouseStockMovement.Create(
                        normalizedTenantId,
                        request.SourceWarehouseId,
                        request.SourceLocationId,
                        request.ProductVariantId,
                        WarehouseStockMovementType.TransferOut,
                        -request.Quantity,
                        source.OnHandQuantity,
                        "WarehouseTransfer",
                        transfer.Id.ToString(),
                        request.Reason);

                var transferIn =
                    WarehouseStockMovement.Create(
                        normalizedTenantId,
                        request.DestinationWarehouseId,
                        request.DestinationLocationId,
                        request.ProductVariantId,
                        WarehouseStockMovementType.TransferIn,
                        request.Quantity,
                        destination.OnHandQuantity,
                        "WarehouseTransfer",
                        transfer.Id.ToString(),
                        request.Reason);

                transfer.Complete();

                await stockRepository.AddMovementAsync(
                    transferOut,
                    ct);

                await stockRepository.AddMovementAsync(
                    transferIn,
                    ct);

                await stockRepository.AddTransferAsync(
                    transfer,
                    ct);

                return Map(transfer);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseTransferDto>>
        GetTransfersAsync(
            string tenantId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skip));
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take));
        }

        take = Math.Min(
            take,
            200);

        var transfers =
            await stockRepository.GetTransfersAsync(
                tenantId.Trim(),
                skip,
                take,
                cancellationToken);

        return transfers
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<WarehouseStockMovementDto>>
        GetStockMovementsAsync(
            string tenantId,
            Guid warehouseId,
            Guid locationId,
            Guid productVariantId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ValidateGuid(
            warehouseId,
            nameof(warehouseId));

        ValidateGuid(
            locationId,
            nameof(locationId));

        ValidateGuid(
            productVariantId,
            nameof(productVariantId));

        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skip));
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take));
        }

        take = Math.Min(
            take,
            200);

        var movements =
            await stockRepository.GetMovementsAsync(
                tenantId.Trim(),
                warehouseId,
                locationId,
                productVariantId,
                skip,
                take,
                cancellationToken);

        return movements
            .Select(
                movement =>
                    new WarehouseStockMovementDto(
                        movement.Id,
                        movement.WarehouseId,
                        movement.LocationId,
                        movement.ProductVariantId,
                        movement.Type.ToString(),
                        movement.QuantityDelta,
                        movement.BalanceAfter,
                        movement.ReferenceType,
                        movement.ReferenceId,
                        movement.Reason,
                        movement.OccurredAt))
            .ToList();
    }

    private async Task EnsureWarehouseAndLocation(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var warehouse =
            await warehouseRepository.GetWarehouseAsync(
                tenantId,
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
                "Warehouse is inactive.");
        }

        var location =
            await warehouseRepository.GetLocationAsync(
                tenantId,
                warehouseId,
                locationId,
                cancellationToken);

        if (location is null)
        {
            throw new KeyNotFoundException(
                "Warehouse location was not found.");
        }

        if (!location.IsActive)
        {
            throw new InvalidOperationException(
                "Warehouse location is inactive.");
        }
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

    private static void ValidateGuid(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid identifier is required.",
                parameterName);
        }
    }

    private static void ValidateRequest(
        CreateWarehouseTransferRequest request)
    {
        ValidateGuid(
            request.SourceWarehouseId,
            nameof(request.SourceWarehouseId));

        ValidateGuid(
            request.SourceLocationId,
            nameof(request.SourceLocationId));

        ValidateGuid(
            request.DestinationWarehouseId,
            nameof(request.DestinationWarehouseId));

        ValidateGuid(
            request.DestinationLocationId,
            nameof(request.DestinationLocationId));

        ValidateGuid(
            request.ProductVariantId,
            nameof(request.ProductVariantId));

        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity));
        }
    }

    private static WarehouseTransferDto Map(
        WarehouseTransfer transfer)
    {
        return new WarehouseTransferDto(
            transfer.Id,
            transfer.SourceWarehouseId,
            transfer.SourceLocationId,
            transfer.DestinationWarehouseId,
            transfer.DestinationLocationId,
            transfer.ProductVariantId,
            transfer.Quantity,
            transfer.Status.ToString(),
            transfer.Reason,
            transfer.RequestedAt,
            transfer.CompletedAt);
    }
}