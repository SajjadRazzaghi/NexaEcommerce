using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

public sealed class OrderConcurrencyService(
    OrdersDbContext context)
    : Application.Services.IOrderConcurrencyService
{
    private const int LockTimeoutMilliseconds = 30_000;

    public async Task<T> ExecuteAsync<T>(
        string tenantId,
        Guid orderId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        Validate(
            tenantId,
            orderId,
            action);

        var connection =
            context.Database.GetDbConnection();

        var wasOpen =
            connection.State ==
            ConnectionState.Open;

        if (!wasOpen)
        {
            await context.Database.OpenConnectionAsync(
                cancellationToken);
        }

        var resource =
            BuildResourceName(
                tenantId,
                orderId);

        try
        {
            await AcquireLockAsync(
                connection,
                resource,
                cancellationToken);

            /*
             * Very important:
             *
             * Anything loaded before acquiring the lock may already
             * be stale because another request may have changed the
             * same order while we were waiting for the lock.
             *
             * Clear the EF tracker so the code inside the lock gets
             * fresh entities from the database.
             */
            context.ChangeTracker.Clear();

            try
            {
                return await action(
                    cancellationToken);
            }
            finally
            {
                /*
                 * Session locks are held by the SQL connection.
                 * Always release the lock before leaving the critical
                 * section.
                 */
                await ReleaseLockAsync(
                    connection,
                    resource,
                    cancellationToken);
            }
        }
        finally
        {
            if (!wasOpen)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    public async Task ExecuteAsync(
        string tenantId,
        Guid orderId,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            tenantId,
            orderId,
            async ct =>
            {
                await action(ct);
                return true;
            },
            cancellationToken);
    }

    private static async Task AcquireLockAsync(
        DbConnection connection,
        string resource,
        CancellationToken cancellationToken)
    {
        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            DECLARE @Result int;

            EXEC @Result = sp_getapplock
                @Resource = @Resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Session',
                @LockTimeout = @LockTimeout;

            SELECT @Result;
            """;

        command.CommandType =
            CommandType.Text;

        AddParameter(
            command,
            "@Resource",
            resource);

        AddParameter(
            command,
            "@LockTimeout",
            LockTimeoutMilliseconds);

        var result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        var returnCode =
            Convert.ToInt32(
                result);

        if (returnCode < 0)
        {
            throw new InvalidOperationException(
                $"Could not acquire order concurrency lock '{resource}'. SQL Server sp_getapplock returned {returnCode}.");
        }
    }

    private static async Task ReleaseLockAsync(
        DbConnection connection,
        string resource,
        CancellationToken cancellationToken)
    {
        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            DECLARE @Result int;

            EXEC @Result = sp_releaseapplock
                @Resource = @Resource,
                @LockOwner = 'Session';

            SELECT @Result;
            """;

        command.CommandType =
            CommandType.Text;

        AddParameter(
            command,
            "@Resource",
            resource);

        var result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        var returnCode =
            Convert.ToInt32(
                result);

        if (returnCode < 0)
        {
            throw new InvalidOperationException(
                $"Could not release order concurrency lock '{resource}'. SQL Server sp_releaseapplock returned {returnCode}.");
        }
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object value)
    {
        var parameter =
            command.CreateParameter();

        parameter.ParameterName =
            name;

        parameter.Value =
            value;

        command.Parameters.Add(
            parameter);
    }

    private static string BuildResourceName(
        string tenantId,
        Guid orderId)
    {
        return
            $"NexaECommerce:Order:{tenantId.Trim()}:{orderId:N}";
    }

    private static void Validate<T>(
        string tenantId,
        Guid orderId,
        Func<CancellationToken, Task<T>> action)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        ArgumentNullException.ThrowIfNull(
            action);
    }
}