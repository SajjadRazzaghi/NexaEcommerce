using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Orders.Application.Pos;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.PointOfSale;

public sealed class PosBridgeEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pos/bridge").WithTags("POS Bridge");

        // A signed-in POS seller may pair a Bridge. The plaintext token is returned once only.
        group.MapPost("/devices", CreateDeviceAsync)
            .RequirePermission(PosPermissions.Cancel);
        group.MapDelete("/devices/{deviceId:guid}", DisableDeviceAsync)
            .RequirePermission(PosPermissions.Cancel);

        // These routes use the per-device secret, not the browser cookie session.
        group.MapGet("/health", HealthAsync);
        group.MapGet("/availability", SellerAvailabilityAsync)
    .RequirePermission(PosPermissions.Sell);
        group.MapGet("/next", NextAsync);
        group.MapGet("/sales/open", SellerOpenSaleStatusAsync)
            .RequirePermission(PosPermissions.Sell);
        group.MapPost(
        "/sales/{saleId:guid}/cancel-unclaimed",
        CancelUnclaimedAsync)
    .RequirePermission(PosPermissions.Sell);
        group.MapGet("/sales/{saleId:guid}/status", SellerSaleStatusAsync)
            .RequirePermission(PosPermissions.Sell);
        group.MapPost("/sales/{saleId:guid}/reconcile", ReconcileAsync)
            .RequirePermission(PosPermissions.Cancel);
        group.MapPost("/{saleId:guid}/complete", CompleteAsync);
    }
    private static async Task<IResult> SellerAvailabilityAsync(
    ICurrentTenant tenant,
    IPosBridgeService service,
    CancellationToken ct)
    {
        var result = await service.GetSellerAvailabilityAsync(
            tenant.Id,
            ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> CancelUnclaimedAsync(
        Guid saleId,
        ICurrentTenant tenant,
        IPosExecutionContext user,
        IPosBridgeService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.CancelUnclaimedAsync(
                tenant.Id,
                user.UserId,
                saleId,
                ct);

            return Results.Ok(result);
        }
        catch (PosValidationException ex)
        {
            return Results.Problem(
                statusCode: 400,
                title: "درخواست لغو نامعتبر است.",
                detail: ex.Message);
        }
        catch (PosNotFoundException ex)
        {
            return Results.Problem(
                statusCode: 404,
                title: "فاکتور پیدا نشد.",
                detail: ex.Message);
        }
        catch (PosConflictException ex)
        {
            return Results.Problem(
                statusCode: 409,
                title: "لغو امن امکان‌پذیر نیست.",
                detail: ex.Message);
        }
    }
    private static async Task<IResult> CreateDeviceAsync(
        [FromBody] PosBridgeCreateDeviceRequest request,
        ICurrentTenant tenant,
        IPosBridgeService service,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            return Results.Problem(statusCode: 400, title: "نام دستگاه الزامی است و نباید بیشتر از ۱۰۰ نویسه باشد.");

        try
        {
            return Results.Ok(await service.CreateDeviceAsync(tenant.Id, request.Name, ct));
        }
        catch (PosConflictException ex)
        {
            return Results.Problem(statusCode: 409, title: "Bridge فعال وجود دارد.", detail: ex.Message);
        }
        catch (PosValidationException ex)
        {
            return Results.Problem(statusCode: 400, title: "اطلاعات دستگاه معتبر نیست.", detail: ex.Message);
        }
    }

    private static async Task<IResult> DisableDeviceAsync(
        Guid deviceId,
        ICurrentTenant tenant,
        IPosBridgeService service,
        CancellationToken ct)
    {
        try
        {
            await service.DisableDeviceAsync(tenant.Id, deviceId, ct);
            return Results.NoContent();
        }
        catch (PosNotFoundException ex)
        {
            return Results.Problem(statusCode: 404, title: "دستگاه Bridge پیدا نشد.", detail: ex.Message);
        }
        catch (PosConflictException ex)
        {
            return Results.Problem(statusCode: 409, title: "Bridge در حال پردازش است.", detail: ex.Message);
        }
    }

    private static async Task<IResult> HealthAsync(
        HttpContext http,
        IPosBridgeService service,
        CancellationToken ct)
    {
        var token = ReadToken(http);
        if (token is null) return Results.Unauthorized();
        try { return Results.Ok(await service.GetHealthAsync(token, ct)); }
        catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
    }

    private static async Task<IResult> NextAsync(
        HttpContext http,
        IPosBridgeService service,
        CancellationToken ct)
    {
        var token = ReadToken(http);
        if (token is null) return Results.Unauthorized();
        try
        {
            var work = await service.ClaimNextAsync(token, ct);
            return work is null ? Results.NoContent() : Results.Ok(work);
        }
        catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
    }

    private static async Task<IResult> SellerOpenSaleStatusAsync(
        ICurrentTenant tenant,
        IPosExecutionContext user,
        IPosBridgeService service,
        CancellationToken ct)
    {
        var result = await service.GetSellerUnresolvedSaleStatusAsync(tenant.Id, user.UserId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> SellerSaleStatusAsync(
        Guid saleId,
        ICurrentTenant tenant,
        IPosExecutionContext user,
        IPosBridgeService service,
        CancellationToken ct)
    {
        var result = await service.GetSellerSaleStatusAsync(tenant.Id, user.UserId, saleId, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ReconcileAsync(
        Guid saleId,
        ICurrentTenant tenant,
        [FromBody] PosBridgeReconciliationRequest request,
        IPosBridgeService service,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.ReconcileAsync(tenant.Id, saleId, request, ct));
        }
        catch (PosValidationException ex)
        {
            return Results.Problem(statusCode: 400, title: "درخواست تطبیق نامعتبر است.", detail: ex.Message);
        }
        catch (PosNotFoundException ex)
        {
            return Results.Problem(statusCode: 404, title: "فاکتور پیدا نشد.", detail: ex.Message);
        }
        catch (PosConflictException ex)
        {
            return Results.Problem(statusCode: 409, title: "وضعیت فاکتور قابل تطبیق نیست.", detail: ex.Message);
        }
    }

    private static async Task<IResult> CompleteAsync(
        Guid saleId,
        HttpContext http,
        [FromBody] PosBridgePaymentCompletionRequest request,
        IPosBridgeService service,
        CancellationToken ct)
    {
        var token = ReadToken(http);
        if (token is null) return Results.Unauthorized();
        try
        {
            return Results.Ok(await service.CompleteAsync(token, saleId, request, ct));
        }
        catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
        catch (PosValidationException ex)
        {
            return Results.Problem(statusCode: 400, title: "نتیجه پرداخت نامعتبر است.", detail: ex.Message);
        }
        catch (PosNotFoundException ex)
        {
            return Results.Problem(statusCode: 404, title: "فاکتور پیدا نشد.", detail: ex.Message);
        }
        catch (PosConflictException ex)
        {
            return Results.Problem(statusCode: 409, title: "وضعیت پرداخت تغییر کرده است.", detail: ex.Message);
        }
    }

    private static string? ReadToken(HttpContext http)
    {
        var value = http.Request.Headers["X-Nexa-Pos-Bridge"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
