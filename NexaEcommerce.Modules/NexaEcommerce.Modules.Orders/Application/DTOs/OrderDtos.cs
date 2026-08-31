namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record CheckoutLineDto(
    Guid ProductVariantId,
    int Quantity);

public sealed record CheckoutRequest(
    IReadOnlyList<CheckoutLineDto> Items,
    string ShippingFullName,
    string ShippingPhone,
    string ShippingAddress,
    string ShippingCity,
    string? ShippingPostalCode,
    decimal ShippingAmount = 0);

public sealed record OrderItemDto(
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string Currency,
    decimal Subtotal,
    decimal ShippingAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string ShippingFullName,
    string ShippingPhone,
    string ShippingAddress,
    string ShippingCity,
    string? ShippingPostalCode,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    string UserId,
    string Status,
    string Currency,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt);

public sealed record OrderListDto(
    IReadOnlyList<OrderListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);

public sealed record UpdateOrderStatusRequest(
    string Status);

public sealed record OrderStatusResultDto(
    Guid OrderId,
    string OrderNumber,
    string PreviousStatus,
    string CurrentStatus);