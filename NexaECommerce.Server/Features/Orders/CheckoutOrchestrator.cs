using System.Security.Cryptography;
using System.Text;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaECommerce.Server.Features.Orders;

public sealed class CheckoutOrchestrator(
    IOrderService orders,
    IInventoryService inventory,
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
             * Warehouse selection is required before physical
             * warehouse stock can be reserved.
             */
            await fulfillmentService.CreateForOrderAsync(
                tenantId,
                order.Id,
                cancellationToken);

            /*
             * 2. Create logical order reservations.
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
             * Refresh the order so the reservation collection contains
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
             * 3. Reserve global inventory.
             *
             * StockItem represents the global sellable inventory.
             * This reservation moves quantity from AvailableQuantity
             * to ReservedQuantity.
             *
             * ReserveAsync is idempotent by reservation key.
             */
            foreach (var reservation in order.InventoryReservations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reservation.Status is
                    not NexaEcommerce.Modules.Orders.Domain.Entities.InventoryReservationStatus.Reserved
                    and not NexaEcommerce.Modules.Orders.Domain.Entities.InventoryReservationStatus.Committed)
                {
                    continue;
                }

                await inventory.ReserveAsync(
                    tenantId,
                    reservation.ProductVariantId,
                    reservation.Quantity,
                    reservation.ReservationKey,
                    ReservationLifetime,
                    cancellationToken);
            }

            /*
             * 4. Select one warehouse.
             *
             * Allocation itself does not change stock.
             */
            await warehouseAllocation.AllocateAsync(
                tenantId,
                order.Id,
                cancellationToken);

            /*
             * 5. Reserve physical warehouse stock.
             *
             * WarehouseStock.Reserve(...) changes the physical
             * warehouse availability.
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
             * Release physical warehouse reservations first.
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

            /*
             * Release global StockItem reservations.
             *
             * Inventory release is intentionally best-effort here.
             * Inventory reconciliation can repair a reservation if
             * the release could not be completed immediately.
             */
            try
            {
                var failedOrder =
                    await orders.GetAsync(
                        tenantId,
                        order.Id,
                        userId,
                        CancellationToken.None);

                if (failedOrder is not null)
                {
                    foreach (var reservation in
                             failedOrder.InventoryReservations)
                    {
                        if (reservation.Status !=
                            NexaEcommerce.Modules.Orders.Domain.Entities.InventoryReservationStatus.Reserved)
                        {
                            continue;
                        }

                        try
                        {
                            await inventory.ReleaseAsync(
                                tenantId,
                                reservation.ReservationKey,
                                CancellationToken.None);
                        }
                        catch
                        {
                            // Preserve the original checkout exception.
                        }
                    }
                }
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
            throw