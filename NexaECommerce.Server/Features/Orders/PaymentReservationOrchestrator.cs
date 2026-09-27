using System.Security.Cryptography;
using System.Text;

using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentReservationOrchestrator(
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IPaymentAttemptRepository paymentAttemptRepository,
    IInventoryService inventory,
    ILogger<PaymentReservationOrchestrator> logger)
{
    private static readonly TimeSpan PaymentReservationLifetime =
        TimeSpan.FromMinutes(30);

    private static readonly TimeSpan StaleOrderLifetime =
        TimeSpan.FromHours(24);

    public async Task EnsureForStartAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(
            tenantId,
            userId,
            idempotencyKey);

        await EnsureAsync(
            tenantId.Trim(),
            userId.Trim(),
            orderId,
            idempotencyKey.Trim(),
            enforceStaleOrderPolicy: true,
            cancellationToken);
    }

    public async Task EnsureForCompletionAsync(
        string tenantId,
        string userId,
        Guid orderId,
        Guid paymentAttemptId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        if (paymentAttemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment attempt id is required.",
                nameof(paymentAttemptId));
        }

        await EnsureAsync(
            tenantId.Trim(),
            userId.Trim(),
            orderId,
            $"attempt:{paymentAttemptId:N}",
            enforceStaleOrderPolicy: false,
            cancellationToken);
    }

    public async Task<int> CancelStalePendingOrdersAsync(
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

        batchSize =
            Math.Clamp(
                batchSize,
                1,
                500);

        var normalizedTenantId =
            tenantId.Trim();

        var now =
            DateTimeOffset.UtcNow;

        var cutoff =
            now - StaleOrderLifetime;

        var orders =
            await orderRepository
                .GetPendingPaymentOrdersOlderThanAsync(
                    normalizedTenantId,
                    cutoff,
                    batchSize,
                    cancellationToken);

        if (orders.Count == 0)
        {
            return 0;
        }

        var cancelled = 0;

        foreach (var order in orders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hasRecoverableReservation =
                await HasRecoverableReservationForStaleOrderAsync(
                    normalizedTenantId,
                    order,
                    now,
                    cancellationToken);

            if (hasRecoverableReservation)
            {
                continue;
            }

            try
            {
                order.Cancel();

                cancelled++;

                logger.LogInformation(
                    "Stale pending-payment order {OrderId} ({OrderNumber}) was cancelled because it is older than {LifetimeHours} hours and has no recoverable inventory reservation.",
                    order.Id,
                    order.OrderNumber,
                    StaleOrderLifetime.TotalHours);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(
                    ex,
                    "Failed to cancel stale pending-payment order {OrderId} ({OrderNumber}).",
                    order.Id,
                    order.OrderNumber);
            }
        }

        if (cancelled > 0)
        {
            await orderUnitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return cancelled;
    }

    private async Task EnsureAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string reservationIdentity,
        bool enforceStaleOrderPolicy,
        CancellationToken cancellationToken)
    {
        var order =
            await orderRepository.GetByIdAsync(
                tenantId,
                orderId,
                userId,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cancelled orders cannot be paid.");
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "Only orders pending payment can reserve inventory for payment.");
        }

        var items =
            order.Items.ToList();

        if (items.Count == 0)
        {
            throw new InvalidOperationException(
                "The order does not contain any items.");
        }

        /*
         * When this call comes from StartPayment, protect against
         * accidentally using a failed payment attempt with the same
         * idempotency key.
         */
        if (!reservationIdentity.StartsWith(
                "attempt:",
                StringComparison.Ordinal))
        {
            var existingPaymentAttempt =
                await paymentAttemptRepository
                    .GetByIdempotencyKeyAsync(
                        tenantId,
                        userId,
                        reservationIdentity,
                        cancellationToken);

            if (existingPaymentAttempt is not null)
            {
                if (existingPaymentAttempt.OrderId !=
                    orderId)
                {
                    throw new InvalidOperationException(
                        "The payment idempotency key is already associated with another order.");
                }

                if (existingPaymentAttempt.Status ==
                    PaymentAttemptStatus.Failed)
                {
                    throw new InvalidOperationException(
                        "The payment attempt has already failed. Use a new idempotency key.");
                }
            }
        }

        var now =
            DateTimeOffset.UtcNow;

        var targetExpiration =
            now.Add(
                PaymentReservationLifetime);

        var satisfiedItems =
            new HashSet<Guid>();

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var satisfied =
                await TryUseExistingReservationAsync(
                    tenantId,
                    order,
                    item,
                    targetExpiration,
                    now,
                    cancellationToken);

            if (satisfied)
            {
                satisfiedItems.Add(
                    item.ProductVariantId);
            }
        }

        /*
         * Orders older than the revivable window are not allowed to
         * create fresh payment reservations.
         *
         * If there is at least one recoverable reservation we leave
         * the order alive for the reconciliation process to resolve.
         */
        if (enforceStaleOrderPolicy &&
            now - order.CreatedAt > StaleOrderLifetime &&
            satisfiedItems.Count == 0)
        {
            try
            {
                order.Cancel();

                await orderUnitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to persist cancellation for expired order {OrderId}.",
                    order.Id);
            }

            throw new PaymentReservationException(
                PaymentReservationFailureKind.OrderExpired,
                "The order has expired because it is older than 24 hours and no active inventory reservation remains.");
        }

        var createdReservationKeys =
            new List<string>();

        try
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (satisfiedItems.Contains(
                        item.ProductVariantId))
                {
                    continue;
                }

                await CreateAndAttachReservationAsync(
                    tenantId,
                    userId,
                    order,
                    item,
                    reservationIdentity,
                    targetExpiration,
                    createdReservationKeys,
                    cancellationToken);
            }

            await orderUnitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            foreach (
                var reservationKey
                in createdReservationKeys.AsEnumerable().Reverse())
            {
                try
                {
                    await inventory.ReleaseAsync(
                        tenantId,
                        reservationKey,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to compensate inventory reservation {ReservationKey}.",
                        reservationKey);
                }
            }

            foreach (var reservation
                     in order.InventoryReservations)
            {
                if (!createdReservationKeys.Contains(
                        reservation.ReservationKey,
                        StringComparer.Ordinal))
                {
                    continue;
                }

                if (reservation.Status ==
                    InventoryReservationStatus.Reserved)
                {
                    reservation.MarkReleased();
                }
            }

            try
            {
                await orderUnitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to persist payment reservation compensation for order {OrderId}.",
                    order.Id);
            }

            throw;
        }
    }

    private async Task<bool>
        TryUseExistingReservationAsync(
            string tenantId,
            Order order,
            OrderItem item,
            DateTimeOffset targetExpiration,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        /*
         * A committed reservation is already sufficient for the order.
         */
        var committed =
            order.InventoryReservations
                .FirstOrDefault(
                    x =>
                        x.ProductVariantId ==
                        item.ProductVariantId &&
                        x.Quantity ==
                        item.Quantity &&
                        x.Status ==
                        InventoryReservationStatus.Committed);

        if (committed is not null)
        {
            return true;
        }

        var candidates =
            order.InventoryReservations
                .Where(
                    x =>
                        x.ProductVariantId ==
                        item.ProductVariantId &&
                        x.Quantity ==
                        item.Quantity &&
                        x.Status ==
                        InventoryReservationStatus.Reserved)
                .OrderByDescending(
                    x => x.ExpiresAt)
                .ToList();

        foreach (var orderReservation in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var inventoryReservation =
                await inventory.GetReservationAsync(
                    tenantId,
                    orderReservation.ReservationKey,
                    cancellationToken);

            if (inventoryReservation is null)
            {
                orderReservation.MarkExpired();
                continue;
            }

            if (inventoryReservation.ProductVariantId !=
                    item.ProductVariantId ||
                inventoryReservation.Quantity !=
                    item.Quantity)
            {
                throw new PaymentReservationException(
                    PaymentReservationFailureKind.InventoryUnavailable,
                    $"Inventory reservation for product '{item.ProductVariantId}' does not match the order quantity.");
            }

            StockReservationStatus inventoryStatus;

            if (!Enum.TryParse<StockReservationStatus>(
                    inventoryReservation.Status,
                    true,
                    out inventoryStatus))
            {
                throw new PaymentReservationException(
                    PaymentReservationFailureKind.InventoryUnavailable,
                    $"Inventory reservation status '{inventoryReservation.Status}' is invalid.");
            }

            switch (inventoryStatus)
            {
                case StockReservationStatus.Active:
                    {
                        if (inventoryReservation.ExpiresAt <= now)
                        {
                            try
                            {
                                await inventory.ReleaseAsync(
                                    tenantId,
                                    orderReservation.ReservationKey,
                                    cancellationToken);

                                orderReservation.MarkExpired();
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(
                                    ex,
                                    "Failed to release expired reservation {ReservationKey}.",
                                    orderReservation.ReservationKey);

                                throw new PaymentReservationException(
                                    PaymentReservationFailureKind.InventoryUnavailable,
                                    "The existing inventory reservation could not be recovered.");
                            }

                            continue;
                        }

                        var effectiveExpiration =
                            inventoryReservation.ExpiresAt;

                        if (effectiveExpiration <
                            targetExpiration)
                        {
                            try
                            {
                                var extended =
                                    await inventory
                                        .ExtendReservationAsync(
                                            tenantId,
                                            orderReservation.ReservationKey,
                                            targetExpiration,
                                            cancellationToken);

                                effectiveExpiration =
                                    extended.ExpiresAt;
                            }
                            catch (InvalidOperationException)
                            {
                                var refreshed =
                                    await inventory
                                        .GetReservationAsync(
                                            tenantId,
                                            orderReservation.ReservationKey,
                                            cancellationToken);

                                if (refreshed is null)
                                {
                                    orderReservation.MarkExpired();
                                    continue;
                                }

                                StockReservationStatus refreshedStatus;

                                if (!Enum.TryParse<
                                        StockReservationStatus>(
                                        refreshed.Status,
                                        true,
                                        out refreshedStatus))
                                {
                                    throw new PaymentReservationException(
                                        PaymentReservationFailureKind.InventoryUnavailable,
                                        $"Inventory reservation status '{refreshed.Status}' is invalid.");
                                }

                                if (refreshedStatus ==
                                        StockReservationStatus.Active &&
                                    refreshed.ExpiresAt >
                                        now)
                                {
                                    effectiveExpiration =
                                        refreshed.ExpiresAt;
                                }
                                else if (refreshedStatus ==
                                         StockReservationStatus.Committed)
                                {
                                    orderReservation.MarkCommitted();

                                    return true;
                                }
                                else
                                {
                                    switch (refreshedStatus)
                                    {
                                        case StockReservationStatus.Released:
                                            orderReservation.MarkReleased();
                                            break;

                                        case StockReservationStatus.Expired:
                                            orderReservation.MarkExpired();
                                            break;
                                    }

                                    continue;
                                }
                            }
                        }

                        orderReservation.ExtendTo(
                            effectiveExpiration);

                        return true;
                    }

                case StockReservationStatus.Committed:
                    {
                        orderReservation.MarkCommitted();

                        return true;
                    }

                case StockReservationStatus.Released:
                    {
                        orderReservation.MarkReleased();

                        continue;
                    }

                case StockReservationStatus.Expired:
                    {
                        orderReservation.MarkExpired();

                        continue;
                    }

                default:
                    throw new PaymentReservationException(
                        PaymentReservationFailureKind.InventoryUnavailable,
                        $"Inventory reservation '{orderReservation.ReservationKey}' has an unsupported state.");
            }
        }

        return false;
    }

    private async Task
        CreateAndAttachReservationAsync(
            string tenantId,
            string userId,
            Order order,
            OrderItem item,
            string reservationIdentity,
            DateTimeOffset targetExpiration,
            List<string> createdReservationKeys,
            CancellationToken cancellationToken)
    {
        var baseKey =
            BuildPaymentReservationKey(
                tenantId,
                userId,
                order.Id,
                reservationIdentity,
                item.ProductVariantId);

        var reservationKey =
            baseKey;

        var existing =
            await inventory.GetReservationAsync(
                tenantId,
                baseKey,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.ProductVariantId !=
                    item.ProductVariantId ||
                existing.Quantity !=
                    item.Quantity)
            {
                throw new PaymentReservationException(
                    PaymentReservationFailureKind.InventoryUnavailable,
                    "The payment reservation key is already associated with another inventory reservation.");
            }

            StockReservationStatus existingStatus;

            if (!Enum.TryParse<StockReservationStatus>(
                    existing.Status,
                    true,
                    out existingStatus))
            {
                throw new PaymentReservationException(
                    PaymentReservationFailureKind.InventoryUnavailable,
                    $"Inventory reservation status '{existing.Status}' is invalid.");
            }

            if (existingStatus ==
                    StockReservationStatus.Active &&
                existing.ExpiresAt >
                    DateTimeOffset.UtcNow)
            {
                var effectiveExpiration =
                    existing.ExpiresAt;

                if (effectiveExpiration <
                    targetExpiration)
                {
                    try
                    {
                        var extended =
                            await inventory
                                .ExtendReservationAsync(
                                    tenantId,
                                    baseKey,
                                    targetExpiration,
                                    cancellationToken);

                        effectiveExpiration =
                            extended.ExpiresAt;
                    }
                    catch (InvalidOperationException)
                    {
                        var refreshed =
                            await inventory
                                .GetReservationAsync(
                                    tenantId,
                                    baseKey,
                                    cancellationToken);

                        if (refreshed is null)
                        {
                            reservationKey =
                                BuildRecoveryReservationKey(
                                    baseKey);
                        }
                        else
                        {
                            StockReservationStatus refreshedStatus;

                            if (!Enum.TryParse<
                                    StockReservationStatus>(
                                    refreshed.Status,
                                    true,
                                    out refreshedStatus))
                            {
                                throw new PaymentReservationException(
                                    PaymentReservationFailureKind.InventoryUnavailable,
                                    $"Inventory reservation status '{refreshed.Status}' is invalid.");
                            }

                            if (refreshedStatus ==
                                    StockReservationStatus.Active &&
                                refreshed.ExpiresAt >
                                    DateTimeOffset.UtcNow)
                            {
                                effectiveExpiration =
                                    refreshed.ExpiresAt;
                            }
                            else if (refreshedStatus ==
                                     StockReservationStatus.Committed)
                            {
                                var committedReservation =
                                    order.AddInventoryReservation(
                                        baseKey,
                                        item.ProductVariantId,
                                        item.Quantity,
                                        refreshed.ExpiresAt);

                                if (committedReservation.Status ==
                                    InventoryReservationStatus.Reserved)
                                {
                                    committedReservation.MarkCommitted();
                                }

                                return;
                            }
                            else
                            {
                                reservationKey =
                                    BuildRecoveryReservationKey(
                                        baseKey);
                            }
                        }
                    }
                }

                if (reservationKey ==
                    baseKey)
                {
                    order.AddInventoryReservation(
                        baseKey,
                        item.ProductVariantId,
                        item.Quantity,
                        effectiveExpiration);

                    return;
                }
            }
            else if (existingStatus ==
                     StockReservationStatus.Committed)
            {
                var committedReservation =
                    order.AddInventoryReservation(
                        baseKey,
                        item.ProductVariantId,
                        item.Quantity,
                        existing.ExpiresAt);

                if (committedReservation.Status ==
                    InventoryReservationStatus.Reserved)
                {
                    committedReservation.MarkCommitted();
                }

                return;
            }
            else
            {
                reservationKey =
                    BuildRecoveryReservationKey(
                        baseKey);
            }
        }

        var reservation =
            await inventory.ReserveAsync(
                tenantId,
                item.ProductVariantId,
                item.Quantity,
                reservationKey,
                PaymentReservationLifetime,
                cancellationToken);

        StockReservationStatus createdStatus;

        if (!Enum.TryParse<StockReservationStatus>(
                reservation.Status,
                true,
                out createdStatus) ||
            createdStatus !=
                StockReservationStatus.Active ||
            reservation.ExpiresAt <=
                DateTimeOffset.UtcNow)
        {
            throw new PaymentReservationException(
                PaymentReservationFailureKind.InventoryUnavailable,
                "Inventory reservation could not be created in an active state.");
        }

        order.AddInventoryReservation(
            reservationKey,
            item.ProductVariantId,
            item.Quantity,
            reservation.ExpiresAt);

        createdReservationKeys.Add(
            reservationKey);
    }

    private async Task<bool>
        HasRecoverableReservationForStaleOrderAsync(
            string tenantId,
            Order order,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        foreach (
            var orderReservation
            in order.InventoryReservations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (orderReservation.Status ==
                InventoryReservationStatus.Committed)
            {
                return true;
            }

            if (orderReservation.Status !=
                InventoryReservationStatus.Reserved)
            {
                continue;
            }

            var inventoryReservation =
                await inventory.GetReservationAsync(
                    tenantId,
                    orderReservation.ReservationKey,
                    cancellationToken);

            if (inventoryReservation is null)
            {
                orderReservation.MarkExpired();
                continue;
            }

            StockReservationStatus inventoryStatus;

            if (!Enum.TryParse<StockReservationStatus>(
                    inventoryReservation.Status,
                    true,
                    out inventoryStatus))
            {
                logger.LogError(
                    "Unknown inventory reservation status {Status} for {ReservationKey}.",
                    inventoryReservation.Status,
                    inventoryReservation.ReservationKey);

                return true;
            }

            switch (inventoryStatus)
            {
                case StockReservationStatus.Active:

                    if (inventoryReservation.ExpiresAt >
                        now)
                    {
                        return true;
                    }

                    try
                    {
                        await inventory.ReleaseAsync(
                            tenantId,
                            orderReservation.ReservationKey,
                            cancellationToken);

                        orderReservation.MarkExpired();
                    }
                    catch (Exception ex)
                    {
                        /*
                         * Never cancel a stale order while the inventory
                         * state is unresolved.
                         */
                        logger.LogWarning(
                            ex,
                            "Could not release expired reservation {ReservationKey} while evaluating stale order {OrderId}.",
                            orderReservation.ReservationKey,
                            order.Id);

                        return true;
                    }

                    break;

                case StockReservationStatus.Committed:

                    orderReservation.MarkCommitted();

                    return true;

                case StockReservationStatus.Released:

                    orderReservation.MarkReleased();

                    break;

                case StockReservationStatus.Expired:

                    orderReservation.MarkExpired();

                    break;

                default:

                    return true;
            }
        }

        return false;
    }

    private static string BuildPaymentReservationKey(
        string tenantId,
        string userId,
        Guid orderId,
        string reservationIdentity,
        Guid productVariantId)
    {
        var source =
            string.Join(
                "|",
                tenantId.Trim(),
                userId.Trim(),
                orderId.ToString("N"),
                reservationIdentity.Trim(),
                productVariantId.ToString("N"));

        var hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    source));

        return
            "payment:" +
            Convert.ToHexString(
                hash)
            .ToLowerInvariant();
    }

    private static string BuildRecoveryReservationKey(
        string baseKey)
    {
        return
            $"{baseKey}:r{Guid.NewGuid():N}";
    }

    private static void ValidateIdentity(
        string tenantId,
        string userId,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));
        }

        if (idempotencyKey.Trim().Length > 128)
        {
            throw new ArgumentException(
                "Idempotency key cannot exceed 128 characters.",
                nameof(idempotencyKey));
        }
    }
}

public enum PaymentReservationFailureKind
{
    InventoryUnavailable = 1,
    OrderExpired = 2
}

public sealed class PaymentReservationException
    : InvalidOperationException
{
    public PaymentReservationException(
        PaymentReservationFailureKind kind,
        string message)
        : base(message)
    {
        Kind = kind;
    }

    public PaymentReservationFailureKind Kind
    {
        get;
    }
}