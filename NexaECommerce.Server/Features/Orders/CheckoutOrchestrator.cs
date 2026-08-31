using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaECommerce.Server.Features.Orders;

public sealed class CheckoutOrchestrator(
IInventoryService inventory,
IOrderService orders)
{
    private static readonly TimeSpan ReservationLifetime =
    TimeSpan.FromMinutes(15);

public async Task<OrderDto> ExecuteAsync(
    string tenantId,
    string userId,
    string idempotencyKey,
    CheckoutRequest request,
    CancellationToken cancellationToken = default)
    {
        ValidateInput(
            tenantId,
            userId,
            idempotencyKey,
            request);

        /*
         * CreateFromCheckoutAsync is idempotent.
         *
         * When the same key is retried, it returns the
         * already existing order instead of creating a
         * second order.
         */
        var order =
            await orders.CreateFromCheckoutAsync(
                tenantId,
                userId,
                idempotencyKey,
                request,
                cancellationToken);

        /*
         * The order is already past PendingPayment only
         * when a previous checkout attempt already completed
         * its business transition.
         *
         * This makes the HTTP operation safe to retry.
         */
        if (!string.Equals(
                order.Status,
                "PendingPayment",
                StringComparison.OrdinalIgnoreCase))
        {
            return order;
        }

        /*
         * A previous retry may have created reservations already.
         * We do not blindly reserve again.
         *
         * The reservation key is deterministic for every
         * checkout line, so InventoryService itself also gives us
         * idempotency at the reservation layer.
         */
        foreach (var item in order.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var reservationKey =
                BuildReservationKey(
                    tenantId,
                    userId,
                    idempotencyKey,
                    item.ProductVariantId);

            var reservation =
                await inventory.ReserveAsync(
                    tenantId,
                    item.ProductVariantId,
                    item.Quantity,
                    reservationKey,
                    ReservationLifetime,
                    cancellationToken);

            /*
             * This is the missing link in the existing implementation.
             *
             * The reservation must be recorded inside the Order aggregate
             * so PaymentCompletionOrchestrator can later commit exactly
             * the reservations belonging to this order.
             */
            var expiresAt =
                reservation.ExpiresAt;

            await orders.RecordInventoryReservationAsync(
                tenantId,
                userId,
                order.Id,
                reservationKey,
                item.ProductVariantId,
                item.Quantity,
                expiresAt,
                cancellationToken);
        }

        return
            await orders.GetAsync(
                tenantId,
                order.Id,
                userId,
                cancellationToken)
            ?? order;
    }

    private static void ValidateInput(
        string tenantId,
        string userId,
        string idempotencyKey,
        CheckoutRequest request)
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

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "Checkout must contain at least one item.");
        }
    }

    public static string BuildReservationKey(
        string tenantId,
        string userId,
        string idempotencyKey,
        Guid productVariantId)
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

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        return string.Concat(
            "checkout:",
            tenantId.Trim(),
            ":",
            userId.Trim(),
            ":",
            idempotencyKey.Trim(),
            ":",
            productVariantId.ToString("N"));
    }


}
