using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Repositories;

public sealed class InventoryRepository(
    InventoryDbContext context)
    : IInventoryRepository
{
    public async Task<StockItem?> GetStockAsync(
        string tenantId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        return await context.StockItems
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.ProductVariantId == productVariantId,
                cancellationToken);
    }

    public async Task<StockItem?> GetStockByIdAsync(
        string tenantId,
        Guid stockItemId,
        CancellationToken cancellationToken = default)
    {
        return await context.StockItems
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == stockItemId,
                cancellationToken);
    }

    public async Task<StockReservation?> GetReservationAsync(
        string tenantId,
        string reservationKey,
        CancellationToken cancellationToken = default)
    {
        return await context.StockReservations
            .Include(x => x.StockItem)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.ReservationKey == reservationKey,
                cancellationToken);
    }

    public async Task AddStockAsync(
        StockItem stockItem,
        CancellationToken cancellationToken = default)
    {
        await context.StockItems.AddAsync(
            stockItem,
            cancellationToken);
    }

    public async Task AddReservationAsync(
        StockReservation reservation,
        CancellationToken cancellationToken = default)
    {
        await context.StockReservations.AddAsync(
            reservation,
            cancellationToken);
    }
}