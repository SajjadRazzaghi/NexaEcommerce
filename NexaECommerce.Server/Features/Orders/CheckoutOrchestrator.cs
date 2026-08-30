using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaECommerce.Server.Features.Orders;

public sealed class CheckoutOrchestrator(
    IInventoryService inventory,
    IOrderService orders)
{
    public async Task<OrderDto> ExecuteAsync(
        string tenantId,
        string userId,
        string idempotencyKey,
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing =
            await orders.CreateFromCheckoutAsync(
                tenantId,
                userId,
                idempotencyKey,
                request,
                cancellationToken);

        /*
         * If the idempotency key already belongs to an order,
         * do not create another reservation flow here.
         *
         * The caller can safely retry the HTTP request.
         */
        if (existing.Status !=
            "PendingPayment")
        {
            return existing;
        }

        var reservationKeys =
            new List<string>(
                existing.Items.Count);

        try
        {
            foreach (var item in existing.Items)
            {
                var reservationKey =
                    BuildReservationKey(
                        tenantId,
                        userId,
                        idempotencyKey,
                        item.ProductVariantId);

                await inventory.ReserveAsync(
                    tenantId,
                    item.ProductVariantId,
                    item.Quantity,
                    reservationKey,
                    TimeSpan.FromMinutes(15),
                    cancellationToken);

                reservationKeys.Add(
                    reservationKey);
            }

            return existing;
        }
        catch
        {
            foreach (var reservationKey
                     in reservationKeys)
            {
                try
                {
                    await inventory.ReleaseAsync(
                        tenantId,
                        reservationKey,
                        cancellationToken);
                }
                catch
                {
                    // Preserve original reservation failure.
                }
            }

            try
            {
                await orders.CancelAsync(
                    tenantId,
                    existing.Id,
                    userId,
                    cancellationToken);
            }
            catch
            {
                // Preserve the original checkout failure.
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