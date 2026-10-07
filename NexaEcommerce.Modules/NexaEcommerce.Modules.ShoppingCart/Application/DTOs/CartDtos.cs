namespace NexaEcommerce.Modules.ShoppingCart.Application.DTOs;

public sealed record AddCartItemDto(
    Guid ProductVariantId,
    int Quantity);

public sealed record SetCartItemQuantityDto(
    Guid ProductVariantId,
    int Quantity);

public sealed record RemoveCartItemDto(
    Guid ProductVariantId);

public sealed record CartItemAttributeDto(
    Guid AttributeValueId,
    Guid ProductAttributeId,
    Guid? CatalogAttributeId,
    Guid? CatalogAttributeValueId,
    string AttributeCode,
    string AttributeName,
    int RoleValue,
    string Value,
    string? DisplayValue,
    string? ColorHex);

public sealed record CartItemDto(
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int AvailableStock,
    IReadOnlyList<CartItemAttributeDto> Attributes);

public sealed record CartDto(
    Guid Id,
    string TenantId,
    IReadOnlyList<CartItemDto> Items,
    int TotalQuantity,
    decimal Subtotal,
    string Currency,
    decimal TotalAmount)
{
    public static CartDto Empty(
        string tenantId)
    {
        return new(
            Guid.Empty,
            tenantId,
            [],
            0,
            0,
            "IRR",
            0);
    }
}