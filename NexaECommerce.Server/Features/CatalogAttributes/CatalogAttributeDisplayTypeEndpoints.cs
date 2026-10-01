using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaECommerce.Server.Platform.Features;

namespace NexaECommerce.Server.Features.CatalogAttributes;

public sealed class CatalogAttributeDisplayTypeEndpoints
    : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/catalog/attribute-display-types")
                .WithTags("Catalog Attribute Display Types");

        group.MapGet("/", List)
            .AllowAnonymous();
    }

    private static async Task<IResult> List(
        [FromServices]
        ICatalogAttributeDisplayTypeService service,
        CancellationToken ct)
    {
        var result =
            await service.GetAllActiveAsync(ct);

        return Results.Ok(result);
    }
}