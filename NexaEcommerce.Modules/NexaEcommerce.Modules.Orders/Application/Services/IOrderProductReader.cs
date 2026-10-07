namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IOrderProductReader
{
    Task<OrderProductSnapshot?> GetAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}

public sealed record OrderProductAttributeSnapshot(
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

public sealed record OrderProductSnapshot(
    Guid Id,
    string Sku,
    string ProductName,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    bool IsPublished,
    IReadOnlyList<OrderProductAttributeSnapshot> Attributes);