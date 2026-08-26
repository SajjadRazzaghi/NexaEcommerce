using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Categories;

public sealed class CategoryEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/categories")
                .WithTags("Categories")
                .AddEndpointFilter<ValidationFilter>()
                .AddEndpointFilter<PerformanceFilter>();

        // =====================================================
        // Public
        // =====================================================

        group.MapGet("/", GetAll)
            .AllowAnonymous();

        group.MapGet("/roots", GetRoots)
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetById)
            .AllowAnonymous();

        group.MapGet("/slug/{slug}", GetBySlug)
            .AllowAnonymous();

        group.MapGet(
                "/{parentCategoryId:guid}/children",
                GetChildren)
            .AllowAnonymous();

        // =====================================================
        // Admin
        // =====================================================

        group.MapPost("/", Create)
            .RequirePermission("categories.create")
            .AddEndpointFilter<TransactionFilter>();

        group.MapPut("/{id:guid}", Update)
            .RequirePermission("categories.update")
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete("/{id:guid}", Delete)
            .RequirePermission("categories.delete")
            .AddEndpointFilter<TransactionFilter>();
    }

    // =========================================================
    // GET /api/categories
    // =========================================================

    private static async Task<IResult> GetAll(
        ICategoryService service,
        CancellationToken ct)
    {
        var categories =
            await service.GetAllAsync(ct);

        return Results.Ok(categories);
    }

    // =========================================================
    // GET /api/categories/roots
    // =========================================================

    private static async Task<IResult> GetRoots(
        ICategoryService service,
        CancellationToken ct)
    {
        var categories =
            await service.GetRootCategoriesAsync(ct);

        return Results.Ok(categories);
    }

    // =========================================================
    // GET /api/categories/{id}
    // =========================================================

    private static async Task<IResult> GetById(
        Guid id,
        ICategoryService service,
        CancellationToken ct)
    {
        var category =
            await service.GetByIdAsync(
                id,
                ct);

        if (category is null)
        {
            return Results.NotFound(
                new
                {
                    error = "دسته‌بندی یافت نشد."
                });
        }

        return Results.Ok(category);
    }

    // =========================================================
    // GET /api/categories/slug/{slug}
    // =========================================================

    private static async Task<IResult> GetBySlug(
        string slug,
        ICategoryService service,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Results.BadRequest(
                new
                {
                    error = "Slug معتبر نیست."
                });
        }

        var category =
            await service.GetBySlugAsync(
                slug,
                ct);

        if (category is null)
        {
            return Results.NotFound(
                new
                {
                    error = "دسته‌بندی یافت نشد."
                });
        }

        return Results.Ok(category);
    }

    // =========================================================
    // GET /api/categories/{parentCategoryId}/children
    // =========================================================

    private static async Task<IResult> GetChildren(
        Guid parentCategoryId,
        ICategoryService service,
        CancellationToken ct)
    {
        var categories =
            await service.GetSubCategoriesAsync(
                parentCategoryId,
                ct);

        return Results.Ok(categories);
    }

    // =========================================================
    // POST /api/categories
    // =========================================================

    private static async Task<IResult> Create(
        [FromBody] CreateCategoryDto request,
        ICategoryService service,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.CreateAsync(
                    request,
                    ct);

            return Results.Created(
                $"/api/categories/{result.Id}",
                result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    // =========================================================
    // PUT /api/categories/{id}
    // =========================================================

    private static async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateCategoryDto request,
        ICategoryService service,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.UpdateAsync(
                    id,
                    request,
                    ct);

            if (!result)
            {
                return Results.NotFound(
                    new
                    {
                        error = "دسته‌بندی یافت نشد."
                    });
            }

            return Results.NoContent();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    // =========================================================
    // DELETE /api/categories/{id}
    // =========================================================

    private static async Task<IResult> Delete(
        Guid id,
        ICategoryService service,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.DeleteAsync(
                    id,
                    ct);

            if (!result)
            {
                return Results.NotFound(
                    new
                    {
                        error = "دسته‌بندی یافت نشد."
                    });
            }

            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }
}