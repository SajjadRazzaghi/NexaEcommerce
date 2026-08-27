using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;

namespace NexaECommerce.Server.Features.Cart;

public sealed class CatalogProductVariantReader(
    CatalogDbContext catalogDbContext)
    : IProductVariantReader
{
    public async Task<ProductVariantSnapshot?>
        GetSellableVariantAsync(
            Guid productVariantId,
            CancellationToken cancellationToken = default)
    {
        var variant =
            await catalogDbContext.ProductVariants
                .AsNoTracking()
                .Include(x => x.Product)
                    .ThenInclude(x => x.Images)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == productVariantId &&
                        x.IsActive &&
                        !x.IsDeleted &&
                        x.Product.IsActive &&
                        x.Product.IsPublished &&
                        !x.Product.IsDeleted,
                    cancellationToken);

        if (variant is null)
            return null;

        var image =
            variant.Product.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => x.ImageUrl)
                .FirstOrDefault();

        return new ProductVariantSnapshot(
            variant.Id,
            variant.PriceOverride,
            variant.StockQuantity,
            variant.Product.Name,
            image,
            variant.IsActive,
            variant.Product.IsPublished);
    }
}