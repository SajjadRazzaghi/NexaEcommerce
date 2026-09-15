using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexaECommerce.Server.Data;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Errors;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using NexaECommerce.Server.Platform.Settings;

namespace NexaECommerce.Server.Features.Appearance;

public sealed record AppearanceDto(
    string? StoreName,
    string? LogoUrl,
    string? Theme,
    string? BrandColor,
    string? CustomTheme);

public sealed record UpdateAppearanceRequest(
    string? StoreName,
    string? LogoUrl,
    string? Theme,
    string? BrandColor,
    string? CustomTheme);

/// <summary>
/// Public tenant branding and appearance endpoint.
/// The storefront can read it before authentication.
/// Only users with appearance.manage can change it.
/// </summary>
public sealed class AppearanceEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup("/api/appearance")
                .WithTags("Appearance");

        group.MapGet(
                "/",
                Get)
            .AllowAnonymous();

        group.MapPut(
                "/",
                Update)
            .RequireAuthorization()
            .RequirePermission(
                AppearancePermissions.Manage);
    }

    private static async Task<IResult> Get(
        ISettingService settings,
        ITenantContext tenant,
        AppDbContext db,
        CancellationToken ct)
    {
        var theme =
            await settings.GetAsync<string>(
                AppearanceSettings.Theme,
                ct);

        var color =
            await settings.GetAsync<string>(
                AppearanceSettings.BrandColor,
                ct);

        var custom =
            await settings.GetAsync<string>(
                AppearanceSettings.CustomTheme,
                ct);

        var tenantInfo =
            await db.Set<Tenant>()
                .AsNoTracking()
                .Where(t =>
                    t.Id == tenant.TenantId &&
                    t.Status == TenantStatus.Active)
                .Select(
                    t => new
                    {
                        t.Name,
                        t.LogoUrl,
                        t.PrimaryColor
                    })
                .FirstOrDefaultAsync(ct);

        if (tenantInfo is null)
        {
            return Results.Ok(
                new AppearanceDto(
                    null,
                    null,
                    Normalize(theme),
                    Normalize(color),
                    Normalize(custom)));
        }

        if (string.IsNullOrWhiteSpace(color))
        {
            color =
                tenantInfo.PrimaryColor;
        }

        return Results.Ok(
            new AppearanceDto(
                Normalize(tenantInfo.Name),
                Normalize(tenantInfo.LogoUrl),
                Normalize(theme),
                Normalize(color),
                Normalize(custom)));
    }

    private static async Task<IResult> Update(
        UpdateAppearanceRequest req,
        ISettingService settings,
        ITenantContext tenant,
        AppDbContext db,
        CancellationToken ct)
    {
        var storeName =
            (req.StoreName ?? string.Empty)
                .Trim();

        var logoUrl =
            (req.LogoUrl ?? string.Empty)
                .Trim();

        var theme =
            (req.Theme ?? string.Empty)
                .Trim();

        var color =
            (req.BrandColor ?? string.Empty)
                .Trim();

        var custom =
            ValidateCustomTheme(
                req.CustomTheme);

        // --------------------------------------------------------
        // Store name
        // --------------------------------------------------------

        if (storeName.Length > 128)
        {
            throw new BadRequestException(
                "Store name cannot exceed 128 characters.");
        }

        if (storeName.Length > 0 &&
            !IsSafeText(storeName))
        {
            throw new BadRequestException(
                "Store name contains invalid characters.");
        }

        // --------------------------------------------------------
        // Logo
        // --------------------------------------------------------

        if (logoUrl.Length > 2048)
        {
            throw new BadRequestException(
                "Logo URL cannot exceed 2048 characters.");
        }

        if (logoUrl.Length > 0 &&
            !IsSafeUrl(logoUrl))
        {
            throw new BadRequestException(
                "Logo URL must be an HTTP, HTTPS or root-relative URL.");
        }

        // --------------------------------------------------------
        // Appearance
        // --------------------------------------------------------

        if (theme.Length > 32 ||
            !IsSafeValue(theme))
        {
            throw new BadRequestException(
                "Invalid appearance theme.");
        }

        if (color.Length > 64 ||
            !IsSafeValue(color))
        {
            throw new BadRequestException(
                "Invalid appearance brand color.");
        }

        // --------------------------------------------------------
        // Tenant branding
        // --------------------------------------------------------

        var tenantEntity =
            await db.Set<Tenant>()
                .Where(t =>
                    t.Id == tenant.TenantId)
                .FirstOrDefaultAsync(ct);

        if (tenantEntity is null)
        {
            throw new KeyNotFoundException(
                "Active tenant was not found.");
        }

        if (tenantEntity.Status !=
            TenantStatus.Active)
        {
            throw new InvalidOperationException(
                "The active tenant is not available.");
        }

        /*
         * Empty values are allowed so the tenant can intentionally
         * fall back to the product defaults.
         */
        tenantEntity.Name =
            string.IsNullOrWhiteSpace(storeName)
                ? tenantEntity.Name
                : storeName;

        tenantEntity.LogoUrl =
            string.IsNullOrWhiteSpace(logoUrl)
                ? null
                : logoUrl;

        await settings.SetAsync(
            AppearanceSettings.Theme,
            theme,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await settings.SetAsync(
            AppearanceSettings.BrandColor,
            color,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await settings.SetAsync(
            AppearanceSettings.CustomTheme,
            custom,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        /*
         * Keep the tenant's persisted brand colour synchronized too.
         * GET still gives explicit Appearance.BrandColor precedence.
         */
        tenantEntity.PrimaryColor =
            string.IsNullOrWhiteSpace(color)
                ? null
                : color;

        await db.SaveChangesAsync(ct);

        return Results.Ok(
            new AppearanceDto(
                Normalize(tenantEntity.Name),
                Normalize(tenantEntity.LogoUrl),
                Normalize(theme),
                Normalize(color),
                Normalize(custom)));
    }

    private static bool IsSafeText(
        string value)
    {
        return value.IndexOfAny(
            [
                '<',
                '>',
                '{',
                '}',
                ';',
                '\"',
                '\'',
                '\r',
                '\n'
            ]) < 0;
    }

    private static bool IsSafeValue(
        string value)
    {
        return value.IndexOfAny(
            [
                ';',
                '{',
                '}',
                '<',
                '>',
                '\"',
                '\'',
                '\r',
                '\n'
            ]) < 0;
    }

    private static bool IsSafeUrl(
        string value)
    {
        if (value.StartsWith(
                "/",
                StringComparison.Ordinal))
        {
            return !value.StartsWith(
                "//",
                StringComparison.Ordinal);
        }

        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri))
        {
            return false;
        }

        return uri.Scheme ==
                   Uri.UriSchemeHttp ||
               uri.Scheme ==
                   Uri.UriSchemeHttps;
    }

    private static string ValidateCustomTheme(
        string? value)
    {
        var v =
            (value ?? string.Empty)
                .Trim();

        if (v.Length == 0)
        {
            return string.Empty;
        }

        if (v.Length > 4000)
        {
            throw new BadRequestException(
                "Custom theme is too large.");
        }

        try
        {
            using var _ =
                JsonDocument.Parse(v);
        }
        catch
        {
            throw new BadRequestException(
                "Custom theme must be valid JSON.");
        }

        return v;
    }

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }
}