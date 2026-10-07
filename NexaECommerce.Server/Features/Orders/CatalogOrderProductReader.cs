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

                .Include(
                    x => x.Product)

                .Include(
                    x => x.AttributeValues)
                    .ThenInclude(
                        x => x.AttributeValue)
                        .ThenInclude(
                            x => x.ProductAttribute)

                .Include(
                    x => x.AttributeValues)
                    .ThenInclude(
                        x => x.AttributeValue)
                        .ThenInclude(
                            x => x.CatalogAttributeValue)

                .FirstOrDefaultAsync(
                    x =>
                        x.Id ==
                            productVariantId &&

                        !x.IsDeleted,
                    cancellationToken);

        if (variant is null)
        {
            return null;
        }

        var isPublished =
            variant.Product.IsPublished &&
            variant.Product.IsActive &&
            !variant.Product.IsDeleted;

        var discountPercentage =
            Math.Clamp(
                variant.Product.DiscountPercentage,
                0m,
                100m);

        var price =
            variant.PriceOverride -
            (
                variant.PriceOverride *
                discountPercentage /
                100m
            );

        var attributes =
            variant.AttributeValues
                .Where(
                    mapping =>
                        !mapping.IsDeleted &&
                        mapping.AttributeValue != null &&
                        mapping.AttributeValue.ProductAttribute != null)
                .Select(
                    mapping =>
                        new OrderProductAttributeSnapshot(
                            mapping.AttributeValueId,

                            mapping.AttributeValue!
                                .ProductAttributeId,

                            mapping.AttributeValue!
                                .ProductAttribute
                                .CatalogAttributeId,

                            mapping.AttributeValue!
                                .CatalogAttributeValueId,

                            mapping.AttributeValue!
                                .ProductAttribute
                                .Code,

                            mapping.AttributeValue!
                                .ProductAttribute
                                .Name,

                            (int)
                            mapping.AttributeValue!
                                .ProductAttribute
                                .Role,

                            mapping.AttributeValue!
                                .Value,

                            mapping.AttributeValue!
                                .DisplayValue,

                            mapping.AttributeValue!
                                .ColorHex))
                .OrderBy(
                    x => x.AttributeName)
                .ThenBy(
                    x => x.Value)
                .ToList();

        return new OrderProductSnapshot(
            variant.Id,
            variant.Sku,
            variant.Product.Name,
            price,
            variant.StockQuantity,
            variant.IsActive,
            isPublished,
            attributes);
    }
}