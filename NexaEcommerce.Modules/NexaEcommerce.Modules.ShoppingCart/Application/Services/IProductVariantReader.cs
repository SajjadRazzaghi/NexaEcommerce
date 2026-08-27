namespace NexaEcommerce.Modules.ShoppingCart.Application.Services;

public interface IProductVariantReader
{
    Task<ProductVariantSnapshot?> GetSellableVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}

public sealed record ProductVariantSnapshot(
    Guid Id,
    decimal Price,
    int StockQuantity,
    string ProductName,
    string? ImageUrl,
    bool IsActive,
    bool IsPublished);