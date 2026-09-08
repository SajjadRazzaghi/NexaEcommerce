using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Inventory.Infrastructure.Repositories;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class InventoryService(
    IInventoryRepository repository,
    IInventoryUnitOfWork unitOfWork)
    : IInventoryService
{
    private const int MaxConcurrencyRetries = 5;

    public async Task<StockDto?> GetStockAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
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
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

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
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

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

            if (difference > 0)
            {
                stock.Add(
                    difference);
            }
            else if (difference < 0)
            {
                stock.Remove(
                    -difference);
            }
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(stock);
    }

    public async Task<StockDto> AdjustStockAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
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
        }
        else if (quantity > 0)
        {
            stock.Add(
                quantity);
        }
        else if (quantity < 0)
        {
            stock.Remove(
                -quantity);
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(stock);
    }

    public async Task<StockReservationDto> ReserveAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        string reservationKey,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
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

            /*
             * Always query again after a concurrency conflict.
             * The previous DbContext state must never be reused.
             */
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

            /*
             * Reserve() validates available stock and increments
             * the application-managed Version concurrency token.
             */
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

            try
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return Map(reservation);
            }
            catch (DbUpdateConcurrencyException)
            {
                /*
                 * EF Core cannot safely retry with the entities that
                 * participated in the failed SaveChanges operation.
                 *
                 * Clear them completely, reload from the database,
                 * and retry the business operation.
                 */
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
                /*
                 * Another request may have created the same
                 * idempotent reservation between our existence check
                 * and INSERT.
                 */
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

            if (reservation.IsExpired)
            {
                stock.Release(
                    reservation.Quantity);

                reservation.MarkExpired();
            }
            else
            {
                stock.Release(
                    reservation.Quantity);

                reservation.MarkReleased();
            }

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

