using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class WarehouseStockEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/inventory/warehouse-stock")
                .WithTags("Inventory Warehouse Stock")
                .AddEndpointFilter<PerformanceFilter>();

        group.MapGet(
                "/",
                GetByWarehouse)
            .RequirePermission(
                InventoryPermissions.Read);

        group.MapGet(
                "/{warehouseId:guid}/{locationId:guid}/{productVariantId:guid}",
                Get)
            .RequirePermission(
                InventoryPermissions.Read);

        group.MapPost(
                "/",
                Create)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPut(
                "/",
                Set)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPost(
                "/adjust",
                Adjust)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPost(
                "/receive",
                Receive)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPost(
                "/damage",
                Damage)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPut(
                "/reorder-point",
                SetReorderPoint)
            .RequirePermission(
                InventoryPermissions.Manage);
    }

    private static async Task<IResult> GetByWarehouse(
        Guid warehouseId,
        Guid? locationId,
        Guid? productVariantId,
        bool includeZeroStock,
        [FromServices] IWarehouseStockService service,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetByWarehouseAsync(
                    currentTenant.Id,
                    warehouseId,
                    locationId,
                    productVariantId,
                    includeZeroStock,
                    ct);

            return Results.Ok(
                new
                {
                    warehouseId,
                    locationId,
                    productVariantId,
                    includeZeroStock,
                    items = result
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
    }

    private static async Task<IResult> Get(
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        [FromServices] IWarehouseStockService service,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetAsync(
                    currentTenant.Id,
                    warehouseId,
                    locationId,
                    productVariantId,
                    ct);

            return result is null
                ? Results.NotFound(
                    new
                    {
                        error = "Warehouse stock was not found."
                    })
                : Results.Ok(result);
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

    private static async Task<IResult> Create(
        [FromBody] CreateWarehouseStockRequest request,
        [FromServices] IWarehouseStockService service,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.CreateAsync(
                    currentTenant.Id,
                    request,
                    ct);

            return Results.Created(
                $"/api/inventory/warehouse-stock/{result.WarehouseId}/{result.LocationId}/{result.ProductVariantId}",
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

    private static async Task<IResult> Set(
        [FromBody] SetWarehouseStockRequest request,
        [FromServices] IWarehouseStockService service,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.SetAsync(
                    currentTenant.Id,
                    request,
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

    private static async Task<IResult> Adjust(
        [FromBody] AdjustWarehouseStockRequest request,
        [FromServices] IWarehouseStockService service,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.AdjustAsync(
                    currentTenant.Id,
                    request,
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

    private static async Task<IResult> Receive(
        [FromBody] ReceiveWarehouseStockRequest request,
        [FromServices] IWarehouseStockService service,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.ReceiveAsync(
                    currentTenant.Id,
                    request,
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

    private static async Task<IResult> Damage(
        [FromBody] DamageWarehouseStockRequest request,
        [FromServices] IWarehouseStockService service,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.DamageAsync(
                    currentTenant.Id,
                    request,
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

    private static async Task<IResult> SetReorderPoint(
        [FromBody] SetWarehouseReorderPointRequest request,
        [FromServices] IWarehouseStockService service,
        [FromServices] ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.SetReorderPointAsync(
                    currentTenant.Id,
                    request,
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