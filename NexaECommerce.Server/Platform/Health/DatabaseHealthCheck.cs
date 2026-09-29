using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexaECommerce.Server.Data;

namespace NexaECommerce.Server.Platform.Health;

/// <summary>
/// Verifies that the application database is reachable.
///
/// AppDbContext has a historical migration chain that is intentionally
/// not applied by EcommerceDatabaseInitializer because it was generated
/// from an older data model. Therefore this health check must not treat
/// those historical pending migrations as a degraded runtime condition.
/// </summary>
public sealed class DatabaseHealthCheck(
    AppDbContext db)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        if (!await db.Database.CanConnectAsync(ct))
        {
            return HealthCheckResult.Unhealthy(
                "Cannot connect to the application database.");
        }

        var data =
            new Dictionary<string, object>
            {
                ["provider"] =
                    db.Database.ProviderName ??
                    "unknown",
            };

        return HealthCheckResult.Healthy(
            "Database reachable.",
            data);
    }
}

