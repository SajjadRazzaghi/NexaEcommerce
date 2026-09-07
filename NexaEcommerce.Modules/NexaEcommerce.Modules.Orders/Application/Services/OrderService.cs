using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Pricing;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class OrderService(
    IOrderRepository repository,
    IOrderProductReader productReader,
    IOrderUnitOfWork unitOfWork,
    IShippingMethodService shippingMethods,
    IPricingCalculator pricingCalculator,
    ICouponService couponService,
    ITaxRateService taxRateService)
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
                    x => x.ProductVariantId)
                .Select(
                    x =>
                        new CheckoutLineDto(
                            x.Key,
                            x.Sum(
                                item => item.Quantity)))
                .ToList();

        var order =
            Order.Create(
                tenantId,
                userId,
                GenerateOrderNumber(),
                idempotencyKey,
                "IRR",
                0m,
                shippingQuote.Price,
                0m,
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

            if (product.Price < 0m)
            {
                throw new InvalidOperationException(
                    $"Product variant {line.ProductVariantId} has an invalid price.");
            }

            order.AddItem(
                product.Id,
                product.Sku,
                product.ProductName,
                product.Price,
                line.Quantity);
        }

        /*
         * ------------------------------------------------------------
         * Financial pricing
         * ------------------------------------------------------------
         *
         * All amounts below are calculated on the backend.
         *
         * Frontend totals are never trusted.
         */

        var subtotal =
            order.Items.Sum(
                x => x.LineTotal);

        decimal discountAmount = 0m;

        string? normalizedCouponCode = null;

        if (!string.IsNullOrWhiteSpace(
                request.CouponCode))
        {
            normalizedCouponCode =
                request.CouponCode
                    .Trim()
                    .ToUpperInvariant();

            var couponValidation =
                await couponService.ValidateAsync(
                    tenantId,
                    userId,
                    normalizedCouponCode,
                    subtotal,
                    cancellationToken);

            if (!couponValidation.IsValid)
            {
                throw new InvalidOperationException(
                    couponValidation.Message ??
                    $"Coupon '{normalizedCouponCode}' is not valid.");
            }

            discountAmount =
                couponValidation.DiscountAmount;
        }

        decimal taxRatePercent = 0m;

        if (request.TaxRateId.HasValue)
        {
            var taxRate =
                await taxRateService.GetAsync(
                    tenantId,
                    request.TaxRateId.Value,
                    cancellationToken);

            if (taxRate is null)
            {
                throw new InvalidOperationException(
                    "Selected tax rate was not found.");
            }

            if (!taxRate.IsActive)
            {
                throw new InvalidOperationException(
                    "Selected tax rate is not active.");
            }

            taxRatePercent =
                taxRate.RatePercent;
        }
        else
        {
            var defaultTax =
                await taxRateService.CalculateDefaultAsync(
                    tenantId,
                    Math.Max(
                        0m,
                        subtotal -
                        discountAmount),
                    cancellationToken);

            if (defaultTax is not null)
            {
                taxRatePercent =
                    defaultTax.RatePercent;
            }
        }

        var pricing =
            pricingCalculator.Calculate(
                new PricingInput(
                    subtotal,
                    shippingQuote.Price,
                    discountAmount,
                    taxRatePercent));

        order.ApplyPricing(
            pricing.Subtotal,
            pricing.ShippingAmount,
            pricing.DiscountAmount,
            pricing.TaxableAmount,
            pricing.TaxRatePercent,
            pricing.TaxAmount,
            pricing.TotalAmount,
            normalizedCouponCode);

        /*
         * Coupon redemption belongs to the same business operation.
         *
         * CouponService has its own idempotent per-order redemption check.
         */
        if (normalizedCouponCode is not null)
        {
            await couponService.RedeemAsync(
                tenantId,
                userId,
                order.Id,
                normalizedCouponCode,
                pricing.Subtotal,
                cancellationToken);
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
            case OrderStatus.Processing:
                order.StartProcessing();
                break;

            case OrderStatus.Cancelled:
                order.Cancel();
                break;

            case OrderStatus.Paid:
                throw new InvalidOperationException(
                    "Paid status can only be established by successful payment completion.");

            case OrderStatus.Shipped:
                throw new InvalidOperationException(
                    "Shipped status can only be established by the shipment workflow.");

            case OrderStatus.Delivered:
                throw new InvalidOperationException(
                    "Delivered status can only be established by the shipment workflow.");

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
        ValidateInventoryReservation(
            tenantId,
            userId,
            orderId,
            reservationKey,
            productVariantId,
            quantity,
            expiresAt);

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
        ArgumentNullException.ThrowIfNull(
            request);

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

        if (idempotencyKey.Trim().Length > 128)
        {
            throw new ArgumentException(
                "Idempotency key cannot exceed 128 characters.",
                nameof(idempotencyKey));
        }

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "Checkout must contain at least one item.",
                nameof(request));
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
                "Shipping information is incomplete.",
                nameof(request));
        }

        if (request.Items.Any(
                x =>
                    x.ProductVariantId ==
                    Guid.Empty))
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(request));
        }

        if (request.Items.Any(
                x =>
                    x.Quantity <= 0))
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.",
                nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(
                request.CouponCode) &&
            request.CouponCode.Trim().Length > 64)
        {
            throw new ArgumentException(
                "Coupon code cannot exceed 64 characters.",
                nameof(request.CouponCode));
        }

        if (request.TaxRateId.HasValue &&
            request.TaxRateId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Tax rate id is invalid.",
                nameof(request.TaxRateId));
        }
    }

    private static void ValidateInventoryReservation(
        string tenantId,
        string userId,
        Guid orderId,
        string reservationKey,
        Guid productVariantId,
        int quantity,
        DateTimeOffset expiresAt)
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

        if (string.IsNullOrWhiteSpace(
                reservationKey))
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
            order.TaxableAmount,
            order.TaxRatePercent,
            order.TaxAmount,
            order.TotalAmount,
            order.CouponCode,
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