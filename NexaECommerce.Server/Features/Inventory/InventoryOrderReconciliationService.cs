using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaECommerce.Server.Features.Orders;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class InventoryOrderReconciliationService(
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IInventoryService inventory,
    IOrderConcurrencyService orderConcurrency,
    PaymentReservationOrchestrator paymentReservations,
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

        tenantId =
            tenantId.Trim();

        var candidateOrders =
            await orderRepository
                .GetOrdersForInventoryReconciliationAsync(
                    tenantId,
                    batchSize,
                    cancellationToken);

        var checkedReservations =
            0;

        var repairedReservations =
            0;

        var discrepancies =
            0;

        /*
         * ------------------------------------------------------------
         * Process every order independently under the same order lock
         * used by payment.
         * ------------------------------------------------------------
         */
        foreach (var candidate in candidateOrders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await orderConcurrency.ExecuteAsync(
                tenantId,
                candidate.Id,
                async ct =>
                {
                    /*
                     * IMPORTANT:
                     *
                     * candidate was loaded before the lock and may
                     * already be stale.
                     *
                     * Reload the order after acquiring the lock.
                     */
                    var order =
                        await orderRepository.GetByIdAsync(
                            tenantId,
                            candidate.Id,
                            null,
                            ct);

                    if (order is null)
                    {
                        return;
                    }

                    var orderChanged =
                        false;

                    foreach (
                        var orderReservation
                        in order.InventoryReservations)
                    {
                        ct.ThrowIfCancellationRequested();

                        checkedReservations++;

                        var inventoryReservation =
                            await inventory.GetReservationAsync(
                                tenantId,
                                orderReservation.ReservationKey,
                                ct);

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
                                ct);

                        switch (action)
                        {
                            case ReconciliationAction.Repaired:
                                repairedReservations++;
                                orderChanged = true;
                                break;

                            case ReconciliationAction.Discrepancy:
                                discrepancies++;
                                break;
                        }
                    }

                    /*
                     * Save while the same order lock is still held.
                     */
                    if (orderChanged)
                    {
                        await orderUnitOfWork.SaveChangesAsync(
                            ct);
                    }
                },
                cancellationToken);
        }

        /*
         * Stale-order cancellation already acquires the same order lock
         * internally.
         */
        var staleOrdersCancelled =
            await paymentReservations
                .CancelStalePendingOrdersAsync(
                    tenantId,
                    batchSize,
                    cancellationToken);

        return new InventoryReconciliationResult(
            candidateOrders.Count,
            checkedReservations,
            repairedReservations,
            discrepancies,
            staleOrdersCancelled);
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
    int Discrepancies,
    int StaleOrdersCancelled);

internal enum ReconciliationAction
{
    None = 0,
    Repaired = 1,
    Discrepancy = 2
}