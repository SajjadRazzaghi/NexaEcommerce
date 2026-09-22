using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class WarehouseTransferEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/inventory/transfers")
                .WithTags("Inventory Transfers")
                .AddEndpointFilter<PerformanceFilter>();

        group.MapGet(
                "/",
                GetTransfers)
            .RequirePermission(
                InventoryPermissions.Read);

        group.MapPost(
                "/",
                Transfer)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapGet(
                "/movements/{warehouseId:guid}/{locationId:guid}/{productVariantId:guid}",
                GetMovements)
            .RequirePermission(
                InventoryPermissions.Read);
    }

    private static async Task<IResult> GetTransfers(
        int skip,
        int take,
        IWarehouseTransferService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetTransfersAsync(
                    currentTenant.Id,
                    skip,
                    take,
                    ct);

            return Results.Ok(
                new
                {
                    skip,
                    take = Math.Min(take, 200),
                    items = result
                });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Transfer(
        [FromBody] CreateWarehouseTransferRequest request,
        IWarehouseTransferService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.TransferAsync(
                    currentTenant.Id,
                    request,
                    ct);

            return Results.Created(
                $"/api/inventory/transfers/{result.Id}",
                result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
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
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(
                new
                {
                    error =
                        "Warehouse stock was changed by another operation. Please refresh and try again."
                });
        }
    }

    private static async Task<IResult> GetMovements(
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        int skip,
        int take,
        IWarehouseTransferService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetStockMovementsAsync(
                    currentTenant.Id,
                    warehouseId,
                    locationId,
                    productVariantId,
                    skip,
                    take,
                    ct);

            return Results.Ok(
                new
                {
                    warehouseId,
                    locationId,
                    productVariantId,
                    skip,
                    take = Math.Min(take, 200),
                    items = result
                });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }
}