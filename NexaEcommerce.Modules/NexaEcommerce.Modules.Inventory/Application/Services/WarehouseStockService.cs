using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class WarehouseStockService(
    IWarehouseStockRepository stockRepository,
    IWarehouseRepository warehouseRepository,
    IInventoryUnitOfWork unitOfWork)
    : IWarehouseStockService
{
    public async Task<WarehouseStockDto?> GetAsync(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
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

        var stock =
            await stockRepository.GetAsync(
                tenantId.Trim(),
                warehouseId,
                locationId,
                productVariantId,
                cancellationToken);

        return stock is null
            ? null
            : Map(stock);
    }

    public async Task<IReadOnlyList<WarehouseStockDto>>
        GetByWarehouseAsync(
            string tenantId,
            Guid warehouseId,
            Guid? locationId = null,
            Guid? productVariantId = null,
            bool includeZeroStock = true,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ValidateGuid(
            warehouseId,
            nameof(warehouseId));

        if (locationId.HasValue)
        {
            ValidateGuid(
                locationId.Value,
                nameof(locationId));
        }

        if (productVariantId.HasValue)
        {
            ValidateGuid(
                productVariantId.Value,
                nameof(productVariantId));
        }

        await EnsureWarehouseExists(
            tenantId.Trim(),
            warehouseId,
            cancellationToken);

        var stocks =
            await stockRepository.GetByWarehouseAsync(
                tenantId.Trim(),
                warehouseId,
                locationId,
                productVariantId,
                includeZeroStock,
                cancellationToken);

        return stocks
            .Select(Map)
            .ToList();
    }

    public async Task<WarehouseStockDto> CreateAsync(
        string tenantId,
        CreateWarehouseStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ArgumentNullException.ThrowIfNull(request);

        ValidateRequestIds(
            request.WarehouseId,
            request.LocationId,
            request.ProductVariantId);

        ValidateNonNegative(
            request.OnHandQuantity,
            nameof(request.OnHandQuantity));

        ValidateNonNegative(
            request.ReservedQuantity,
            nameof(request.ReservedQuantity));

        ValidateNonNegative(
            request.IncomingQuantity,
            nameof(request.IncomingQuantity));

        ValidateNonNegative(
            request.DamagedQuantity,
            nameof(request.DamagedQuantity));

        ValidateNonNegative(
            request.ReorderPoint,
            nameof(request.ReorderPoint));

        if (request.ReservedQuantity >
            request.OnHandQuantity)
        {
            throw new InvalidOperationException(
                "Reserved quantity cannot exceed on-hand quantity.");
        }

        var normalizedTenantId =
            tenantId.Trim();

        await EnsureWarehouseExists(
            normalizedTenantId,
            request.WarehouseId,
            cancellationToken);

        await EnsureLocationExists(
            normalizedTenantId,
            request.WarehouseId,
            request.LocationId,
            cancellationToken);

        var existing =
            await stockRepository.GetAsync(
                normalizedTenantId,
                request.WarehouseId,
                request.LocationId,
                request.ProductVariantId,
                cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                "Warehouse stock already exists for this product variant at the specified location.");
        }

        var stock =
            WarehouseStock.Create(
                normalizedTenantId,
                request.WarehouseId,
                request.LocationId,
                request.ProductVariantId,
                request.OnHandQuantity,
                request.ReservedQuantity,
                request.IncomingQuantity,
                request.DamagedQuantity,
                request.ReorderPoint);

        await stockRepository.AddAsync(
            stock,
            cancellationToken);

        if (request.OnHandQuantity > 0)
        {
            await AddMovementAsync(
                normalizedTenantId,
                stock,
                WarehouseStockMovementType.OpeningBalance,
                request.OnHandQuantity,
                "WarehouseStock.Create",
                stock.Id.ToString(),
                "Opening warehouse stock.",
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(stock);
    }

    public async Task<WarehouseStockDto> SetAsync(
        string tenantId,
        SetWarehouseStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ArgumentNullException.ThrowIfNull(request);

        ValidateRequestIds(
            request.WarehouseId,
            request.LocationId,
            request.ProductVariantId);

        ValidateNonNegative(
            request.OnHandQuantity,
            nameof(request.OnHandQuantity));

        ValidateNonNegative(
            request.ReservedQuantity,
            nameof(request.ReservedQuantity));

        ValidateNonNegative(
            request.IncomingQuantity,
            nameof(request.IncomingQuantity));

        ValidateNonNegative(
            request.DamagedQuantity,
            nameof(request.DamagedQuantity));

        ValidateNonNegative(
            request.ReorderPoint,
            nameof(request.ReorderPoint));

        if (request.ReservedQuantity >
            request.OnHandQuantity)
        {
            throw new InvalidOperationException(
                "Reserved quantity cannot exceed on-hand quantity.");
        }

        var normalizedTenantId =
            tenantId.Trim();

        await EnsureWarehouseExists(
            normalizedTenantId,
            request.WarehouseId,
            cancellationToken);

        await EnsureLocationExists(
            normalizedTenantId,
            request.WarehouseId,
            request.LocationId,
            cancellationToken);

        var stock =
            await stockRepository.GetAsync(
                normalizedTenantId,
                request.WarehouseId,
                request.LocationId,
                request.ProductVariantId,
                cancellationToken);

        var wasCreated =
            stock is null;

        var previousOnHand =
            stock?.OnHandQuantity ?? 0;

        if (stock is null)
        {
            stock =
                WarehouseStock.Create(
                    normalizedTenantId,
                    request.WarehouseId,
                    request.LocationId,
                    request.ProductVariantId);

            await stockRepository.AddAsync(
                stock,
                cancellationToken);
        }

        stock.SetQuantities(
            request.OnHandQuantity,
            request.ReservedQuantity,
            request.IncomingQuantity,
            request.DamagedQuantity);

        stock.SetReorderPoint(
            request.ReorderPoint);

        var onHandDelta =
            request.OnHandQuantity -
            previousOnHand;

        if (onHandDelta != 0)
        {
            await AddMovementAsync(
                normalizedTenantId,
                stock,
                wasCreated
                    ? WarehouseStockMovementType.OpeningBalance
                    : WarehouseStockMovementType.Correction,
                onHandDelta,
                "WarehouseStock.Set",
                stock.Id.ToString(),
                wasCreated
                    ? "Opening warehouse stock."
                    : "Manual warehouse stock correction.",
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(stock);
    }

    public async Task<WarehouseStockDto> AdjustAsync(
        string tenantId,
        AdjustWarehouseStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ArgumentNullException.ThrowIfNull(request);

        ValidateRequestIds(
            request.WarehouseId,
            request.LocationId,
            request.ProductVariantId);

        if (request.Quantity == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity),
                "Adjustment quantity cannot be zero.");
        }

        var normalizedTenantId =
            tenantId.Trim();

        await EnsureWarehouseExists(
            normalizedTenantId,
            request.WarehouseId,
            cancellationToken);

        await EnsureLocationExists(
            normalizedTenantId,
            request.WarehouseId,
            request.LocationId,
            cancellationToken);

        var stock =
            await GetOrCreateStock(
                normalizedTenantId,
                request.WarehouseId,
                request.LocationId,
                request.ProductVariantId,
                cancellationToken);

        if (request.Quantity > 0)
        {
            stock.AddOnHand(
                request.Quantity);

            await AddMovementAsync(
                normalizedTenantId,
                stock,
                WarehouseStockMovementType.AdjustmentIncrease,
                request.Quantity,
                "WarehouseStock.Adjust",
                stock.Id.ToString(),
                request.Reason,
                cancellationToken);
        }
        else
        {
            var positiveQuantity =
                checked(-request.Quantity);

            stock.RemoveOnHand(
                positiveQuantity);

            await AddMovementAsync(
                normalizedTenantId,
                stock,
                WarehouseStockMovementType.AdjustmentDecrease,
                -positiveQuantity,
                "WarehouseStock.Adjust",
                stock.Id.ToString(),
                request.Reason,
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(stock);
    }

    public async Task<WarehouseStockDto> ReceiveAsync(
        string tenantId,
        ReceiveWarehouseStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ArgumentNullException.ThrowIfNull(request);

        ValidateRequestIds(
            request.WarehouseId,
            request.LocationId,
            request.ProductVariantId);

        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity),
                "Receive quantity must be greater than zero.");
        }

        var normalizedTenantId =
            tenantId.Trim();

        await EnsureWarehouseExists(
            normalizedTenantId,
            request.WarehouseId,
            cancellationToken);

        await EnsureLocationExists(
            normalizedTenantId,
            request.WarehouseId,
            request.LocationId,
            cancellationToken);

        var stock =
            await GetOrCreateStock(
                normalizedTenantId,
                request.WarehouseId,
                request.LocationId,
                request.ProductVariantId,
                cancellationToken);

        var remaining =
            request.Quantity;

        if (stock.IncomingQuantity > 0)
        {
            var receiveFromIncoming =
                Math.Min(
                    remaining,
                    stock.IncomingQuantity);

            if (receiveFromIncoming > 0)
            {
                stock.ReceiveIncoming(
                    receiveFromIncoming);

                remaining -=
                    receiveFromIncoming;
            }
        }

        if (remaining > 0)
        {
            stock.AddOnHand(
                remaining);
        }

        await AddMovementAsync(
            normalizedTenantId,
            stock,
            WarehouseStockMovementType.Receive,
            request.Quantity,
            "WarehouseStock.Receive",
            stock.Id.ToString(),
            request.Reason,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(stock);
    }

    public async Task<WarehouseStockDto> DamageAsync(
        string tenantId,
        DamageWarehouseStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ArgumentNullException.ThrowIfNull(request);

        ValidateRequestIds(
            request.WarehouseId,
            request.LocationId,
            request.ProductVariantId);

        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity),
                "Damage quantity must be greater than zero.");
        }

        var normalizedTenantId =
            tenantId.Trim();

        await EnsureWarehouseExists(
            normalizedTenantId,
            request.WarehouseId,
            cancellationToken);

        await EnsureLocationExists(
            normalizedTenantId,
            request.WarehouseId,
            request.LocationId,
            cancellationToken);

        var stock =
            await GetOrCreateStock(
                normalizedTenantId,
                request.WarehouseId,
                request.LocationId,
                request.ProductVariantId,
                cancellationToken);

        stock.RemoveOnHand(
            request.Quantity);

        stock.AddDamage(
            request.Quantity);

        await AddMovementAsync(
            normalizedTenantId,
            stock,
            WarehouseStockMovementType.Damage,
            -request.Quantity,
            "WarehouseStock.Damage",
            stock.Id.ToString(),
            request.Reason,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(stock);
    }

    public async Task<WarehouseStockDto>
        SetReorderPointAsync(
            string tenantId,
            SetWarehouseReorderPointRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ArgumentNullException.ThrowIfNull(request);

        ValidateRequestIds(
            request.WarehouseId,
            request.LocationId,
            request.ProductVariantId);

        ValidateNonNegative(
            request.ReorderPoint,
            nameof(request.ReorderPoint));

        var normalizedTenantId =
            tenantId.Trim();

        await EnsureWarehouseExists(
            normalizedTenantId,
            request.WarehouseId,
            cancellationToken);

        await EnsureLocationExists(
            normalizedTenantId,
            request.WarehouseId,
            request.LocationId,
            cancellationToken);

        var stock =
            await GetOrCreateStock(
                normalizedTenantId,
                request.WarehouseId,
                request.LocationId,
                request.ProductVariantId,
                cancellationToken);

        stock.SetReorderPoint(
            request.ReorderPoint);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(stock);
    }

    private async Task<WarehouseStock> GetOrCreateStock(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        CancellationToken cancellationToken)
    {
        var stock =
            await stockRepository.GetAsync(
                tenantId,
                warehouseId,
                locationId,
                productVariantId,
                cancellationToken);

        if (stock is not null)
        {
            return stock;
        }

        stock =
            WarehouseStock.Create(
                tenantId,
                warehouseId,
                locationId,
                productVariantId);

        await stockRepository.AddAsync(
            stock,
            cancellationToken);

        return stock;
    }

    private async Task AddMovementAsync(
        string tenantId,
        WarehouseStock stock,
        WarehouseStockMovementType type,
        int quantityDelta,
        string referenceType,
        string referenceId,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (quantityDelta == 0)
        {
            return;
        }

        var movement =
            WarehouseStockMovement.Create(
                tenantId,
                stock.WarehouseId,
                stock.LocationId,
                stock.ProductVariantId,
                type,
                quantityDelta,
                stock.OnHandQuantity,
                referenceType,
                referenceId,
                reason);

        await stockRepository.AddMovementAsync(
            movement,
            cancellationToken);
    }

    private async Task EnsureWarehouseExists(
        string tenantId,
        Guid warehouseId,
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
    }

    private async Task EnsureLocationExists(
        string tenantId,
        Guid warehouseId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
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

    private static void ValidateRequestIds(
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId)
    {
        ValidateGuid(
            warehouseId,
            nameof(warehouseId));

        ValidateGuid(
            locationId,
            nameof(locationId));

        ValidateGuid(
            productVariantId,
            nameof(productVariantId));
    }

    private static void ValidateNonNegative(
        int value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName);
        }
    }

    private static WarehouseStockDto Map(
        WarehouseStock stock)
    {
        return new WarehouseStockDto(
            stock.Id,
            stock.WarehouseId,
            stock.LocationId,
            stock.ProductVariantId,
            stock.OnHandQuantity,
            stock.ReservedQuantity,
            stock.AvailableQuantity,
            stock.IncomingQuantity,
            stock.DamagedQuantity,
            stock.ReorderPoint,
            stock.IsLowStock,
            stock.Version,
            stock.CreatedAt,
            stock.UpdatedAt);
    }
}