using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Orders;

public sealed class PackageEndpoints : IFeatureEndpoints
{
    public void Map(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/orders")
                .WithTags("Packages")
                .RequireAuthorization();

        group.MapGet(
            "/{orderId:guid}/packages",
            GetPackages)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
            "/{orderId:guid}/packages",
            CreatePackage)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
            "/packages/{packageId:guid}/start-packing",
            StartPacking)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPut(
            "/packages/{packageId:guid}/dimensions",
            UpdateDimensions)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
            "/packages/{packageId:guid}/packed",
            MarkPacked)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
            "/packages/{packageId:guid}/label-printed",
            MarkLabelPrinted)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPut(
            "/packages/{packageId:guid}/tracking",
            SetTracking)
            .RequirePermission(
                OrderPermissions.Manage);

        group.MapPost(
            "/packages/{packageId:guid}/shipped",
            MarkShipped)
            .RequirePermission(
                OrderPermissions.UpdateStatus);

        group.MapPost(
            "/packages/{packageId:guid}/delivered",
            MarkDelivered)
            .RequirePermission(
                OrderPermissions.UpdateStatus);

        group.MapGet(
            "/packages/{packageId:guid}/label",
            GetLabel)
            .RequirePermission(
                OrderPermissions.Manage);
    }

    private static async Task<IResult> GetPackages(
        Guid orderId,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.GetByOrderAsync(
                    tenant.Id,
                    orderId,
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
    }

    private static async Task<IResult> CreatePackage(
        Guid orderId,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.CreateAsync(
                    tenant.Id,
                    orderId,
                    ct);

            return Results.Created(
                $"/api/orders/{orderId}/packages/{result.Id}",
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(
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
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> StartPacking(
        Guid packageId,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.StartPackingAsync(
                    tenant.Id,
                    packageId,
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

    private static async Task<IResult> UpdateDimensions(
        Guid packageId,
        [FromBody] UpdatePackageDimensionsRequest request,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.UpdateDimensionsAsync(
                    tenant.Id,
                    packageId,
                    request,
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

    private static async Task<IResult> MarkPacked(
        Guid packageId,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.MarkPackedAsync(
                    tenant.Id,
                    packageId,
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

    private static async Task<IResult> MarkLabelPrinted(
        Guid packageId,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.MarkLabelPrintedAsync(
                    tenant.Id,
                    packageId,
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

    private static async Task<IResult> SetTracking(
        Guid packageId,
        [FromBody] SetPackageTrackingRequest request,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.SetTrackingAsync(
                    tenant.Id,
                    packageId,
                    request,
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

    private static async Task<IResult> MarkShipped(
        Guid packageId,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.MarkShippedAsync(
                    tenant.Id,
                    packageId,
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

    private static async Task<IResult> MarkDelivered(
        Guid packageId,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.MarkDeliveredAsync(
                    tenant.Id,
                    packageId,
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

    private static async Task<IResult> GetLabel(
        Guid packageId,
        [FromServices] IPackageService packages,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await packages.GetLabelAsync(
                    tenant.Id,
                    packageId,
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
