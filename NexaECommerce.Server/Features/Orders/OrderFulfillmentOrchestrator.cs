using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class OrderFulfillmentOrchestrator(
IOrderRepository orderRepository,
IOrderUnitOfWork unitOfWork,
[FromServices] IFulfillmentService fulfillmentService)
{
    public async Task<OrderFulfillmentStartResult> StartAsync(
    string tenantId,
    Guid orderId,
    CancellationToken cancellationToken = default)
    {
        ValidateInput(
        tenantId,
        orderId);


    var normalizedTenantId =
        tenantId.Trim();

        var order =
            await orderRepository.GetByIdAsync(
                normalizedTenantId,
                orderId,
                null,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        switch (order.Status)
        {
            case OrderStatus.PendingPayment:
                throw new InvalidOperationException(
                    "Only paid orders can enter fulfillment.");

            case OrderStatus.Cancelled:
                throw new InvalidOperationException(
                    "A cancelled order cannot enter fulfillment.");

            case OrderStatus.Shipped:
            case OrderStatus.Delivered:
                throw new InvalidOperationException(
                    "A shipped or delivered order cannot start fulfillment.");

            case OrderStatus.Paid:
                return await StartPaidOrderAsync(
                    normalizedTenantId,
                    order,
                    cancellationToken);

            case OrderStatus.Processing:
                return await EnsureProcessingOrderAsync(
                    normalizedTenantId,
                    order,
                    cancellationToken);

            default:
                throw new InvalidOperationException(
                    $"Order status '{order.Status}' is not valid for fulfillment.");
        }
    }

    private async Task<OrderFulfillmentStartResult>
        StartPaidOrderAsync(
            string tenantId,
            Order order,
            CancellationToken cancellationToken)
    {
        if (order.InventoryReservations.Count == 0)
        {
            throw new InvalidOperationException(
                "The order has no inventory reservations and cannot enter fulfillment.");
        }

        if (!order.AreAllInventoryReservationsCommitted)
        {
            throw new InvalidOperationException(
                "All inventory reservations must be committed before fulfillment can start.");
        }

        order.StartProcessing();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        var fulfillment =
            await CreateOrGetFulfillmentAsync(
                tenantId,
                order.Id,
                cancellationToken);

        return new OrderFulfillmentStartResult(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            fulfillment,
            false);
    }

    private async Task<OrderFulfillmentStartResult>
        EnsureProcessingOrderAsync(
            string tenantId,
            Order order,
            CancellationToken cancellationToken)
    {
        var existing =
            await fulfillmentService.GetByOrderAsync(
                tenantId,
                order.Id,
                cancellationToken);

        if (existing is null)
        {
            existing =
                await CreateOrGetFulfillmentAsync(
                    tenantId,
                    order.Id,
                    cancellationToken);
        }

        return new OrderFulfillmentStartResult(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            existing,
            true);
    }

    private async Task<FulfillmentDto>
        CreateOrGetFulfillmentAsync(
            string tenantId,
            Guid orderId,
            CancellationToken cancellationToken)
    {
        var existing =
            await fulfillmentService.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        try
        {
            return await fulfillmentService.CreateForOrderAsync(
                tenantId,
                orderId,
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            /*
             * Another request may have created the unique fulfillment
             * record between our existence check and INSERT.
             *
             * Treat that race as idempotent and read the winner.
             */
            var raced =
                await fulfillmentService.GetByOrderAsync(
                    tenantId,
                    orderId,
                    cancellationToken);

            if (raced is not null)
            {
                return raced;
            }

            throw;
        }
    }

    private static void ValidateInput(
        string tenantId,
        Guid orderId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }
    }


}

public sealed record OrderFulfillmentStartResult(
Guid OrderId,
string OrderNumber,
string OrderStatus,
FulfillmentDto Fulfillment,
bool AlreadyStarted);
