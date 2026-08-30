using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class OrderService(
    IOrderRepository repository,
    IOrderProductReader productReader,
    IOrderUnitOfWork unitOfWork)
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

        var grouped =
            request.Items
                .GroupBy(
                    x => x.ProductVariantId)
                .Select(
                    x =>
                        new CheckoutLineDto(
                            x.Key,
                            x.Sum(
                                i => i.Quantity)))
                .ToList();

        var order =
            Order.Create(
                tenantId,
                userId,
                GenerateOrderNumber(),
                idempotencyKey,
                "IRR",
                0,
                request.ShippingAmount,
                0,
                request.ShippingFullName,
                request.ShippingPhone,
                request.ShippingAddress,
                request.ShippingCity,
                request.ShippingPostalCode);

        foreach (var line in grouped)
        {
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

            /*
             * Catalog data is used for product identity,
             * SKU and price snapshot.
             *
             * Inventory is the authoritative source for
             * available stock.
             */
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
            throw new InvalidOperationException(
                "Order was not found.");
        }

        order.Cancel();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);
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

        if (request.ShippingAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.ShippingAmount));
        }

        if (request.Items.Any(
                x => x.ProductVariantId == Guid.Empty))
        {
            throw new ArgumentException(
                "Product variant id is required.");
        }

        if (request.Items.Any(
                x => x.Quantity <= 0))
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