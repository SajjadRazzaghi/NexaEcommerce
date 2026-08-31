using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class InventoryOrderReconciliationService(
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IInventoryService inventory,
    ILogger<InventoryOrderReconciliationService> logger)
{
    public async Task<InventoryReconciliationResult>
        ReconcileAsync(
            string tenantId,
            int batchSize,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        var orders =
            await orderRepository
                .GetOrdersForInventoryReconciliationAsync(
                    tenantId,
                    batchSize,
                    cancellationToken);

        if (orders.Count == 0)
        {
            return new InventoryReconciliationResult(
                0,
                0,
                0,
                0);
        }

        var checkedReservations = 0;
        var repairedReservations = 0;
        var discrepancies = 0;

        foreach (var order in orders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var orderReservation in
                     order.InventoryReservations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                checkedReservations++;

                var inventoryReservation =
                    await inventory.GetReservationAsync(
                        tenantId,
                        orderReservation.ReservationKey,
                        cancellationToken);

                if (inventoryReservation is null)
                {
                    discrepancies++;

                    logger.LogWarning(
                        "Inventory reservation {ReservationKey} referenced by order {OrderId} was not found.",
                        orderReservation.ReservationKey,
                        order.Id);

                    continue;
                }

                if (!Enum.TryParse<
                        StockReservationStatus>(
                        inventoryReservation.Status,
                        true,
                        out var inventoryStatus))
                {
                    discrepancies++;

                    logger.LogError(
                        "Unknown inventory reservation status {Status} for reservation {ReservationKey}.",
                        inventoryReservation.Status,
                        inventoryReservation.ReservationKey);

                    continue;
                }

                var action =
                    await ReconcileReservationAsync(
                        tenantId,
                        orderReservation,
                        inventoryReservation,
                        inventoryStatus,
                        cancellationToken);

                switch (action)
                {
                    case ReconciliationAction.Repaired:
                        repairedReservations++;
                        break;

                    case ReconciliationAction.Discrepancy:
                        discrepancies++;
                        break;
                }
            }
        }

        if (repairedReservations > 0)
        {
            await orderUnitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return new InventoryReconciliationResult(
            orders.Count,
            checkedReservations,
            repairedReservations,
            discrepancies);
    }

    private async Task<ReconciliationAction>
        ReconcileReservationAsync(
            string tenantId,
            OrderInventoryReservation orderReservation,
            StockReservationDto inventoryReservation,
            StockReservationStatus inventoryStatus,
            CancellationToken cancellationToken)
    {
        switch (orderReservation.Status)
        {
            case InventoryReservationStatus.Reserved:

                return inventoryStatus switch
                {
                    StockReservationStatus.Active
                        => ReconciliationAction.None,

                    StockReservationStatus.Committed
                        => MarkOrderCommitted(
                            orderReservation),

                    StockReservationStatus.Released
                        => MarkOrderReleased(
                            orderReservation),

                    StockReservationStatus.Expired
                        => MarkOrderExpired(
                            orderReservation),

                    _ => ReconciliationAction.Discrepancy
                };

            case InventoryReservationStatus.Committed:

                if (inventoryStatus ==
                    StockReservationStatus.Committed)
                {
                    return ReconciliationAction.None;
                }

                if (inventoryStatus ==
                    StockReservationStatus.Active)
                {
                    if (inventoryReservation.ExpiresAt <=
                        DateTimeOffset.UtcNow)
                    {
                        logger.LogError(
                            "Order reservation {ReservationKey} is committed but its inventory reservation has expired.",
                            orderReservation.ReservationKey);

                        return ReconciliationAction.Discrepancy;
                    }

                    try
                    {
                        await inventory.CommitAsync(
                            tenantId,
                            orderReservation.ReservationKey,
                            cancellationToken);

                        return ReconciliationAction.Repaired;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to commit inventory reservation {ReservationKey} while reconciling a committed order reservation.",
                            orderReservation.ReservationKey);

                        return ReconciliationAction.Discrepancy;
                    }
                }

                logger.LogError(
                    "Order reservation {ReservationKey} is committed but inventory status is {Status}.",
                    orderReservation.ReservationKey,
                    inventoryReservation.Status);

                return ReconciliationAction.Discrepancy;

            case InventoryReservationStatus.Released:

            case InventoryReservationStatus.Expired:

                if (inventoryStatus ==
                    StockReservationStatus.Active)
                {
                    try
                    {
                        await inventory.ReleaseAsync(
                            tenantId,
                            orderReservation.ReservationKey,
                            cancellationToken);

                        return ReconciliationAction.Repaired;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to release inventory reservation {ReservationKey} while reconciling a released order reservation.",
                            orderReservation.ReservationKey);

                        return ReconciliationAction.Discrepancy;
                    }
                }

                if (inventoryStatus ==
                    StockReservationStatus.Committed)
                {
                    logger.LogError(
                        "Order reservation {ReservationKey} is {OrderStatus} but inventory reservation is committed.",
                        orderReservation.ReservationKey,
                        orderReservation.Status);

                    return ReconciliationAction.Discrepancy;
                }

                return ReconciliationAction.None;

            default:
                return ReconciliationAction.Discrepancy;
        }
    }

    private static ReconciliationAction
        MarkOrderCommitted(
            OrderInventoryReservation reservation)
    {
        reservation.MarkCommitted();

        return ReconciliationAction.Repaired;
    }

    private static ReconciliationAction
        MarkOrderReleased(
            OrderInventoryReservation reservation)
    {
        reservation.MarkReleased();

        return ReconciliationAction.Repaired;
    }

    private static ReconciliationAction
        MarkOrderExpired(
            OrderInventoryReservation reservation)
    {
        reservation.MarkExpired();

        return ReconciliationAction.Repaired;
    }
}

public sealed record InventoryReconciliationResult(
    int OrdersChecked,
    int ReservationsChecked,
    int ReservationsRepaired,
    int Discrepancies);

internal enum ReconciliationAction
{
    None = 0,
    Repaired = 1,
    Discrepancy = 2
}

