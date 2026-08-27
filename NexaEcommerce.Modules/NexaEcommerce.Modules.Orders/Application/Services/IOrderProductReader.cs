namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IOrderProductReader
{
    Task<OrderProductSnapshot?> GetAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}

public sealed record OrderProductSnapshot(
    Guid Id,
    string Sku,
    string ProductName,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    bool IsPublished);