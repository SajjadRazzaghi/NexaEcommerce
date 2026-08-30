using Microsoft.EntityFrameworkCore;
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
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));

        var normalizedTenantId =
            tenantId.Trim();

        var normalizedUserId =
            userId.Trim();

        var normalizedIdempotencyKey =
            idempotencyKey.Trim();

        if (normalizedIdempotencyKey.Length > 128)
            throw new ArgumentException(
                "Idempotency key cannot exceed 128 characters.",
                nameof(idempotencyKey));

        var existingOrder =
            await repository.GetByIdempotencyKeyAsync(
                normalizedTenantId,
                normalizedUserId,
                normalizedIdempotencyKey,
                cancellationToken);

        if (existingOrder is not null)
            return Map(existingOrder);

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "Checkout must contain at least one item.");
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

        if (request.ShippingAmount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.ShippingAmount));

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
                normalizedTenantId,
                normalizedUserId,
                GenerateOrderNumber(),
                normalizedIdempotencyKey,
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

        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            var persistedOrder =
                await repository.GetByIdempotencyKeyAsync(
                    normalizedTenantId,
                    normalizedUserId,
                    normalizedIdempotencyKey,
                    cancellationToken);

            if (persistedOrder is not null)
                return Map(persistedOrder);

            throw;
        }

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
            $"NX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N[..8].ToUpperInvariant()}";
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