namespace NexaEcommerce.Modules.ShoppingCart.Application.Services;

public interface IProductVariantReader
{
    Task<ProductVariantSnapshot?> GetSellableVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}

public sealed record ProductVariantAttributeSnapshot(
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

public sealed record ProductVariantSnapshot(
    Guid Id,
    string Sku,
    decimal Price,
    int StockQuantity,
    string ProductName,
    string? ImageUrl,
    bool IsActive,
    bool IsPublished,
    IReadOnlyList<ProductVariantAttributeSnapshot> Attributes);