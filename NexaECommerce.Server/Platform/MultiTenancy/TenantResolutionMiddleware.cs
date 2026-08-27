using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexaECommerce.Server.Data;

namespace NexaECommerce.Server.Platform.MultiTenancy;

/// <summary>
/// Resolves the tenant for the current HTTP request and stores it in HttpContext.Items
/// so ITenantContext and AppDbContext can use the same request tenant.
///
/// Supported strategies:
/// - UserClaim: active tenant from the authenticated principal.
/// - Header: X-Tenant-Id.
/// - Subdomain: tenant.example.com.
/// - Path: /t/{tenantId}/... .
///
/// Single-tenant mode always resolves to the default tenant.
/// </summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IOptions<TenancyOptions> options)
{
    private readonly TenancyOptions _options = options.Value;

    public async Task InvokeAsync(
        HttpContext context,
        AppDbContext db,
        ITenantRoleService tenantRoles)
    {
        var tenantId = ResolveTenantId(context);

        if (_options.Mode == TenancyMode.SingleTenant)
        {
            tenantId = TenancyOptions.DefaultTenant;
        }
        else
        {
            tenantId = await ValidateTenantAsync(
                context,
                db,
                tenantRoles,
                tenantId);
        }

        context.Items[TenantContext.ItemKey] = tenantId;

        await next(context);
    }

    private string ResolveTenantId(HttpContext context)
    {
        var resolution =
            _options.Resolution.Trim().ToLowerInvariant();

        return resolution switch
        {
            "header" =>
                context.Request.Headers.TryGetValue(
                    "X-Tenant-Id",
                    out var headerValue)
                    ? headerValue.ToString().Trim()
                    : string.Empty,

            "path" =>
                ResolveFromPath(context),

            "subdomain" =>
                ResolveFromSubdomain(context),

            "userclaim" =>
                context.User.FindFirst(TenantClaims.ClaimType)?.Value
                ?? string.Empty,

            _ =>
                context.User.FindFirst(TenantClaims.ClaimType)?.Value
                ?? string.Empty
        };
    }

    private static string ResolveFromPath(
        HttpContext context)
    {
        var segments = context.Request.Path
            .Value?
            .Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries)
            ?? [];

        // /t/{tenantId}/...
        if (segments.Length >= 2 &&
            string.Equals(
                segments[0],
                "t",
                StringComparison.OrdinalIgnoreCase))
        {
            return segments[1];
        }

        return string.Empty;
    }

    private static string ResolveFromSubdomain(
        HttpContext context)
    {
        var host = context.Request.Host.Host;

        if (string.IsNullOrWhiteSpace(host))
            return string.Empty;

        // Local development:
        // localhost / 127.0.0.1 → default tenant.
        if (host.Equals(
                "localhost",
                StringComparison.OrdinalIgnoreCase) ||
            host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var parts = host.Split(
            '.',
            StringSplitOptions.RemoveEmptyEntries);

        // e.g. acme.example.com
        if (parts.Length < 3)
            return string.Empty;

        return parts[0];
    }

    private static async Task<string> ValidateTenantAsync(
     HttpContext context,
     AppDbContext db,
     ITenantRoleService tenantRoles,
     string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            var anonymousOrAuthenticatedUserId =
                context.User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;

            if (string.IsNullOrWhiteSpace(
                    anonymousOrAuthenticatedUserId))
            {
                return TenancyOptions.DefaultTenant;
            }

            var activeUserTenant =
                await db.Users
                    .Where(x =>
                        x.Id == anonymousOrAuthenticatedUserId)
                    .Select(x => x.TenantId)
                    .SingleOrDefaultAsync();

            return string.IsNullOrWhiteSpace(activeUserTenant)
                ? TenancyOptions.DefaultTenant
                : activeUserTenant;
        }

        tenantId =
            tenantId.Trim().ToLowerInvariant();

        var tenant =
            await db.Set<Tenant>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == tenantId);

        if (tenant is null)
        {
            throw new Platform.Errors.NotFoundException(
                "Tenant",
                tenantId);
        }

        if (tenant.Status != TenantStatus.Active)
        {
            throw new Platform.Errors.ForbiddenException(
                "The selected tenant is suspended.");
        }

        var authenticatedUserId =
            context.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value;

        // Anonymous public requests may resolve a tenant.
        // Authenticated users may only operate tenants they belong to.
        if (!string.IsNullOrWhiteSpace(authenticatedUserId))
        {
            var allowedTenants =
                await tenantRoles.TenantIdsForUserAsync(
                    authenticatedUserId);

            if (!allowedTenants.Contains(
                    tenantId,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new Platform.Errors.ForbiddenException(
                    "You are not a member of the selected tenant.");
            }
        }

        return tenantId;
    }
}