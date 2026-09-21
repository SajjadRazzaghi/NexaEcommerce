using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Repositories;

public sealed class WarehouseStockReservationRepository(
    InventoryDbContext context)
    : IWarehouseStockReservationRepository
{
    public async Task<IReadOnlyList<WarehouseStockReservation>>
        GetByOrderAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        return await context.WarehouseStockReservations
           
            .Where(
                x =>
                    x.TenantId == tenantId &&
                    x.OrderId == orderId)
            .OrderBy(
                x => x.ProductVariantId)
            .ThenBy(
                x => x.LocationId)
            .ThenBy(
                x => x.Id)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseStockReservation>>
        GetActiveByOrderAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        return await context.WarehouseStockReservations
            .Where(
                x =>
                    x.TenantId == tenantId &&
                    x.OrderId == orderId &&
                    x.Status ==
                        WarehouseStockReservationStatus.Reserved)
            .OrderBy(
                x => x.ProductVariantId)
            .ThenBy(
                x => x.LocationId)
            .ThenBy(
                x => x.Id)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        WarehouseStockReservation reservation,
        CancellationToken cancellationToken = default)
    {
        await context.WarehouseStockReservations.AddAsync(
            reservation,
            cancellationToken);
    }
}