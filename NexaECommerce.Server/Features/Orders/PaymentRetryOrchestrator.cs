using System.Security.Cryptography;
using System.Text;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PaymentRetryOrchestrator(
    IOrderRepository orderRepository,
    IOrderUnitOfWork orderUnitOfWork,
    IInventoryService inventory,
    IPaymentAttemptService paymentAttempts)
{
    private static readonly TimeSpan ReservationLifetime =
        TimeSpan.FromMinutes(15);

    public async Task<PaymentAttemptDto> RetryAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(
            tenantId,
            userId,
            orderId,
            idempotencyKey);

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

        if (order.Status ==
            OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cancelled orders cannot be retried.");
        }

        if (order.Status !=
            OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "Only orders pending payment can be retried.");
        }

        if (order.Items.Count == 0)
        {
            throw new InvalidOperationException(
                "The order does not contain any items.");
        }

        var createdReservations =
            new List<string>();

        try
        {
            foreach (var item in order.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var activeReservation =
                    order.InventoryReservations
                        .Where(
                            x =>
                                x.ProductVariantId ==
                                item.ProductVariantId &&
                                x.Status ==
                                InventoryReservationStatus.Reserved &&
                                x.ExpiresAt >
                                DateTimeOffset.UtcNow)
                        .OrderByDescending(
                            x => x.ExpiresAt)
                        .FirstOrDefault();

                if (activeReservation is not null)
                {
                    if (activeReservation.Quantity !=
                        item.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Active inventory reservation quantity does not match order item '{item.ProductVariantId}'.");
                    }

                    continue;
                }

                var reservationKey =
                    BuildReservationKey(
                        tenantId,
                        userId,
                        orderId,
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

                order.AddInventoryReservation(
                    reservationKey,
                    item.ProductVariantId,
                    item.Quantity,
                    reservation.ExpiresAt);

                createdReservations.Add(
                    reservationKey);
            }

            await orderUnitOfWork.SaveChangesAsync(
                cancellationToken);

            return await paymentAttempts.CreateAsync(
                tenantId,
                userId,
                order.Id,
                idempotencyKey,
                cancellationToken);
        }
        catch
        {
            foreach (var reservationKey in
                     createdReservations.AsEnumerable().Reverse())
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
                    /*
                     * Preserve the original failure.
                     * A later reconciliation process can recover
                     * a reservation which could not be released here.
                     */
                }
            }

            foreach (var reservation in
                     order.InventoryReservations)
            {
                if (!createdReservations.Contains(
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

            await orderUnitOfWork.SaveChangesAsync(
                cancellationToken);

            throw;
        }
    }

    private static string BuildReservationKey(
        string tenantId,
        string userId,
        Guid orderId,
        string idempotencyKey,
        Guid productVariantId)
    {
        var source =
            string.Join(
                "|",
                tenantId.Trim(),
                userId.Trim(),
                orderId.ToString("N"),
                idempotencyKey.Trim(),
                productVariantId.ToString("N"));

        var hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    source));

        return
            "retry:" +
            Convert.ToHexString(hash)
                .ToLowerInvariant();
    }

    private static void ValidateInput(
        string tenantId,
        string userId,
        Guid orderId,
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

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
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