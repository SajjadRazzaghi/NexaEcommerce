using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class FulfillmentService(
    IFulfillmentRepository fulfillmentRepository,
    IOrderRepository orderRepository,
    IOrderUnitOfWork unitOfWork)
    : IFulfillmentService
{
    public async Task<FulfillmentDto?> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        var fulfillment =
            await fulfillmentRepository.GetByOrderAsync(
                tenantId.Trim(),
                orderId,
                cancellationToken);

        return fulfillment is null
            ? null
            : Map(fulfillment);
    }

    public async Task<FulfillmentDto> CreateForOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

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

        if (order.Status is
            OrderStatus.PendingPayment or
            OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Only paid or processing orders can enter fulfillment.");
        }

        var existing =
            await fulfillmentRepository.GetByOrderAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        if (existing is not null)
        {
            return Map(existing);
        }

        var fulfillment =
            Fulfillment.Create(
                orderId,
                normalizedTenantId);

        await fulfillmentRepository.AddAsync(
            fulfillment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(fulfillment);
    }

    public async Task<IReadOnlyList<FulfillmentQueueItemDto>>
        GetQueueAsync(
            string tenantId,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skip));
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take));
        }

        take = Math.Min(
            take,
            200);

        var normalizedTenantId =
            tenantId.Trim();

        var fulfillments =
            await fulfillmentRepository.GetByStatusesAsync(
                normalizedTenantId,
                new[]
                {
                    FulfillmentStatus.Pending,
                    FulfillmentStatus.Picking,
                    FulfillmentStatus.Picked,
                    FulfillmentStatus.Packing,
                    FulfillmentStatus.Packed,
                    FulfillmentStatus.ReadyToShip
                },
                skip,
                take,
                cancellationToken);

        var result =
            new List<FulfillmentQueueItemDto>(
                fulfillments.Count);

        foreach (var fulfillment in fulfillments)
        {
            var order =
                await orderRepository.GetByIdAsync(
                    normalizedTenantId,
                    fulfillment.OrderId,
                    null,
                    cancellationToken);

            if (order is null)
            {
                continue;
            }

            result.Add(
                new FulfillmentQueueItemDto(
                    Map(fulfillment),
                    order.Id,
                    order.OrderNumber,
                    order.ShippingFullName,
                    order.ShippingPhone,
                    order.ShippingAddress,
                    order.ShippingCity,
                    order.ShippingPostalCode,
                    order.TotalAmount,
                    order.Currency,
                    order.Items.Count));
        }

        return result;
    }

    public Task<FulfillmentDto> AssignWarehouseAsync(
        string tenantId,
        Guid orderId,
        Guid warehouseId,
        Guid? pickingLocationId,
        CancellationToken cancellationToken = default)
    {
        return Execute(
            tenantId,
            orderId,
            fulfillment =>
            {
                fulfillment.AssignWarehouse(
                    warehouseId,
                    pickingLocationId);
            },
            cancellationToken);
    }

    public Task<FulfillmentDto> StartPickingAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return Execute(
            tenantId,
            orderId,
            fulfillment =>
            {
                fulfillment.StartPicking();
            },
            cancellationToken);
    }

    public Task<FulfillmentDto> MarkPickedAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return Execute(
            tenantId,
            orderId,
            fulfillment =>
            {
                fulfillment.MarkPicked();
            },
            cancellationToken);
    }

    public Task<FulfillmentDto> StartPackingAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return Execute(
            tenantId,
            orderId,
            fulfillment =>
            {
                fulfillment.StartPacking();
            },
            cancellationToken);
    }

    public Task<FulfillmentDto> MarkPackedAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return Execute(
            tenantId,
            orderId,
            fulfillment =>
            {
                fulfillment.MarkPacked();
            },
            cancellationToken);
    }

    public Task<FulfillmentDto> MarkReadyToShipAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return Execute(
            tenantId,
            orderId,
            fulfillment =>
            {
                fulfillment.MarkReadyToShip();
            },
            cancellationToken);
    }

    private async Task<FulfillmentDto> Execute(
        string tenantId,
        Guid orderId,
        Action<Fulfillment> action,
        CancellationToken cancellationToken)
    {
        ValidateTenant(tenantId);

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        var fulfillment =
            await fulfillmentRepository.GetByOrderAsync(
                tenantId.Trim(),
                orderId,
                cancellationToken);

        if (fulfillment is null)
        {
            throw new KeyNotFoundException(
                "Fulfillment was not found for this order.");
        }

        action(
            fulfillment);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(fulfillment);
    }

    private static void ValidateTenant(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }

    private static FulfillmentDto Map(
        Fulfillment fulfillment)
    {
        return new FulfillmentDto(
            fulfillment.Id,
            fulfillment.OrderId,
            fulfillment.Status.ToString(),
            fulfillment.WarehouseId,
            fulfillment.PickingLocationId,
            fulfillment.PickingStartedAt,
            fulfillment.PickedAt,
            fulfillment.PackingStartedAt,
            fulfillment.PackedAt,
            fulfillment.ReadyToShipAt,
            fulfillment.ShippedAt,
            fulfillment.DeliveredAt,
            fulfillment.CreatedAt,
            fulfillment.UpdatedAt);
    }
}
