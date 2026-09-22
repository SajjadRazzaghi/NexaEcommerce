using System.Security.Cryptography;
using System.Text;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaECommerce.Server.Features.Orders;

public sealed class CheckoutOrchestrator(
    IOrderService orders,
    WarehouseAllocationOrchestrator warehouseAllocation,
    WarehouseReservationOrchestrator warehouseReservation,
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

        try
        {
            /*
             * 1. Create fulfillment before payment.
             *
             * Warehouse selection is required before we can reserve
             * physical warehouse stock.
             */
            await fulfillmentService.CreateForOrderAsync(
                tenantId,
                order.Id,
                cancellationToken);

            /*
             * 2. Create logical order reservations.
             *
             * These are NOT physical stock reservations.
             * Physical stock is reserved only in WarehouseStock.
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
            }

            /*
             * Refresh order so the reservation collection contains
             * the records created above.
             */
            order =
                await orders.GetAsync(
                    tenantId,
                    order.Id,
                    userId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Order could not be reloaded after inventory reservation creation.");

            /*
             * 3. Select one warehouse.
             *
             * Allocation itself does not change stock.
             */
            await warehouseAllocation.AllocateAsync(
                tenantId,
                order.Id,
                cancellationToken);

            /*
             * 4. Reserve the physical stock exactly once.
             *
             * WarehouseStock.Reserve(...) is now the only physical
             * reservation performed during checkout.
             */
            await warehouseReservation.ReserveAsync(
                tenantId,
                order.Id,
                cancellationToken);

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
            /*
             * Cancellation releases warehouse reservations through
             * the normal cancellation workflow.
             */
            try
            {
                await warehouseReservation.ReleaseAsync(
      tenantId,
      order.Id,
      CancellationToken.None);
            }
            catch
            {
                // Preserve the original checkout exception.
            }

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

    private static void ValidateInput(
        string tenantId,
        string userId,
        string idempotencyKey,
        CheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

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