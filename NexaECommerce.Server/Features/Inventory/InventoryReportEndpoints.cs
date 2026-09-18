using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class InventoryReportEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/inventory/reports")
                .WithTags("Inventory Reports")
                .AddEndpointFilter<PerformanceFilter>();

        group.MapGet(
                "/summary",
                GetSummary)
            .RequirePermission(
                InventoryPermissions.Read);

        group.MapGet(
                "/low-stock",
                GetLowStock)
            .RequirePermission(
                InventoryPermissions.Read);

        group.MapGet(
                "/discrepancies",
                GetDiscrepancies)
            .RequirePermission(
                InventoryPermissions.Read);
    }

    private static async Task<IResult> GetSummary(
        IInventoryReportService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        var result =
            await service.GetSummaryAsync(
                currentTenant.Id,
                ct);

        return Results.Ok(
            result);
    }

    private static async Task<IResult> GetLowStock(
        Guid? warehouseId,
        int skip,
        int take,
        IInventoryReportService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetLowStockAsync(
                    currentTenant.Id,
                    warehouseId,
                    skip,
                    take,
                    ct);

            return Results.Ok(
                new
                {
                    warehouseId,
                    skip,
                    take = Math.Min(
                        take,
                        200),
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
    }

    private static async Task<IResult> GetDiscrepancies(
        int skip,
        int take,
        IInventoryReportService service,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        try
        {
            var result =
                await service.GetDiscrepanciesAsync(
                    currentTenant.Id,
                    skip,
                    take,
                    ct);

            return Results.Ok(
                new
                {
                    skip,
                    take = Math.Min(
                        take,
                        200),
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
    }
}
