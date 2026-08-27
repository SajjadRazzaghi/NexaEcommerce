using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class OrderService(
    IOrderRepository repository,
    IOrderProductReader productReader,
    IUnitOfWork unitOfWork)
    : IOrderService
{
    public async Task<OrderDto> CreateFromCheckoutAsync(
        string tenantId,
        string userId,
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "Checkout must contain at least one item.");
        }

        if (string.IsNullOrWhiteSpace(request.ShippingFullName) ||
            string.IsNullOrWhiteSpace(request.ShippingPhone) ||
            string.IsNullOrWhiteSpace(request.ShippingAddress) ||
            string.IsNullOrWhiteSpace(request.ShippingCity))
        {
            throw new ArgumentException(
                "Shipping information is incomplete.");
        }

        if (request.Items.Any(x => x.Quantity <= 0))
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        var grouped =
            request.Items
                .GroupBy(x => x.ProductVariantId)
                .Select(x =>
                    new CheckoutLineDto(
                        x.Key,
                        x.Sum(i => i.Quantity)))
                .ToList();

        var orderNumber =
            GenerateOrderNumber();

        var order =
            Order.Create(
                tenantId,
                userId,
                orderNumber,
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

            if (line.Quantity >
                product.StockQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for '{product.ProductName}'.");
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
                .Select(x =>
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