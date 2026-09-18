using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class InventoryService(
    IInventoryRepository repository,
    IInventoryUnitOfWork unitOfWork)
    : IInventoryService
{
    private const int MaxConcurrencyRetries = 5;
public async Task<IReadOnlyList<InventoryMovementDto>> GetMovementsAsync(
    string tenantId,
    Guid productVariantId,
    int skip,
    int take,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

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

        // جلوگیری از درخواست‌های غیرمنطقی و فشار ناخواسته به DB
        take = Math.Min(
            take,
            200);

        var movements =
            await repository.GetMovementsAsync(
                tenantId.Trim(),
                productVariantId,
                skip,
                take,
                cancellationToken);

        return movements
            .Select(
                movement =>
                    new InventoryMovementDto(
                        movement.Id,
                        movement.ProductVariantId,
                        movement.Type.ToString(),
                        movement.AvailableDelta,
                        movement.ReservedDelta,
                        movement.AvailableBalance,
                        movement.ReservedBalance,
                        movement.TotalBalance,
                        movement.ReferenceType,
                        movement.ReferenceId,
                        movement.Reason,
                        movement.OccurredAt))
            .ToList();
    }

    public async Task<StockDto?> GetStockAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        var stock =
            await repository.GetStockAsync(
                tenantId,
                productVariantId,
                cancellationToken);

        return stock is null
            ? null
            : Map(stock);
    }

    public async Task<StockReservationDto?> GetReservationAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (string.IsNullOrWhiteSpace(reservationKey))
        {
            throw new ArgumentException(
                "Reservation key is required.",
                nameof(reservationKey));
        }

        var normalizedKey =
            reservationKey.Trim();

        var reservation =
            await repository.GetReservationAsync(
                tenantId,
                normalizedKey,
                cancellationToken);

        return reservation is null
            ? null
            : Map(reservation);
    }

    public async Task<StockDto> SetStockAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        for (var attempt = 1;
             attempt <= MaxConcurrencyRetries;
             attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stock =
                await repository.GetStockAsync(
                    tenantId,
                    productVariantId,
                    cancellationToken);

            if (stock is null)
            {
                stock =
                    StockItem.Create(
                        tenantId,
                        productVariantId,
                        quantity);

                await repository.AddStockAsync(
                    stock,
                    cancellationToken);

                await repository.AddMovementAsync(
                    InventoryMovement.Create(
                        tenantId,
                        stock.Id,
                        productVariantId,
                        InventoryMovementType.OpeningBalance,
                        quantity,
                        0,
                        stock.AvailableQuantity,
                        stock.ReservedQuantity,
                        null,
                        null,
                        "Initial stock balance."),
                    cancellationToken);
            }
            else
            {
                if (stock.ReservedQuantity > quantity)
                {
                    throw new InvalidOperationException(
                        "New stock quantity cannot be lower than reserved quantity.");
                }

                var currentTotal =
                    stock.TotalQuantity;

                var difference =
                    quantity - currentTotal;

                if (difference == 0)
                {
                    return Map(stock);
                }

                if (difference > 0)
                {
                    stock.Add(
                        difference);
                }
                else
                {
                    stock.Remove(
                        -difference);
                }

                await repository.AddMovementAsync(
                    InventoryMovement.Create(
                        tenantId,
                        stock.Id,
                        productVariantId,
                        InventoryMovementType.Correction,
                        difference,
                        0,
                        stock.AvailableQuantity,
                        stock.ReservedQuantity,
                        null,
                        null,
                        "Stock quantity corrected."),
                    cancellationToken);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return Map(stock);
            }
            catch (DbUpdateConcurrencyException)
            {
                repository.ClearTracking();

                if (attempt == MaxConcurrencyRetries)
                {
                    throw new InvalidOperationException(
                        "Stock could not be updated because it is being updated concurrently.");
                }

                await DelayBeforeRetryAsync(
                    attempt,
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Stock update could not be completed.");
    }

    public async Task<StockDto> AdjustStockAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (quantity == 0)
        {
            var existing =
                await repository.GetStockAsync(
                    tenantId,
                    productVariantId,
                    cancellationToken);

            if (existing is null)
            {
                throw new InvalidOperationException(
                    "Stock record was not found.");
            }

            return Map(existing);
        }

        for (var attempt = 1;
             attempt <= MaxConcurrencyRetries;
             attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stock =
                await repository.GetStockAsync(
                    tenantId,
                    productVariantId,
                    cancellationToken);

            if (stock is null)
            {
                if (quantity < 0)
                {
                    throw new InvalidOperationException(
                        "Cannot reduce stock that does not exist.");
                }

                stock =
                    StockItem.Create(
                        tenantId,
                        productVariantId,
                        quantity);

                await repository.AddStockAsync(
                    stock,
                    cancellationToken);

                await repository.AddMovementAsync(
                    InventoryMovement.Create(
                        tenantId,
                        stock.Id,
                        productVariantId,
                        InventoryMovementType.AdjustmentIncrease,
                        quantity,
                        0,
                        stock.AvailableQuantity,
                        stock.ReservedQuantity,
                        "Adjustment",
                        null,
                        "Stock adjustment."),
                    cancellationToken);
            }
            else
            {
                if (quantity > 0)
                {
                    stock.Add(
                        quantity);
                }
                else
                {
                    stock.Remove(
                        -quantity);
                }

                var movementType =
                    quantity > 0
                        ? InventoryMovementType.AdjustmentIncrease
                        : InventoryMovementType.AdjustmentDecrease;

                await repository.AddMovementAsync(
                    InventoryMovement.Create(
                        tenantId,
                        stock.Id,
                        productVariantId,
                        movementType,
                        quantity,
                        0,
                        stock.AvailableQuantity,
                        stock.ReservedQuantity,
                        "Adjustment",
                        null,
                        "Stock adjustment."),
                    cancellationToken);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return Map(stock);
            }
            catch (DbUpdateConcurrencyException)
            {
                repository.ClearTracking();

                if (attempt == MaxConcurrencyRetries)
                {
                    throw new InvalidOperationException(
                        "Stock could not be adjusted because it is being updated concurrently.");
                }

                await DelayBeforeRetryAsync(
                    attempt,
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Stock adjustment could not be completed.");
    }

    public async Task<StockReservationDto> ReserveAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        string reservationKey,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        if (string.IsNullOrWhiteSpace(reservationKey))
        {
            throw new ArgumentException(
                "Reservation key is required.",
                nameof(reservationKey));
        }

        var normalizedKey =
            reservationKey.Trim();

        if (normalizedKey.Length > 128)
        {
            throw new ArgumentException(
                "Reservation key cannot exceed 128 characters.",
                nameof(reservationKey));
        }

        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiration));
        }

        for (var attempt = 1;
             attempt <= MaxConcurrencyRetries;
             attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing =
                await repository.GetReservationAsync(
                    tenantId,
                    normalizedKey,
                    cancellationToken);

            if (existing is not null)
            {
                if (existing.ProductVariantId != productVariantId ||
                    existing.Quantity != quantity)
                {
                    throw new InvalidOperationException(
                        "Reservation key is already used for another reservation.");
                }

                return Map(existing);
            }

            var stock =
                await repository.GetStockAsync(
                    tenantId,
                    productVariantId,
                    cancellationToken);

            if (stock is null)
            {
                throw new InvalidOperationException(
                    "Stock record was not found.");
            }

            var previousAvailable =
                stock.AvailableQuantity;

            var previousReserved =
                stock.ReservedQuantity;

            stock.Reserve(
                quantity);

            var reservation =
                StockReservation.Create(
                    tenantId,
                    normalizedKey,
                    productVariantId,
                    stock.Id,
                    quantity,
                    DateTimeOffset.UtcNow.Add(
                        expiration));

            await repository.AddReservationAsync(
                reservation,
                cancellationToken);

            await repository.AddMovementAsync(
                InventoryMovement.Create(
                    tenantId,
                    stock.Id,
                    productVariantId,
                    InventoryMovementType.Reservation,
                    -quantity,
                    quantity,
                    stock.AvailableQuantity,
                    stock.ReservedQuantity,
                    "Reservation",
                    normalizedKey,
                    $"Reservation moved {quantity} unit(s) from available to reserved. Previous available={previousAvailable}, previous reserved={previousReserved}."),
                cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return Map(reservation);
            }
            catch (DbUpdateConcurrencyException)
            {
                repository.ClearTracking();

                if (attempt == MaxConcurrencyRetries)
                {
                    throw new InvalidOperationException(
                        "Stock could not be reserved because it is being updated concurrently.");
                }

                await DelayBeforeRetryAsync(
                    attempt,
                    cancellationToken);
            }
            catch (DbUpdateException)
            {
                repository.ClearTracking();

                var persisted =
                    await repository.GetReservationAsync(
                        tenantId,
                        normalizedKey,
                        cancellationToken);

                if (persisted is not null)
                {
                    if (persisted.ProductVariantId !=
                            productVariantId ||
                        persisted.Quantity !=
                            quantity)
                    {
                        throw new InvalidOperationException(
                            "Reservation key is already used for another reservation.");
                    }

                    return Map(persisted);
                }

                throw;
            }
        }

        throw new InvalidOperationException(
            "Stock reservation could not be completed.");
    }

    public async Task<StockReservationDto> ReleaseAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (string.IsNullOrWhiteSpace(reservationKey))
        {
            throw new ArgumentException(
                "Reservation key is required.",
                nameof(reservationKey));
        }

        var normalizedKey =
            reservationKey.Trim();

        for (var attempt = 1;
             attempt <= MaxConcurrencyRetries;
             attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var reservation =
                await repository.GetReservationAsync(
                    tenantId,
                    normalizedKey,
                    cancellationToken);

            if (reservation is null)
            {
                throw new KeyNotFoundException(
                    "Reservation was not found.");
            }

            if (reservation.Status ==
                StockReservationStatus.Released)
            {
                return Map(reservation);
            }

            if (reservation.Status ==
                StockReservationStatus.Committed)
            {
                throw new InvalidOperationException(
                    "A committed reservation cannot be released.");
            }

            var stock =
                await repository.GetStockByIdAsync(
                    tenantId,
                    reservation.StockItemId,
                    cancellationToken);

            if (stock is null)
            {
                throw new InvalidOperationException(
                    "Stock record was not found.");
            }

            stock.Release(
                reservation.Quantity);

            var movementType =
                reservation.IsExpired
                    ? InventoryMovementType.ReservationRelease
                    : InventoryMovementType.ReservationRelease;

            if (reservation.IsExpired)
            {
                reservation.MarkExpired();
            }
            else
            {
                reservation.MarkReleased();
            }

            await repository.AddMovementAsync(
                InventoryMovement.Create(
                    tenantId,
                    stock.Id,
                    reservation.ProductVariantId,
                    movementType,
                    reservation.Quantity,
                    -reservation.Quantity,
                    stock.AvailableQuantity,
                    stock.ReservedQuantity,
                    "Reservation",
                    normalizedKey,
                    reservation.Status ==
                        StockReservationStatus.Expired
                        ? "Expired reservation released."
                        : "Reservation released."),
                cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return Map(reservation);
            }
            catch (DbUpdateConcurrencyException)
            {
                repository.ClearTracking();

                if (attempt == MaxConcurrencyRetries)
                {
                    throw new InvalidOperationException(
                        "Stock could not be released because it is being updated concurrently.");
                }

                await DelayBeforeRetryAsync(
                    attempt,
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Reservation release could not be completed.");
    }

    public async Task<StockReservationDto> CommitAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(
            tenantId);

        if (string.IsNullOrWhiteSpace(reservationKey))
        {
            throw new ArgumentException(
                "Reservation key is required.",
                nameof(reservationKey));
        }

        var normalizedKey =
            reservationKey.Trim();

        for (var attempt = 1;
             attempt <= MaxConcurrencyRetries;
             attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var reservation =
                await repository.GetReservationAsync(
                    tenantId,
                    normalizedKey,
                    cancellationToken);

            if (reservation is null)
            {
                throw new KeyNotFoundException(
                    "Reservation was not found.");
            }

            if (reservation.Status ==
                StockReservationStatus.Committed)
            {
                return Map(reservation);
            }

            if (!reservation.IsActive)
            {
                throw new InvalidOperationException(
                    "Only active reservations can be committed.");
            }

            if (reservation.IsExpired)
            {
                throw new InvalidOperationException(
                    "Expired reservation cannot be committed.");
            }

            var stock =
                await repository.GetStockByIdAsync(
                    tenantId,
                    reservation.StockItemId,
                    cancellationToken);

            if (stock is null)
            {
                throw new InvalidOperationException(
                    "Stock record was not found.");
            }

            stock.Commit(
                reservation.Quantity);

            reservation.MarkCommitted();

            await repository.AddMovementAsync(
                InventoryMovement.Create(
                    tenantId,
                    stock.Id,
                    reservation.ProductVariantId,
                    InventoryMovementType.ReservationCommit,
                    0,
                    -reservation.Quantity,
                    stock.AvailableQuantity,
                    stock.ReservedQuantity,
                    "Reservation",
                    normalizedKey,
                    "Reserved stock committed to the order."),
                cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return Map(reservation);
            }
            catch (DbUpdateConcurrencyException)
            {
                repository.ClearTracking();

                if (attempt == MaxConcurrencyRetries)
                {
                    throw new InvalidOperationException(
                        "Stock could not be committed because it is being updated concurrently.");
                }

                await DelayBeforeRetryAsync(
                    attempt,
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Reservation commit could not be completed.");
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

    private static async Task DelayBeforeRetryAsync(
        int attempt,
        CancellationToken cancellationToken)
    {
        var delayMilliseconds =
            attempt switch
            {
                1 => 75,
                2 => 150,
                3 => 300,
                4 => 500,
                _ => 750
            };

        await Task.Delay(
            delayMilliseconds,
            cancellationToken);
    }

    private static StockDto Map(
        StockItem stock)
    {
        return new StockDto(
            stock.ProductVariantId,
            stock.AvailableQuantity,
            stock.ReservedQuantity,
            stock.TotalQuantity);
    }

    private static StockReservationDto Map(
        StockReservation reservation)
    {
        return new StockReservationDto(
            reservation.ReservationKey,
            reservation.ProductVariantId,
            reservation.Quantity,
            reservation.Status.ToString(),
            reservation.ExpiresAt);
    }
}
