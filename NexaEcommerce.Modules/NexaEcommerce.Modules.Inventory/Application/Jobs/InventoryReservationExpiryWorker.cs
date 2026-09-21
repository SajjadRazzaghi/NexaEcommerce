using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexaEcommerce.Modules.Inventory.Application.Services;

namespace NexaEcommerce.Modules.Inventory.Application.Jobs;

public sealed class InventoryReservationExpiryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<InventoryReservationExpiryWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval =
        TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer =
            new PeriodicTimer(
                Interval);

        while (
            await timer.WaitForNextTickAsync(
                stoppingToken))
        {
            try
            {
                using var scope =
                    scopeFactory.CreateScope();

                var service =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IWarehouseStockReservationService>();

                var affected =
                    await service.ExpirePendingAsync(
                        stoppingToken);

                if (affected > 0)
                {
                    logger.LogInformation(
                        "Expired {Count} inventory reservations.",
                        affected);
                }
            }
            catch (
                OperationCanceledException)
                when (
                    stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Inventory reservation expiry job failed.");
            }
        }
    }
}