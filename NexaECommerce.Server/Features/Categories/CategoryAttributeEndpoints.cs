
using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Categories;

public sealed class CategoryAttributeEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/categories/{categoryId:guid}/attributes")
            .WithTags("Category Attributes");

        group.MapGet("/", GetForCategory)
            .RequirePermission("categories.update");

        group.MapPut("/", ReplaceForCategory)
            .RequirePermission("categories.update")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> GetForCategory(
        Guid categoryId,
        [FromServices] ICategoryAttributeManagementService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.GetForCategoryAsync(
                categoryId,
                ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ReplaceForCategory(
        Guid categoryId,
        [FromBody] ReplaceCategoryAttributesDto request,
        [FromServices] ICategoryAttributeManagementService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.ReplaceForCategoryAsync(
                categoryId,
                request,
                ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
