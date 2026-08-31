using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Application.Services;

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

        var repository =
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

        var reservations =
            await repository.GetExpiredReservationsAsync(
                DateTimeOffset.UtcNow,
                BatchSize,
                cancellationToken);

        if (reservations.Count == 0)
            return;

        var processedInventory =
            0;

        var processedOrders =
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

        foreach (var reservation in reservations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var order =
                await orderRepository.GetByReservationKeyAsync(
                    reservation.TenantId,
                    reservation.ReservationKey,
                    cancellationToken);

            if (order is null)
                continue;

            var changed =
                order.MarkInventoryReservationExpired(
                    reservation.ReservationKey);

            if (changed)
            {
                processedOrders++;
            }
        }

        if (processedOrders > 0)
        {
            await orderUnitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        logger.LogInformation(
            "Expired {InventoryCount} inventory reservations and synchronized {OrderCount} order reservations.",
            processedInventory,
            processedOrders);
    }
}

