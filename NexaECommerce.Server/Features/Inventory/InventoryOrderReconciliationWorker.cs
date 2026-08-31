using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class InventoryOrderReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<InventoryOrderReconciliationWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval =
        TimeSpan.FromMinutes(1);

    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Inventory/order reconciliation worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(
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
                    "Inventory/order reconciliation failed.");
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
            "Inventory/order reconciliation worker stopped.");
    }

    private async Task ProcessAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            scopeFactory.CreateScope();

        var reconciliation =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryOrderReconciliationService>();

        /*
         * Current TenantResolutionMiddleware resolves one tenant
         * from the current HTTP request. Background services do not
         * have an HTTP request, so reconciliation must be tenant-aware.
         *
         * The current application uses a default tenant for the
         * development environment. This worker intentionally uses
         * that configured tenant until a tenant catalog/job scheduler
         * exists.
         */
        var tenantId =
            scope.ServiceProvider
                .GetRequiredService<
                    NexaEcommerce.SharedKernel.Abstractions.ICurrentTenant>()
                .Id;

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            logger.LogWarning(
                "Inventory/order reconciliation skipped because no tenant is available.");
            return;
        }

        var result =
            await reconciliation.ReconcileAsync(
                tenantId,
                BatchSize,
                cancellationToken);

        if (result.ReservationsChecked == 0)
            return;

        logger.LogInformation(
            "Inventory/order reconciliation checked {ReservationsChecked} reservations and repaired {ReservationsRepaired}; discrepancies: {Discrepancies}.",
            result.ReservationsChecked,
            result.ReservationsRepaired,
            result.Discrepancies);
    }
}