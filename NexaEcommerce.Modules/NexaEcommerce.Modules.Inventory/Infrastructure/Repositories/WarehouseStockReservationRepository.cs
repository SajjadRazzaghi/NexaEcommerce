using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Persistence.Repositories;

public sealed class WarehouseStockReservationRepository(
    InventoryDbContext dbContext)
    : IWarehouseStockReservationRepository
{
    public async Task<WarehouseStockReservation?> GetAsync(
        string tenantId,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext
            .WarehouseStockReservations
            .FirstOrDefaultAsync(
                reservation =>
                    reservation.TenantId ==
                        tenantId &&
                    reservation.Id ==
                        reservationId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseStockReservation>>
        GetPendingByOrderAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext
            .WarehouseStockReservations
            .Where(
                reservation =>
                    reservation.TenantId ==
                        tenantId &&
                    reservation.OrderId ==
                        orderId &&
                    reservation.Status ==
                        WarehouseStockReservationStatus.Pending)
            .OrderBy(
                reservation =>
                    reservation.CreatedAt)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseStockReservation>>
        GetExpiredPendingAsync(
            DateTimeOffset utcNow,
            int take,
            CancellationToken cancellationToken = default)
    {
        return await dbContext
            .WarehouseStockReservations
            .Where(
                reservation =>
                    reservation.Status ==
                        WarehouseStockReservationStatus.Pending &&
                    reservation.ExpiresAt <=
                        utcNow)
            .OrderBy(
                reservation =>
                    reservation.ExpiresAt)
            .Take(take)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        WarehouseStockReservation reservation,
        CancellationToken cancellationToken = default)
    {
        await dbContext
            .WarehouseStockReservations
            .AddAsync(
                reservation,
                cancellationToken);
    }
}