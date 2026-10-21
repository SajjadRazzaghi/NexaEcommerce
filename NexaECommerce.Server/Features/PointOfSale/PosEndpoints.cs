using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace NexaEcommerce.Modules.Orders.Application.Pos;

/// <summary>
/// Minimal API endpoints for the in-store POS.
/// Call app.MapPosEndpoints() after authentication/authorization is configured.
/// </summary>
public static class PosEndpoints
{
    public static IEndpointRouteBuilder MapPosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/pos")
            .WithTags("Point of Sale")
            .RequireAuthorization();

        group.MapGet("/products", SearchProductsAsync)
            .WithName("PosSearchProducts")
            .Produces<IReadOnlyList<PosProductDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/barcodes/{barcode}", FindByBarcodeAsync)
            .WithName("PosFindByBarcode")
            .Produces<PosVariantDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/sales", CreateSaleAsync)
            .WithName("PosCreateSale")
            .Produces<PosSaleResultDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> SearchProductsAsync(
        [FromQuery] string? search,
        IPosOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await orchestrator.SearchProductsAsync(search, cancellationToken));
        }
        catch (PosValidationException ex)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid search", ex.Message);
        }
    }

    private static async Task<IResult> FindByBarcodeAsync(
        string barcode,
        IPosOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await orchestrator.FindByBarcodeAsync(barcode, cancellationToken));
        }
        catch (PosValidationException ex)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid barcode", ex.Message);
        }
        catch (PosNotFoundException ex)
        {
            return Problem(StatusCodes.Status404NotFound, "Barcode not found", ex.Message);
        }
        catch (PosInsufficientStockException ex)
        {
            return Problem(StatusCodes.Status409Conflict, "Insufficient stock", ex.Message);
        }
    }

    private static async Task<IResult> CreateSaleAsync(
        [FromBody] PosCreateSaleRequestDto request,
        IPosOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await orchestrator.CreateSaleAsync(request, cancellationToken);
            // Idempotent replay returns the same sale; a new commit also returns the stored sale.
            // Frontend treats either 200 or 201 as success.
            return Results.Ok(result);
        }
        catch (PosValidationException ex)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid POS sale", ex.Message);
        }
        catch (PosInsufficientStockException ex)
        {
            return Problem(StatusCodes.Status409Conflict, "Insufficient stock", ex.Message);
        }
        catch (PosConflictException ex)
        {
            return Problem(StatusCodes.Status409Conflict, "POS conflict", ex.Message);
        }
    }

    private static IResult Problem(int status, string title, string detail) =>
        Results.Problem(statusCode: status, title: title, detail: detail);
}
