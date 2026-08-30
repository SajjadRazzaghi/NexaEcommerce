using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaEcommerce.Modules.Inventory.Application.Services;

public sealed class InventoryService(
    IInventoryRepository repository,
    IInventoryUnitOfWork unitOfWork)
    : IInventoryService
   
{
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

    public async Task<StockDto> SetStockAsync(
        string tenantId,
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity));

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
                stock.Add(difference);
            else if (difference < 0)
                stock.Remove(-difference);
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
            stock.Add(quantity);
        }
        else if (quantity < 0)
        {
            stock.Remove(-quantity);
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
            throw new ArgumentOutOfRangeException(
                nameof(quantity));

        if (string.IsNullOrWhiteSpace(reservationKey))
            throw new ArgumentException(
                "Reservation key is required.",
                nameof(reservationKey));

        if (reservationKey.Length > 128)
            throw new ArgumentException(
                "Reservation key cannot exceed 128 characters.",
                nameof(reservationKey));

        if (expiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(expiration));

        var normalizedKey =
            reservationKey.Trim();

        var existing =
            await repository.GetReservationAsync(
                tenantId,
                normalizedKey,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.ProductVariantId !=
                productVariantId ||
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

        stock.Reserve(quantity);

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
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "Stock changed while the reservation was being created. Please retry.");
        }
        catch (DbUpdateException)
        {
            var persisted =
                await repository.GetReservationAsync(
                    tenantId,
                    normalizedKey,
                    cancellationToken);

            if (persisted is not null)
                return Map(persisted);

            throw;
        }

        return Map(reservation);
    }

    public async Task<StockReservationDto> ReleaseAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default)
    {
        var reservation =
            await repository.GetReservationAsync(
                tenantId,
                reservationKey,
                cancellationToken);

        if (reservation is null)
            throw new KeyNotFoundException(
                "Reservation was not found.");

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
            throw new InvalidOperationException(
                "Stock record was not found.");

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

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(reservation);
    }

    public async Task<StockReservationDto> CommitAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default)
    {
        var reservation =
            await repository.GetReservationAsync(
                tenantId,
                reservationKey,
                cancellationToken);

        if (reservation is null)
            throw new KeyNotFoundException(
                "Reservation was not found.");

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
            throw new InvalidOperationException(
                "Stock record was not found.");

        stock.Commit(
            reservation.Quantity);

        reservation.MarkCommitted();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(reservation);
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