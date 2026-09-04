using System.Security.Cryptography;
using System.Text;
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

        var order =
            await orders.CreateFromCheckoutAsync(
                tenantId,
                userId,
                idempotencyKey,
                request,
                cancellationToken);

        if (!string.Equals(
                order.Status,
                "PendingPayment",
                StringComparison.OrdinalIgnoreCase))
        {
            return order;
        }

        var acquiredReservationKeys =
            new List<string>();

        try
        {
            foreach (var item in order.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var reservationKey =
                    BuildReservationKey(
                        tenantId,
                        userId,
                        idempotencyKey,
                        item.ProductVariantId);

                /*
                 * Register the key before calling Inventory.
                 *
                 * If ReserveAsync succeeds but recording the reservation
                 * on the order fails, compensation must still release it.
                 */
                acquiredReservationKeys.Add(
                    reservationKey);

                var reservation =
                    await inventory.ReserveAsync(
                        tenantId,
                        item.ProductVariantId,
                        item.Quantity,
                        reservationKey,
                        ReservationLifetime,
                        cancellationToken);

                await orders.RecordInventoryReservationAsync(
                    tenantId,
                    userId,
                    order.Id,
                    reservationKey,
                    item.ProductVariantId,
                    item.Quantity,
                    reservation.ExpiresAt,
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
        catch
        {
            await CompensateFailedCheckoutAsync(
                tenantId,
                userId,
                order.Id,
                acquiredReservationKeys);

            throw;
        }
    }

    private async Task CompensateFailedCheckoutAsync(
        string tenantId,
        string userId,
        Guid orderId,
        IReadOnlyCollection<string> reservationKeys)
    {
        foreach (var reservationKey in
                 reservationKeys.Reverse())
        {
            try
            {
                await inventory.ReleaseAsync(
                    tenantId,
                    reservationKey);
            }
            catch
            {
                /*
                 * Compensation must continue for all remaining
                 * reservations. Reconciliation can handle a
                 * reservation that remains inconsistent.
                 */
            }
        }

        try
        {
            await orders.CancelAsync(
                tenantId,
                orderId,
                userId);
        }
        catch
        {
            /*
             * Preserve the original checkout exception.
             */
        }
    }

    private static void ValidateInput(
        string tenantId,
        string userId,
        string idempotencyKey,
        CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
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
                "Checkout must contain at least one item.",
                nameof(request));
        }
    }

    public static string BuildReservationKey(
        string tenantId,
        string userId,
        string idempotencyKey,
        Guid productVariantId)
    {
        if (string.IsNullOrWhiteSpace(
                tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
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

        var material =
            string.Concat(
                tenantId.Trim(),
                "|",
                userId.Trim(),
                "|",
                idempotencyKey.Trim(),
                "|",
                productVariantId.ToString("N"));

        var hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(material));

        return string.Concat(
            "checkout:",
            Convert.ToHexString(hash)
                .ToLowerInvariant());
    }
}

