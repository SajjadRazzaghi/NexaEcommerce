using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class WarehouseStockReservationService(
    IWarehouseStockReservationRepository reservationRepository,
    IWarehouseStockRepository stockRepository,
    IWarehouseRepository warehouseRepository,
    IInventoryUnitOfWork unitOfWork)
    : IWarehouseStockReservationService
{
    public async Task<WarehouseStockReservationDto> ReserveAsync(
        string tenantId,
        ReserveWarehouseStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        ArgumentNullException.ThrowIfNull(
            request);

        ValidateIds(
            request.WarehouseId,
            request.LocationId,
            request.ProductVariantId,
            request.OrderId);

        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity));
        }

        if (request.DurationMinutes <= 0 ||
            request.DurationMinutes > 1440)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.DurationMinutes),
                "Reservation duration must be between 1 and 1440 minutes.");
        }

        var normalizedTenantId =
            tenantId.Trim();

        await EnsureWarehouseAndLocationAsync(
            normalizedTenantId,
            request.WarehouseId,
            request.LocationId,
            cancellationToken);

        /*
         * IMPORTANT:
         * The stock row must be loaded for update inside
         * the same database transaction used to change it.
         *
         * Use the existing repository transaction/locking
         * mechanism in WarehouseStockRepository.
         */
        await unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var stock =
                await stockRepository.GetForUpdateAsync(
                    normalizedTenantId,
                    request.WarehouseId,
                    request.LocationId,
                    request.ProductVariantId,
                    cancellationToken);

            if (stock is null)
            {
                throw new InvalidOperationException(
                    "Warehouse stock does not exist for the selected product variant and location.");
            }

            if (stock.AvailableQuantity <
                request.Quantity)
            {
                throw new InvalidOperationException(
                    "Insufficient available stock.");
            }

            stock.Reserve(
                request.Quantity);

            var reservation =
                WarehouseStockReservation.Create(
                    normalizedTenantId,
                    request.WarehouseId,
                    request.LocationId,
                    request.ProductVariantId,
                    request.OrderId,
                    request.Quantity,
                    TimeSpan.FromMinutes(
                        request.DurationMinutes));

            await reservationRepository.AddAsync(
                reservation,
                cancellationToken);

            var movement =
                WarehouseStockMovement.Create(
                    normalizedTenantId,
                    stock.WarehouseId,
                    stock.LocationId,
                    stock.ProductVariantId,
                    WarehouseStockMovementType.Reservation,
                    request.Quantity,
                    stock.OnHandQuantity,
                    "WarehouseStockReservation",
                    reservation.Id.ToString(),
                    "Stock reserved for order.");

            await stockRepository.AddMovementAsync(
                movement,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            await unitOfWork.CommitTransactionAsync(
                cancellationToken);

            return Map(reservation);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<WarehouseStockReservationDto>
        ReleaseAsync(
            string tenantId,
            Guid reservationId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (reservationId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation id is required.",
                nameof(reservationId));
        }

        var normalizedTenantId =
            tenantId.Trim();

        await unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var reservation =
                await reservationRepository.GetAsync(
                    normalizedTenantId,
                    reservationId,
                    cancellationToken);

            if (reservation is null)
            {
                throw new KeyNotFoundException(
                    "Reservation was not found.");
            }

            if (reservation.Status !=
                WarehouseStockReservationStatus.Pending)
            {
                return Map(reservation);
            }

            var stock =
                await stockRepository.GetForUpdateAsync(
                    normalizedTenantId,
                    reservation.WarehouseId,
                    reservation.LocationId,
                    reservation.ProductVariantId,
                    cancellationToken);

            if (stock is null)
            {
                throw new InvalidOperationException(
                    "The reserved stock record was not found.");
            }

            stock.Release
                reservation.Quantity);

            reservation.MarkReleased();

            var movement =
                WarehouseStockMovement.Create(
                    normalizedTenantId,
                    stock.WarehouseId,
                    stock.LocationId,
                    stock.ProductVariantId,
                    WarehouseStockMovementType.ReservationRelease,
                    -reservation.Quantity,
                    stock.OnHandQuantity,
                    "WarehouseStockReservation",
                    reservation.Id.ToString(),
                    "Reserved stock released.");

            await stockRepository.AddMovementAsync(
                movement,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            await unitOfWork.CommitTransactionAsync(
                cancellationToken);

            return Map(reservation);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<WarehouseStockReservationDto>
        CommitAsync(
            string tenantId,
            Guid reservationId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (reservationId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation id is required.",
                nameof(reservationId));
        }

        var normalizedTenantId =
            tenantId.Trim();

        await unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var reservation =
                await reservationRepository.GetAsync(
                    normalizedTenantId,
                    reservationId,
                    cancellationToken);

            if (reservation is null)
            {
                throw new KeyNotFoundException(
                    "Reservation was not found.");
            }

            if (reservation.Status ==
                WarehouseStockReservationStatus.Committed)
            {
                return Map(reservation);
            }

            if (reservation.Status !=
                WarehouseStockReservationStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"Reservation cannot be committed because it is {reservation.Status}.");
            }

            if (reservation.ExpiresAt <=
                DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException(
                    "Reservation has expired.");
            }

            var stock =
                await stockRepository.GetForUpdateAsync(
                    normalizedTenantId,
                    reservation.WarehouseId,
                    reservation.LocationId,
                    reservation.ProductVariantId,
                    cancellationToken);

            if (stock is null)
            {
                throw new InvalidOperationException(
                    "Reserved stock record was not found.");
            }

            stock.ConsumeReserved(
                reservation.Quantity);

            reservation.MarkCommitted();

            var movement =
                WarehouseStockMovement.Create(
                    normalizedTenantId,
                    stock.WarehouseId,
                    stock.LocationId,
                    stock.ProductVariantId,
                    WarehouseStockMovementType.Sale,
                    -reservation.Quantity,
                    stock.OnHandQuantity,
                    "WarehouseStockReservation",
                    reservation.Id.ToString(),
                    "Reserved stock committed to sale.");

            await stockRepository.AddMovementAsync(
                movement,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            await unitOfWork.CommitTransactionAsync(
                cancellationToken);

            return Map(reservation);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<int> ExpirePendingAsync(
        CancellationToken cancellationToken = default)
    {
        var reservations =
            await reservationRepository.GetExpiredPendingAsync(
                DateTimeOffset.UtcNow,
                100,
                cancellationToken);

        var affected = 0;

        foreach (
            var reservation
            in reservations)
        {
            try
            {
                await ReleaseExpiredAsync(
                    reservation,
                    cancellationToken);

                affected++;
            }
            catch
            {
                /*
                 * Expiry processing must continue for
                 * the remaining reservations.
                 */
            }
        }

        return affected;
    }

    private async Task ReleaseExpiredAsync(
        WarehouseStockReservation reservation,
        CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var stock =
                await stockRepository.GetForUpdateAsync(
                    reservation.TenantId,
                    reservation.WarehouseId,
                    reservation.LocationId,
                    reservation.ProductVariantId,
                    cancellationToken);

            if (stock is null)
            {
                throw new InvalidOperationException(
                    "Reserved stock record was not found.");
            }

            if (reservation.Status ==
                WarehouseStockReservationStatus.Pending)
            {
                stock.Release
                    reservation.Quantity);

                reservation.MarkExpired();

                var movement =
                    WarehouseStockMovement.Create(
                        reservation.TenantId,
                        stock.WarehouseId,
                        stock.LocationId,
                        stock.ProductVariantId,
                        WarehouseStockMovementType.ReservationRelease,
                        -reservation.Quantity,
                        stock.OnHandQuantity,
                        "WarehouseStockReservation",
                        reservation.Id.ToString(),
                        "Expired reservation released.");

                await stockRepository.AddMovementAsync(
                    movement,
                    cancellationToken);

                await unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }

            await unitOfWork.CommitTransactionAsync(
                cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task EnsureWarehouseAndLocationAsync(
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

        if (warehouse is null ||
            !warehouse.IsActive)
        {
            throw new InvalidOperationException(
                "Warehouse is missing or inactive.");
        }

        var location =
            await warehouseRepository.GetLocationAsync(
                tenantId,
                warehouseId,
                locationId,
                cancellationToken);

        if (location is null ||
            !location.IsActive)
        {
            throw new InvalidOperationException(
                "Warehouse location is missing or inactive.");
        }
    }

    private static void ValidateTenant(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(
            tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }

    private static void ValidateIds(
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        Guid orderId)
    {
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

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }
    }

    private static WarehouseStockReservationDto Map(
        WarehouseStockReservation reservation)
    {
        return new WarehouseStockReservationDto(
            reservation.Id,
            reservation.WarehouseId,
            reservation.LocationId,
            reservation.ProductVariantId,
            reservation.OrderId,
            reservation.Quantity,
            reservation.Status.ToString(),
            reservation.ExpiresAt,
            reservation.CreatedAt,
            reservation.ReleasedAt,
            reservation.CommittedAt);
    }
}