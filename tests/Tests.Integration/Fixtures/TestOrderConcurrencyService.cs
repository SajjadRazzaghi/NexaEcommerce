using System.Collections.Concurrent;
using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaECommerce.Tests.Integration.Fixtures;

public sealed class TestOrderConcurrencyService
    : IOrderConcurrencyService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim>
        Locks = new(StringComparer.Ordinal);

    public async Task<T> ExecuteAsync<T>(
        string tenantId,
        Guid orderId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        Validate(tenantId, orderId);

        ArgumentNullException.ThrowIfNull(action);

        var key =
            $"{tenantId.Trim()}:{orderId:N}";

        var gate =
            Locks.GetOrAdd(
                key,
                _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);

        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task ExecuteAsync(
        string tenantId,
        Guid orderId,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

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

    private static void Validate(
        string tenantId,
        Guid orderId)
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
    }
}