using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class ShipmentRepository(
    OrdersDbContext context)
    : IShipmentRepository
{
    public async Task<Shipment?> GetByIdAsync(
        string tenantId,
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        return await context.Shipments
            .FirstOrDefaultAsync(
                x =>
                    x.Id == shipmentId &&
                    x.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<Shipment?> GetByOrderIdAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await context.Shipments
            .FirstOrDefaultAsync(
                x =>
                    x.OrderId == orderId &&
                    x.TenantId == tenantId,
                cancellationToken);
    }

    public async Task AddAsync(
        Shipment shipment,
        CancellationToken cancellationToken = default)
    {
        await context.Shipments.AddAsync(
            shipment,
            cancellationToken);
    }
}
