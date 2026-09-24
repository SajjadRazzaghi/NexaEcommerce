using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Orders.Application.Services;

namespace NexaECommerce.Server.Features.Orders;

public sealed class CatalogOrderProductReader(
    CatalogDbContext catalog)
    : IOrderProductReader
{
    public async Task<OrderProductSnapshot?> GetAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var variant =
            await catalog.ProductVariants
                .AsNoTracking()
                .Where(x =>
                    x.Id == productVariantId &&
                    !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.Sku,
                    ProductName = x.Product.Name,
                    BasePrice = x.PriceOverride,
                    DiscountPercentage =
                        x.Product.DiscountPercentage,
                    x.StockQuantity,
                    x.IsActive,
                    IsPublished =
                        x.Product.IsPublished &&
                        x.Product.IsActive &&
                        !x.Product.IsDeleted
                })
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (variant is null)
        {
            return null;
        }

        var discountPercentage =
            Math.Clamp(
                variant.DiscountPercentage,
                0m,
                100m);

        var price =
            variant.BasePrice -
            (
                variant.BasePrice *
                discountPercentage /
                100m
            );

        return new OrderProductSnapshot(
            variant.Id,
            variant.Sku,
            variant.ProductName,
            price,
            variant.StockQuantity,
            variant.IsActive,
            variant.IsPublished);
    }
}