using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Application.Services;

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

        var unitOfWork =
            scope.ServiceProvider
                .GetRequiredService<
                    IInventoryUnitOfWork>();

        var reservations =
            await repository.GetExpiredReservationsAsync(
                DateTimeOffset.UtcNow,
                BatchSize,
                cancellationToken);

        if (reservations.Count == 0)
            return;

        var processed = 0;

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

            processed++;
        }

        if (processed == 0)
            return;

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Expired and released {Count} inventory reservations.",
            processed);
    }
}