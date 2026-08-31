namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record ShippingMethodDto(
    Guid Id,
    string Code,
    string Name,
    string Carrier,
    decimal Price,
    int SortOrder,
    bool IsActive);

public sealed record CreateShippingMethodRequest(
    string Code,
    string Name,
    string Carrier,
    decimal Price,
    int SortOrder = 0);

public sealed record UpdateShippingMethodRequest(
    string Name,
    string Carrier,
    decimal Price,
    int SortOrder);

public sealed record ShippingQuoteDto(
    Guid ShippingMethodId,
    string Code,
    string Name,
    string Carrier,
    decimal Price);
