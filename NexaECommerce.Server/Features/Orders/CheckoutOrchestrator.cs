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

        var order =
            await orders.CreateFromCheckoutAsync(
                tenantId,
                userId,
                idempotencyKey,
                request,
                cancellationToken);

        var reservationKeys =
            new List<string>(
                order.Items.Count);

        try
        {
            foreach (var item in order.Items)
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

            return order;
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
                    // Preserve original checkout failure.
                }
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