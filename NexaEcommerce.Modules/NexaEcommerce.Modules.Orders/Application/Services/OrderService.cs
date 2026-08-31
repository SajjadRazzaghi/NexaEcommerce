using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class OrderService(
    IOrderRepository repository,
    IOrderProductReader productReader,
    IOrderUnitOfWork unitOfWork,
    IShippingMethodService shippingMethods)
    : IOrderService
{
    public async Task<OrderDto> CreateFromCheckoutAsync(
        string tenantId,
        string userId,
        string idempotencyKey,
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCheckout(
            tenantId,
            userId,
            idempotencyKey,
            request);

        var existing =
            await repository.GetByIdempotencyKeyAsync(
                tenantId,
                userId,
                idempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            return Map(existing);
        }

        var shippingQuote =
            await shippingMethods.QuoteAsync(
                tenantId,
                request.ShippingMethodId,
                cancellationToken);

        var grouped =
            request.Items
                .GroupBy(
                    x =>
                        x.ProductVariantId)
                .Select(
                    x =>
                        new CheckoutLineDto(
                            x.Key,
                            x.Sum(
                                item =>
                                    item.Quantity)))
                .ToList();

        var order =
            Order.Create(
                tenantId,
                userId,
                GenerateOrderNumber(),
                idempotencyKey,
                "IRR",
                0,
                shippingQuote.Price,
                0,
                request.ShippingFullName,
                request.ShippingPhone,
                request.ShippingAddress,
                request.ShippingCity,
                request.ShippingPostalCode);

        foreach (var line in grouped)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var product =
                await productReader.GetAsync(
                    line.ProductVariantId,
                    cancellationToken);

            if (product is null ||
                !product.IsActive ||
                !product.IsPublished)
            {
                throw new InvalidOperationException(
                    $"Product variant {line.ProductVariantId} is no longer available.");
            }

            order.AddItem(
                product.Id,
                product.Sku,
                product.ProductName,
                product.Price,
                line.Quantity);
        }

        await repository.AddAsync(
            order,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(order);
    }

    public async Task<OrderDto?> GetAsync(
        string tenantId,
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var order =
            await repository.GetByIdAsync(
                tenantId,
                id,
                userId,
                cancellationToken);

        return order is null
            ? null
            : Map(order);
    }

    public async Task<OrderListDto> GetUserOrdersAsync(
        string tenantId,
        string userId,
        int page,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        NormalizePaging(
            ref page,
            ref pageSize);

        var total =
            await repository.CountUserOrdersAsync(
                tenantId,
                userId,
                status,
                cancellationToken);

        var orders =
            await repository.GetUserOrdersAsync(
                tenantId,
                userId,
                page,
                pageSize,
                status,
                cancellationToken);

        return CreateList(
            orders,
            page,
            pageSize,
            total);
    }

    public async Task<OrderListDto> GetTenantOrdersAsync(
        string tenantId,
        int page,
        int pageSize,
        string? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        NormalizePaging(
            ref page,
            ref pageSize);

        var total =
            await repository.CountTenantOrdersAsync(
                tenantId,
                status,
                search,
                cancellationToken);

        var orders =
            await repository.GetTenantOrdersAsync(
                tenantId,
                page,
                pageSize,
                status,
                search,
                cancellationToken);

        return CreateList(
            orders,
            page,
            pageSize,
            total);
    }

    public async Task<OrderStatusResultDto> UpdateStatusAsync(
        string tenantId,
        Guid orderId,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException(
                "Status is required.",
                nameof(status));
        }

        if (!Enum.TryParse<OrderStatus>(
                status,
                true,
                out var targetStatus))
        {
            throw new ArgumentException(
                $"Unknown order status '{status}'.",
                nameof(status));
        }

        var order =
            await repository.GetByIdAsync(
                tenantId,
                orderId,
                null,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        var previous =
            order.Status;

        if (previous == targetStatus)
        {
            return new OrderStatusResultDto(
                order.Id,
                order.OrderNumber,
                previous.ToString(),
                targetStatus.ToString());
        }

        switch (targetStatus)
        {
            case OrderStatus.Paid:
                order.MarkPaid();
                break;

            case OrderStatus.Processing:
                order.StartProcessing();
                break;

            case OrderStatus.Shipped:
                order.MarkShipped();
                break;

            case OrderStatus.Delivered:
                order.MarkDelivered();
                break;

            case OrderStatus.Cancelled:
                order.Cancel();
                break;

            case OrderStatus.PendingPayment:
                throw new InvalidOperationException(
                    "An order cannot be moved back to PendingPayment.");

            default:
                throw new ArgumentException(
                    $"Unsupported order status '{status}'.",
                    nameof(status));
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new OrderStatusResultDto(
            order.Id,
            order.OrderNumber,
            previous.ToString(),
            order.Status.ToString());
    }

    public async Task CancelAsync(
        string tenantId,
        Guid orderId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var order =
            await repository.GetByIdAsync(
                tenantId,
                orderId,
                userId,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        order.Cancel();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<OrderDto> RecordInventoryReservationAsync(
        string tenantId,
        string userId,
        Guid orderId,
        string reservationKey,
        Guid productVariantId,
        int quantity,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
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

        if (string.IsNullOrWhiteSpace(reservationKey))
        {
            throw new ArgumentException(
                "Reservation key is required.",
                nameof(reservationKey));
        }

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(
                "Reservation expiration must be in the future.",
                nameof(expiresAt));
        }

        var order =
            await repository.GetByIdAsync(
                tenantId,
                orderId,
                userId,
                cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException(
                "Order was not found.");
        }

        if (order.Status !=
            OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "Inventory reservations can only be recorded for orders pending payment.");
        }

        order.AddInventoryReservation(
            reservationKey,
            productVariantId,
            quantity,
            expiresAt);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(order);
    }

    private static OrderListDto CreateList(
        IReadOnlyList<Order> orders,
        int page,
        int pageSize,
        int totalItems)
    {
        var totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)pageSize);

        return new OrderListDto(
            orders
                .Select(
                    x =>
                        new OrderListItemDto(
                            x.Id,
                            x.OrderNumber,
                            x.UserId,
                            x.Status.ToString(),
                            x.Currency,
                            x.TotalAmount,
                            x.Items.Count,
                            x.CreatedAt))
                .ToList(),
            page,
            pageSize,
            totalItems,
            totalPages,
            page > 1,
            page < totalPages);
    }

    private static void NormalizePaging(
        ref int page,
        ref int pageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }
    }

    private static void ValidateCheckout(
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

        if (idempotencyKey.Length > 128)
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

        if (request.ShippingMethodId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Shipping method is required.",
                nameof(request.ShippingMethodId));
        }

        if (string.IsNullOrWhiteSpace(
                request.ShippingFullName) ||
            string.IsNullOrWhiteSpace(
                request.ShippingPhone) ||
            string.IsNullOrWhiteSpace(
                request.ShippingAddress) ||
            string.IsNullOrWhiteSpace(
                request.ShippingCity))
        {
            throw new ArgumentException(
                "Shipping information is incomplete.");
        }

        if (request.Items.Any(
                x =>
                    x.ProductVariantId ==
                    Guid.Empty))
        {
            throw new ArgumentException(
                "Product variant id is required.");
        }

        if (request.Items.Any(
                x =>
                    x.Quantity <= 0))
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }
    }

    private static string GenerateOrderNumber()
    {
        return
            $"NX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }

    private static OrderDto Map(
        Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.Currency,
            order.Subtotal,
            order.ShippingAmount,
            order.DiscountAmount,
            order.TotalAmount,
            order.ShippingFullName,
            order.ShippingPhone,
            order.ShippingAddress,
            order.ShippingCity,
            order.ShippingPostalCode,
            order.Items
                .Select(
                    x =>
                        new OrderItemDto(
                            x.ProductVariantId,
                            x.Sku,
                            x.ProductName,
                            x.UnitPrice,
                            x.Quantity,
                            x.LineTotal))
                .ToList());
    }
}
