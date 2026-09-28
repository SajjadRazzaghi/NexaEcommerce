using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

public sealed class OrderUnitOfWork(
    OrdersDbContext context,
    ILogger<OrderUnitOfWork> logger)
    : IOrderUnitOfWork
{
    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(
                ex,
                "Orders SaveChanges concurrency exception. Database={Database}, DataSource={DataSource}",
                context.Database.GetDbConnection().Database,
                context.Database.GetDbConnection().DataSource);

            foreach (var entry in ex.Entries)
            {
                var entityType =
                    entry.Metadata.ClrType.Name;

                logger.LogError(
                    "Concurrency entity: Type={EntityType}, State={State}",
                    entityType,
                    entry.State);

                if (entry.Entity is OrderInventoryReservation reservation)
                {
                    logger.LogError(
                        "OrderInventoryReservation conflict: Id={Id}, OrderId={OrderId}, TenantId={TenantId}, ReservationKey={ReservationKey}, Status={Status}, Quantity={Quantity}, ExpiresAt={ExpiresAt}, CompletedAt={CompletedAt}",
                        reservation.Id,
                        reservation.OrderId,
                        reservation.TenantId,
                        reservation.ReservationKey,
                        reservation.Status,
                        reservation.Quantity,
                        reservation.ExpiresAt,
                        reservation.CompletedAt);

                    try
                    {
                        var databaseValues =
                            await entry.GetDatabaseValuesAsync(
                                cancellationToken);

                        if (databaseValues is null)
                        {
                            logger.LogError(
                                "OrderInventoryReservation {ReservationId} is NOT visible from the application's DbContext connection.",
                                reservation.Id);
                        }
                        else
                        {
                            logger.LogError(
                                "Database values for reservation {ReservationId}: OrderId={OrderId}, TenantId={TenantId}, ReservationKey={ReservationKey}, Status={Status}, Quantity={Quantity}, ExpiresAt={ExpiresAt}, CreatedAt={CreatedAt}, CompletedAt={CompletedAt}",
                                reservation.Id,
                                databaseValues[nameof(OrderInventoryReservation.OrderId)],
                                databaseValues[nameof(OrderInventoryReservation.TenantId)],
                                databaseValues[nameof(OrderInventoryReservation.ReservationKey)],
                                databaseValues[nameof(OrderInventoryReservation.Status)],
                                databaseValues[nameof(OrderInventoryReservation.Quantity)],
                                databaseValues[nameof(OrderInventoryReservation.ExpiresAt)],
                                databaseValues[nameof(OrderInventoryReservation.CreatedAt)],
                                databaseValues[nameof(OrderInventoryReservation.CompletedAt)]);
                        }
                    }
                    catch (Exception diagnosticsException)
                    {
                        logger.LogError(
                            diagnosticsException,
                            "Failed to read database values for concurrency diagnostics of reservation {ReservationId}.",
                            reservation.Id);
                    }
                }
            }

            throw;
        }
    }
}