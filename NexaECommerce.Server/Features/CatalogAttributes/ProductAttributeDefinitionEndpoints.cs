
using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaECommerce.Server.Features.Products;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;

namespace NexaECommerce.Server.Features.CatalogAttributes;

public sealed class ProductAttributeDefinitionEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/catalog/attribute-definitions")
            .WithTags("Product Attribute Definitions");

        group.MapGet("/", GetForCategories)
            .RequirePermission(ProductPermissions.Read);
    }

    private static async Task<IResult> GetForCategories(
        [FromQuery] string? categoryIds,
        [FromQuery] Guid? productId,
        [FromServices] IProductAttributeDefinitionService service,
        CancellationToken ct)
    {
        var ids = new List<Guid>();

        if (!string.IsNullOrWhiteSpace(categoryIds))
        {
            foreach (var raw in categoryIds.Split(
                         ',',
                         StringSplitOptions.TrimEntries |
                         StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Guid.TryParse(raw, out var id) ||
                    id == Guid.Empty)
                {
                    return Results.BadRequest(new
                    {
                        error = $"Invalid category ID: '{raw}'."
                    });
                }

                ids.Add(id);
            }
        }

        var result = await service.GetForCategoriesAsync(
            ids.Distinct().ToArray(),
            productId,
            ct);

        return Results.Ok(result);
    }
}
