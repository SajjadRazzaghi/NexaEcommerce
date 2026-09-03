using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Inventory;

public sealed class InventoryReconciliationEndpoints
    : IFeatureEndpoints
{
    public void Map(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/inventory")
                .WithTags("Inventory Reconciliation")
                .RequireAuthorization();

        group.MapPost(
                "/reconciliation",
                Reconcile)
            .RequirePermission(
                InventoryPermissions.Manage);
    }

    private static async Task<IResult> Reconcile(
        [FromQuery]
        int? batchSize,

        InventoryOrderReconciliationService service,

        ICurrentTenant currentTenant,

        CancellationToken ct)
    {
        var requestedBatchSize =
            batchSize ?? 100;

        if (requestedBatchSize <= 0)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["batchSize"] =
                    [
                        "Batch size must be greater than zero."
                    ]
                });
        }

        if (requestedBatchSize > 500)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["batchSize"] =
                    [
                        "Batch size cannot exceed 500."
                    ]
                });
        }

        try
        {
            var result =
                await service.ReconcileAsync(
                    currentTenant.Id,
                    requestedBatchSize,
                    ct);

            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [ex.ParamName ?? "request"] =
                    [
                        ex.Message
                    ]
                });
        }
    }
}

