using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaECommerce.Server.Features.Cart;

public sealed class CatalogProductVariantReader(
    CatalogDbContext catalogDbContext,
    [FromServices] IStockReader stockReader,
    [FromServices] ICurrentTenant currentTenant)
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

                .Include(
                    x => x.Product)
                    .ThenInclude(
                        x => x.Images)

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

                        x.IsActive &&
                        !x.IsDeleted &&

                        x.Product.IsActive &&
                        x.Product.IsPublished &&
                        !x.Product.IsDeleted,
                    cancellationToken);

        if (
            variant is null
        )
        {
            return null;
        }

        var image =
            variant.Product.Images
                .OrderByDescending(
                    x => x.IsPrimary)
                .ThenBy(
                    x => x.DisplayOrder)
                .Select(
                    x => x.ImageUrl)
                .FirstOrDefault();

        var stockQuantity =
            await stockReader
                .GetAvailableQuantityAsync(
                    currentTenant.Id,
                    variant.Id,
                    cancellationToken)
            ?? 0;

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

                        mapping.AttributeValue !=
                            null &&

                        mapping.AttributeValue
                            .ProductAttribute !=
                            null &&

                        (
                            (
                                (int)
                                mapping
                                    .AttributeValue!
                                    .ProductAttribute!
                                    .Role
                            )
                            & 4
                        ) == 4)
                .Select(
                    mapping =>
                        new ProductVariantAttributeSnapshot(
                            mapping.AttributeValueId,

                            mapping.AttributeValue!
                                .ProductAttributeId,

                            mapping.AttributeValue!
                                .ProductAttribute!
                                .CatalogAttributeId,

                            mapping.AttributeValue!
                                .CatalogAttributeValueId,

                            mapping.AttributeValue!
                                .ProductAttribute!
                                .Code,

                            mapping.AttributeValue!
                                .ProductAttribute!
                                .Name,

                            (int)
                                mapping.AttributeValue!
                                    .ProductAttribute!
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

        return new ProductVariantSnapshot(
            variant.Id,
            variant.Sku,
            price,
            Math.Max(
                0,
                stockQuantity),
            variant.Product.Name,
            image,
            variant.IsActive,
            variant.Product.IsPublished,
            attributes);
    }
}