using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using NexaECommerce.Server.Features.Products;

namespace NexaECommerce.Server.Features.Products;

public sealed class ProductInventoryEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/products/{id:guid}/inventory/sync",
                Sync)
            .WithTags("Products")
            .RequirePermission(ProductPermissions.Update);
    }

    private static async Task<IResult> Sync(
        Guid id,
        IProductService productService,
        ProductInventorySynchronizer inventorySynchronizer,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        var product =
            await productService.GetByIdAsync(
                id,
                ct);

        if (product is null)
        {
            return Results.NotFound(
                new
                {
                    error = "Product not found."
                });
        }

        await inventorySynchronizer.SyncMissingStockAsync(
            currentTenant.Id,
            product,
            ct);

        return Results.Ok(
            new
            {
                message = "Product inventory synchronized.",
                productId = product.Id,
                productName = product.Name,
                variants = product.Variants.Count
            });
    }
}