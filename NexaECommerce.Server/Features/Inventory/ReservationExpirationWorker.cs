using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class ReservationExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationExpirationWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval =
        TimeSpan.FromSeconds(30);

    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Inventory reservation expiration worker started.");

        try
        {
            await Task.Delay(
                PollInterval,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process expired inventory reservations.");
            }

            try
            {
                await Task.Delay(
                    PollInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation(
            "Inventory reservation expiration worker stopped.");
    }

    private async Task ProcessExpiredReservationsAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            scopeFactory.CreateScope();

        var inventoryRepository =
            scope.ServiceProvider
                .GetRequiredService<
                    IInventoryRepository>();

        var inventoryUnitOfWork =
            scope.ServiceProvider
                .GetRequiredService<
                    IInventoryUnitOfWork>();

        var orderRepository =
            scope.ServiceProvider
                .GetRequiredService<
                    IOrderRepository>();

        var orderUnitOfWork =
            scope.ServiceProvider
                .GetRequiredService<
                    IOrderUnitOfWork>();

        var orderConcurrency =
            scope.ServiceProvider
                .GetRequiredService<
                    IOrderConcurrencyService>();

        var reservations =
            await inventoryRepository
                .GetExpiredReservationsAsync(
                    DateTimeOffset.UtcNow,
                    BatchSize,
                    cancellationToken);

        if (reservations.Count == 0)
        {
            return;
        }

        /*
         * ------------------------------------------------------------
         * STEP 1
         * ------------------------------------------------------------
         *
         * Expire physical inventory reservations first.
         *
         * This uses InventoryDbContext and therefore does not directly
         * modify OrdersDbContext.
         */
        var processedInventory =
            0;

        foreach (var reservation in reservations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!reservation.IsActive ||
                !reservation.IsExpired)
            {
                continue;
            }

            reservation.StockItem.Release(
                reservation.Quantity);

            reservation.MarkExpired();

            processedInventory++;
        }

        if (processedInventory > 0)
        {
            await inventoryUnitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        /*
         * ------------------------------------------------------------
         * STEP 2
         * ------------------------------------------------------------
         *
         * Synchronize the corresponding OrderInventoryReservation.
         *
         * IMPORTANT:
         *
         * The old implementation loaded an Order and modified it
         * without taking the same order lock used by payment.
         *
         * That was one of the causes of:
         *
         * DbUpdateConcurrencyException
         *
         * We now:
         *
         *   1. group reservations by order,
         *   2. acquire the order lock,
         *   3. discard stale tracked entities,
         *   4. reload the order,
         *   5. modify it,
         *   6. save while the lock is still held.
         */

        var processedOrders =
            0;

        var orderGroups =
            reservations
                .GroupBy(
                    x => new
                    {
                        x.TenantId,
                        OrderId = GetOrderId(
                            x.ReservationKey,
                            orderRepository)
                    })
                .ToList();

        /*
         * The above query cannot obtain OrderId synchronously because
         * the repository is asynchronous.
         *
         * Therefore we resolve order ids below using the reservation
         * keys. This also keeps the logic compatible with the current
         * repository interface.
         */

        var orderReservationGroups =
            new Dictionary<
                (string TenantId, Guid OrderId),
                List<string>>();

        foreach (var reservation in reservations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var order =
                await orderRepository.GetByReservationKeyAsync(
                    reservation.TenantId,
                    reservation.ReservationKey,
                    cancellationToken);

            if (order is null)
            {
                continue;
            }

            var key =
                (
                    reservation.TenantId,
                    order.Id);

            if (!orderReservationGroups.TryGetValue(
                    key,
                    out var keys))
            {
                keys = [];

                orderReservationGroups.Add(
                    key,
                    keys);
            }

            keys.Add(
                reservation.ReservationKey);
        }

        foreach (var group in orderReservationGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tenantId =
                group.Key.TenantId;

            var orderId =
                group.Key.OrderId;

            await orderConcurrency.ExecuteAsync(
                tenantId,
                orderId,
                async ct =>
                {
                    /*
                     * Get a completely fresh Order after acquiring
                     * the lock.
                     */
                    var order =
                        await orderRepository.GetByIdAsync(
                            tenantId,
                            orderId,
                            null,
                            ct);

                    if (order is null)
                    {
                        return;
                    }

                    var changed =
                        false;

                    foreach (
                        var reservationKey
                        in group.Value.Distinct(
                            StringComparer.Ordinal))
                    {
                        var reservation =
                            order.InventoryReservations
                                .FirstOrDefault(
                                    x =>
                                        string.Equals(
                                            x.ReservationKey,
                                            reservationKey,
                                            StringComparison.Ordinal));

                        if (reservation is null)
                        {
                            continue;
                        }

                        if (reservation.Status !=
                            InventoryReservationStatus.Reserved)
                        {
                            continue;
                        }

                        reservation.MarkExpired();

                        changed = true;
                    }

                    if (!changed)
                    {
                        return;
                    }

                    await orderUnitOfWork.SaveChangesAsync(
                        ct);

                    processedOrders++;
                },
                cancellationToken);
        }

        logger.LogInformation(
            "Expired {InventoryCount} inventory reservations and synchronized {OrderCount} order reservations.",
            processedInventory,
            processedOrders);
    }

    /*
     * This method intentionally exists only to satisfy the compiler
     * in the grouping declaration above. The real async lookup is
     * performed in the following phase.
     */
    private static Guid GetOrderId(
        string reservationKey,
        IOrderRepository orderRepository)
    {
        /*
         * Never attempt to parse an OrderId from ReservationKey.
         *
         * Payment reservation keys are SHA256-based and do not contain
         * the order id.
         *
         * The method is therefore intentionally unreachable.
         */
        throw new InvalidOperationException(
            "OrderId must be resolved through IOrderRepository.GetByReservationKeyAsync.");
    }
}