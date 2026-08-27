namespace NexaEcommerce.Modules.ShoppingCart.Application.DTOs;

public sealed record AddCartItemDto(
    Guid ProductVariantId,
    int Quantity);

public sealed record SetCartItemQuantityDto(
    Guid ProductVariantId,
    int Quantity);

public sealed record RemoveCartItemDto(
    Guid ProductVariantId);

public sealed record CartItemDto(
    Guid ProductVariantId,
    string ProductName,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record CartDto(
    Guid Id,
    string TenantId,
    IReadOnlyList<CartItemDto> Items,
    int TotalQuantity,
    decimal Subtotal)
{
    public static CartDto Empty(
        string tenantId)
    {
        return new(
            Guid.Empty,
            tenantId,
            [],
            0,
            0);
    }
}