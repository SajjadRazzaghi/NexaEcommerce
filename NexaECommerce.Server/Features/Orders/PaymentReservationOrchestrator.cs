using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentReservationOrchestrator(
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IPaymentAttemptRepository paymentAttemptRepository,
    IInventoryService inventory,
    IOrderConcurrencyService orderConcurrency,
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

        var normalizedTenantId =
            tenantId.Trim();

        var normalizedUserId =
            userId.Trim();

        var normalizedIdempotencyKey =
            idempotencyKey.Trim();

        await orderConcurrency.ExecuteAsync(
            normalizedTenantId,
            orderId,
            ct =>
                EnsureAsync(
                    normalizedTenantId,
                    normalizedUserId,
                    orderId,
                    normalizedIdempotencyKey,
                    enforceStaleOrderPolicy: true,
                    ct),
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

        var normalizedTenantId =
            tenantId.Trim();

        var normalizedUserId =
            userId.Trim();

        await orderConcurrency.ExecuteAsync(
            normalizedTenantId,
            orderId,
            ct =>
                EnsureAsync(
                    normalizedTenantId,
                    normalizedUserId,
                    orderId,
                    $"attempt:{paymentAttemptId:N}",
                    enforceStaleOrderPolicy: false,
                    ct),
            cancellationToken);
    }

    /*
     * Used by PaymentCompletionOrchestrator when the order lock
     * has already been acquired there.
     *
     * IMPORTANT:
     * Do not call EnsureForCompletionAsync from inside another
     * order lock because sp_getapplock is already held.
     */
    internal async Task EnsureForCompletionUnderLockAsync(
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

        foreach (var candidate in orders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var didCancel =
                await orderConcurrency.ExecuteAsync(
                    normalizedTenantId,
                    candidate.Id,
                    async ct =>
                    {
                        /*
                         * OrderConcurrencyService clears the EF
                         * ChangeTracker immediately after acquiring
                         * the lock, so this query gets a fresh order.
                         */
                        var order =
                            await orderRepository.GetByIdAsync(
                                normalizedTenantId,
                                candidate.Id,
                                null,
                                ct);

                        if (order is null)
                        {
                            return false;
                        }

                        if (order.Status !=
                            OrderStatus.PendingPayment)
                        {
                            return false;
                        }

                        if (order.CreatedAt >= cutoff)
                        {
                            return false;
                        }

                        var hasRecoverableReservation =
                            await HasRecoverableReservationForStaleOrderAsync(
                                normalizedTenantId,
                                order,
                                now,
                                ct);

                        if (hasRecoverableReservation)
                        {
                            return false;
                        }

                        try
                        {
                            order.Cancel();

                            await orderUnitOfWork.SaveChangesAsync(
                                ct);

                            logger.LogInformation(
                                "Stale pending-payment order {OrderId} ({OrderNumber}) was cancelled because it is older than {LifetimeHours} hours and has no recoverable inventory reservation.",
                                order.Id,
                                order.OrderNumber,
                                StaleOrderLifetime.TotalHours);

                            return true;
                        }
                        catch (InvalidOperationException ex)
                        {
                            logger.LogError(
                                ex,
                                "Failed to cancel stale pending-payment order {OrderId} ({OrderNumber}).",
                                order.Id,
                                order.OrderNumber);

                            return false;
                        }
                    },
                    cancellationToken);

            if (didCancel)
            {
                cancelled++;
            }
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
            /*
             * DO NOT call SaveChangesAsync again here.
             *
             * The previous SaveChanges may already have failed because
             * an entity was changed/deleted by another actor.
             *
             * We compensate the physical inventory reservation only.
             * The reconciliation process can repair the durable order
             * state later.
             */
            foreach (
                var reservationKey
                in createdReservationKeys
                    .AsEnumerable()
                    .Reverse())
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
    private async Task<OrderInventoryReservation>
  AddOrderReservationAsync(
      Order order,
      string reservationKey,
      Guid productVariantId,
      int quantity,
      DateTimeOffset expiresAt,
      CancellationToken cancellationToken)
    {
        var normalizedKey =
            reservationKey.Trim();

        var existed =
            order.InventoryReservations.Any(
                x =>
                    string.Equals(
                        x.ReservationKey,
                        normalizedKey,
                        StringComparison.Ordinal));

        var reservation =
            order.AddInventoryReservation(
                normalizedKey,
                productVariantId,
                quantity,
                expiresAt);

        if (!existed)
        {
            await orderRepository.AddInventoryReservationAsync(
                reservation,
                cancellationToken);
        }

        return reservation;
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
      await AddOrderReservationAsync(
          order,
          baseKey,
          item.ProductVariantId,
          item.Quantity,
          refreshed.ExpiresAt,
          cancellationToken);

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
                    await AddOrderReservationAsync(
      order,
      baseKey,
      item.ProductVariantId,
      item.Quantity,
      effectiveExpiration,
      cancellationToken);

                    return;
                }
            }
            else if (existingStatus ==
                     StockReservationStatus.Committed)
            {
                var committedReservation =
     await AddOrderReservationAsync(
         order,
         baseKey,
         item.ProductVariantId,
         item.Quantity,
         existing.ExpiresAt,
         cancellationToken);

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

        await AddOrderReservationAsync(
    order,
    reservationKey,
    item.ProductVariantId,
    item.Quantity,
    reservation.ExpiresAt,
    cancellationToken);

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

            /*
             * اگر سمت Order قبلاً Commit شده باشد،
             * این Reservation برای ما قابل بازیابی است.
             */
            if (orderReservation.Status ==
                InventoryReservationStatus.Committed)
            {
                return true;
            }

            /*
             * فقط Reservationهای Reserved را
             * با Inventory تطبیق می‌دهیم.
             */
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

            /*
             * Reservation در Inventory دیگر وجود ندارد.
             * چون Order آن را Reserved می‌دانسته،
             * وضعیت Order را Expired می‌کنیم.
             *
             * این موجودیت از قبل در DbContext لود شده،
             * بنابراین اینجا AddAsync لازم نیست.
             */
            if (inventoryReservation is null)
            {
                orderReservation.MarkExpired();
                continue;
            }

            if (!Enum.TryParse<StockReservationStatus>(
                    inventoryReservation.Status,
                    true,
                    out var inventoryStatus))
            {
                /*
                 * وضعیت نامعتبر است.
                 * در این حالت Order را Cancel نمی‌کنیم،
                 * چون وضعیت Inventory نامشخص است.
                 */
                logger.LogError(
                    "Unknown inventory reservation status {Status} for {ReservationKey}.",
                    inventoryReservation.Status,
                    inventoryReservation.ReservationKey);

                return true;
            }

            switch (inventoryStatus)
            {
                case StockReservationStatus.Active:
                    {
                        /*
                         * Reservation هنوز فعال و قابل استفاده است.
                         */
                        if (inventoryReservation.ExpiresAt > now)
                        {
                            return true;
                        }

                        /*
                         * Reservation در Inventory منقضی شده،
                         * بنابراین ابتدا تلاش می‌کنیم آن را Release کنیم.
                         */
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
                             * اگر نتوانستیم وضعیت Inventory را قطعی کنیم،
                             * Order را Cancel نمی‌کنیم.
                             */
                            logger.LogWarning(
                                ex,
                                "Could not release expired reservation {ReservationKey} while evaluating stale order {OrderId}.",
                                orderReservation.ReservationKey,
                                order.Id);

                            return true;
                        }

                        break;
                    }

                case StockReservationStatus.Committed:
                    {
                        /*
                         * Inventory Commit شده ولی Order هنوز Reserved است.
                         * Order را با Inventory هماهنگ می‌کنیم.
                         */
                        orderReservation.MarkCommitted();

                        return true;
                    }

                case StockReservationStatus.Released:
                    {
                        /*
                         * Inventory Release شده،
                         * پس Reservation سمت Order نیز باید Release شود.
                         */
                        orderReservation.MarkReleased();

                        break;
                    }

                case StockReservationStatus.Expired:
                    {
                        /*
                         * Inventory Expired شده،
                         * پس Order را نیز Expired می‌کنیم.
                         */
                        orderReservation.MarkExpired();

                        break;
                    }

                default:
                    {
                        /*
                         * وضعیت ناشناخته است.
                         * Order را Cancel نمی‌کنیم چون وضعیت Inventory
                         * قابل اعتماد نیست.
                         */
                        logger.LogError(
                            "Unsupported inventory reservation status {Status} for reservation {ReservationKey} while evaluating stale order {OrderId}.",
                            inventoryReservation.Status,
                            inventoryReservation.ReservationKey,
                            order.Id);

                        return true;
                    }
            }
        }

        /*
         * هیچ Reservation فعال یا قابل بازیابی باقی نمانده است.
         * در این حالت Order stale می‌تواند Cancel شود.
         */
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