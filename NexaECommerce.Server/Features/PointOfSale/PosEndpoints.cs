using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.Pos;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.PointOfSale;

public sealed class PosEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pos")
            .WithTags("Point of Sale")
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/products", SearchProductsAsync)
            .RequirePermission(PosPermissions.Sell);

        group.MapGet("/barcodes/{barcode}", FindByBarcodeAsync)
            .RequirePermission(PosPermissions.Sell);

        group.MapPost("/sales", CreateSaleAsync)
            .RequirePermission(PosPermissions.Sell);

        group.MapGet("/sales", ListSalesAsync)
            .RequirePermission(PosPermissions.Read);

        group.MapGet("/sales/{saleId:guid}", GetSaleAsync)
            .RequirePermission(PosPermissions.Read);
    }

    private static async Task<IResult> SearchProductsAsync(
        [FromQuery] string? search,
        IPosOrchestrator orchestrator,
        CancellationToken ct)
    {
        try { return Results.Ok(await orchestrator.SearchProductsAsync(search, ct)); }
        catch (PosValidationException ex) { return Problem(400, "جست‌وجوی نامعتبر", ex.Message); }
    }

    private static async Task<IResult> FindByBarcodeAsync(
        string barcode,
        IPosOrchestrator orchestrator,
        CancellationToken ct)
    {
        try { return Results.Ok(await orchestrator.FindByBarcodeAsync(barcode, ct)); }
        catch (PosValidationException ex) { return Problem(400, "بارکد نامعتبر", ex.Message); }
        catch (PosNotFoundException ex) { return Problem(404, "بارکد پیدا نشد", ex.Message); }
        catch (PosInsufficientStockException ex) { return Problem(409, "موجودی کافی نیست", ex.Message); }
    }

    private static async Task<IResult> CreateSaleAsync(
        [FromBody] PosCreateSaleRequestDto request,
        IPosOrchestrator orchestrator,
        CancellationToken ct)
    {
        try
        {
            var sale = await orchestrator.CreateSaleAsync(request, ct);
            return Results.Created($"/api/pos/sales/{sale.Id:D}", sale);
        }
        catch (PosValidationException ex) { return Problem(400, "فاکتور نامعتبر", ex.Message); }
        catch (PosInsufficientStockException ex) { return Problem(409, "موجودی کافی نیست", ex.Message); }
        catch (PosConflictException ex) { return Problem(409, "تعارض فروش", ex.Message); }
    }

    private static async Task<IResult> ListSalesAsync(
        [FromQuery] int skip,
        [FromQuery] int take,
        IPosSaleCommitter committer,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        if (skip < 0 || take is < 1 or > 100)
            return Problem(400, "صفحه‌بندی نامعتبر", "skip باید صفر یا بیشتر و take بین ۱ تا ۱۰۰ باشد.");

        return Results.Ok(await committer.ListSalesAsync(tenant.Id, skip, take, ct));
    }

    private static async Task<IResult> GetSaleAsync(
        Guid saleId,
        IPosSaleCommitter committer,
        ICurrentTenant tenant,
        CancellationToken ct)
    {
        var sale = await committer.FindSaleByIdAsync(tenant.Id, saleId, ct);
        return sale is null
            ? Results.NotFound(new { title = "فاکتور پیدا نشد." })
            : Results.Ok(sale);
    }

    private static IResult Problem(int status, string title, string detail) =>
        Results.Problem(statusCode: status, title: title, detail: detail);
}