using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class WarehouseEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/inventory/warehouses")
                .WithTags("Inventory Warehouses")
                .AddEndpointFilter<PerformanceFilter>();

        group.MapGet(
                "/",
                GetWarehouses)
            .RequirePermission(
                InventoryPermissions.Read);

        group.MapPost(
                "/",
                CreateWarehouse)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPut(
                "/{warehouseId:guid}",
                UpdateWarehouse)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPut(
                "/{warehouseId:guid}/status",
                SetWarehouseStatus)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPost(
                "/{warehouseId:guid}/set-default",
                SetDefaultWarehouse)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapGet(
                "/{warehouseId:guid}/locations",
                GetLocations)
            .RequirePermission(
                InventoryPermissions.Read);

        group.MapPost(
                "/locations",
                CreateLocation)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPut(
                "/{warehouseId:guid}/locations/{locationId:guid}",
                UpdateLocation)
            .RequirePermission(
                InventoryPermissions.Manage);

        group.MapPut(
                "/{warehouseId:guid}/locations/{locationId:guid}/status",
                SetLocationStatus)
            .RequirePermission(
                InventoryPermissions.Manage);
    }

    private static async Task<IResult> GetWarehouses(
        bool includeInactive,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        var result =
            await service.GetWarehousesAsync(
                currentTenant.Id,
                includeInactive,
                ct);

        return Results.Ok(
            new
            {
                items = result
            });
    }

    private static async Task<IResult> CreateWarehouse(
        [FromBody] CreateWarehouseRequest request,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.CreateWarehouseAsync(
                    currentTenant.Id,
                    request,
                    ct);

            return Results.Created(
                $"/api/inventory/warehouses/{result.Id}",
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
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> UpdateWarehouse(
        Guid warehouseId,
        [FromBody] UpdateWarehouseRequest request,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.UpdateWarehouseAsync(
                    currentTenant.Id,
                    warehouseId,
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

    private static async Task<IResult> SetWarehouseStatus(
        Guid warehouseId,
        [FromBody] SetWarehouseStatusRequest request,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.SetWarehouseStatusAsync(
                    currentTenant.Id,
                    warehouseId,
                    request.IsActive,
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

    private static async Task<IResult> SetDefaultWarehouse(
        Guid warehouseId,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.SetDefaultWarehouseAsync(
                    currentTenant.Id,
                    warehouseId,
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

    private static async Task<IResult> GetLocations(
        Guid warehouseId,
        bool includeInactive,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetLocationsAsync(
                    currentTenant.Id,
                    warehouseId,
                    includeInactive,
                    ct);

            return Results.Ok(
                new
                {
                    warehouseId,
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
    }

    private static async Task<IResult> CreateLocation(
        [FromBody] CreateWarehouseLocationRequest request,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.CreateLocationAsync(
                    currentTenant.Id,
                    request,
                    ct);

            return Results.Created(
                $"/api/inventory/warehouses/{result.WarehouseId}/locations/{result.Id}",
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

    private static async Task<IResult> UpdateLocation(
        Guid warehouseId,
        Guid locationId,
        [FromBody] UpdateWarehouseLocationRequest request,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.UpdateLocationAsync(
                    currentTenant.Id,
                    warehouseId,
                    locationId,
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

    private static async Task<IResult> SetLocationStatus(
        Guid warehouseId,
        Guid locationId,
        [FromBody] SetWarehouseLocationStatusRequest request,
        IWarehouseService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.SetLocationStatusAsync(
                    currentTenant.Id,
                    warehouseId,
                    locationId,
                    request.IsActive,
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
    }
}
