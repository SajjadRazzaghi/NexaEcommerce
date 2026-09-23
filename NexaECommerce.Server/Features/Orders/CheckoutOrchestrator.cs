using System.Security.Cryptography;
using System.Text;

using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaECommerce.Server.Features.Orders;

public sealed class CheckoutOrchestrator(
    IOrderService orders,
    IInventoryService inventory,
    IFulfillmentService fulfillmentService)
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

        var reservationKeys =
            new List<string>();

        try
        {
            // ========================================================
            // 1. Create fulfillment
            //
            // Fulfillment is created during checkout so the order has
            // a fulfillment record ready for later warehouse work.
            //
            // Warehouse allocation and physical warehouse reservation
            // intentionally do NOT happen during checkout.
            // ========================================================

            await fulfillmentService.CreateForOrderAsync(
                tenantId,
                order.Id,
                cancellationToken);

            // ========================================================
            // 2. Create logical inventory reservations
            //
            // OrderDto does not expose InventoryReservations, so we
            // retain the deterministic reservation keys locally.
            // ========================================================

            foreach (var item in order.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var reservationKey =
                    BuildReservationKey(
                        tenantId,
                        userId,
                        idempotencyKey,
                        item.ProductVariantId);

                reservationKeys.Add(
                    reservationKey);

                await orders.RecordInventoryReservationAsync(
                    tenantId,
                    userId,
                    order.Id,
                    reservationKey,
                    item.ProductVariantId,
                    item.Quantity,
                    DateTimeOffset.UtcNow.Add(
                        ReservationLifetime),
                    cancellationToken);

                // ====================================================
                // 3. Reserve GLOBAL inventory
                //
                // StockItem:
                //
                // Available -> Reserved
                //
                // This is the sellable/global inventory reservation.
                // ====================================================

                await inventory.ReserveAsync(
                    tenantId,
                    item.ProductVariantId,
                    item.Quantity,
                    reservationKey,
                    ReservationLifetime,
                    cancellationToken);
            }

            // ========================================================
            // 4. Return the latest order state.
            //
            // Warehouse allocation/reservation belongs to fulfillment
            // and will happen later through the fulfillment endpoints.
            // ========================================================

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
            // ========================================================
            // Rollback global inventory reservations created by this
            // checkout attempt.
            //
            // We intentionally do not access OrderDto.InventoryReservations
            // because that property does not exist in the current DTO.
            // ========================================================

            foreach (
                var reservationKey
                in reservationKeys
                    .Distinct(StringComparer.Ordinal))
            {
                try
                {
                    await inventory.ReleaseAsync(
                        tenantId,
                        reservationKey,
                        CancellationToken.None);
                }
                catch
                {
                    // Preserve the original checkout exception.
                    // Inventory reconciliation can repair a failed
                    // release later.
                }
            }

            // ========================================================
            // Cancel the order after inventory rollback has been
            // attempted.
            // ========================================================

            try
            {
                await orders.CancelAsync(
                    tenantId,
                    order.Id,
                    userId,
                    CancellationToken.None);
            }
            catch
            {
                // Preserve the original checkout exception.
            }

            throw;
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

        var raw =
            $"{tenantId.Trim()}|" +
            $"{userId.Trim()}|" +
            $"{idempotencyKey.Trim()}|" +
            $"{productVariantId:D}";

        var hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(raw));

        return
            $"checkout:{Convert.ToHexString(hash)}";
    }

    private static void ValidateInput(
        string tenantId,
        string userId,
        string idempotencyKey,
        CheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

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

