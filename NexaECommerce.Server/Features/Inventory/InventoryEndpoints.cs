using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class InventoryEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/inventory")
                .WithTags("Inventory")
                .AddEndpointFilter<PerformanceFilter>();

        // ============================================================
        // Stock
        // ============================================================

        group.MapGet(
                "/{productVariantId:guid}",
                GetStock)
            .RequirePermission(
                InventoryPermissions.Read);

        group.MapPut(
                "/stock",
                SetStock)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPost(
                "/adjust",
                AdjustStock)
            .RequirePermission(
                InventoryPermissions.Manage);

        // ============================================================
        // Reservations
        // ============================================================

        group.MapPost(
                "/reservations",
                Reserve)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPost(
                "/reservations/{reservationKey}/release",
                Release)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPost(
                "/reservations/{reservationKey}/commit",
                Commit)
            .RequirePermission(
                InventoryPermissions.Manage);
    }

    private static async Task<IResult> GetStock(
        Guid productVariantId,
        IInventoryService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        var result =
            await service.GetStockAsync(
                currentTenant.Id,
                productVariantId,
                ct);

        return result is null
            ? Results.NotFound(
                new
                {
                    error = "Stock record was not found."
                })
            : Results.Ok(result);
    }

    private static async Task<IResult> SetStock(
        [FromBody] SetStockRequest request,
        IInventoryService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.SetStockAsync(
                    currentTenant.Id,
                    request.ProductVariantId,
                    request.Quantity,
                    ct);

            return Results.Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> AdjustStock(
        [FromBody] AdjustStockRequest request,
        IInventoryService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.AdjustStockAsync(
                    currentTenant.Id,
                    request.ProductVariantId,
                    request.Quantity,
                    ct);

            return Results.Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Reserve(
        [FromBody] ReserveStockRequest request,
        IInventoryService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var expiration =
                TimeSpan.FromMinutes(
                    request.ExpirationMinutes);

            var result =
                await service.ReserveAsync(
                    currentTenant.Id,
                    request.ProductVariantId,
                    request.Quantity,
                    request.ReservationKey,
                    expiration,
                    ct);

            return Results.Ok(result);
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
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Release(
        string reservationKey,
        IInventoryService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.ReleaseAsync(
                    currentTenant.Id,
                    reservationKey,
                    ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Commit(
        string reservationKey,
        IInventoryService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.CommitAsync(
                    currentTenant.Id,
                    reservationKey,
                    ct);

            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }
}