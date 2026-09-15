using System.Net.Mail;
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
    string? FaviconUrl,
    string? Theme,
    string? BrandColor,
    string? CustomTheme,
    string? ContactPhone,
    string? ContactEmail,
    string? ContactAddress,
    string? WebsiteUrl,
    string? SeoTitle,
    string? SeoDescription);

public sealed record UpdateAppearanceRequest(
    string? StoreName,
    string? LogoUrl,
    string? FaviconUrl,
    string? Theme,
    string? BrandColor,
    string? CustomTheme,
    string? ContactPhone,
    string? ContactEmail,
    string? ContactAddress,
    string? WebsiteUrl,
    string? SeoTitle,
    string? SeoDescription);

public sealed class AppearanceEndpoints : IFeatureEndpoints
{
    public void Map(
        IEndpointRouteBuilder app)
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

        var contactPhone =
            await settings.GetAsync<string>(
                AppearanceSettings.ContactPhone,
                ct);

        var contactEmail =
            await settings.GetAsync<string>(
                AppearanceSettings.ContactEmail,
                ct);

        var contactAddress =
            await settings.GetAsync<string>(
                AppearanceSettings.ContactAddress,
                ct);

        var websiteUrl =
            await settings.GetAsync<string>(
                AppearanceSettings.WebsiteUrl,
                ct);

        var seoTitle =
            await settings.GetAsync<string>(
                AppearanceSettings.SeoTitle,
                ct);

        var seoDescription =
            await settings.GetAsync<string>(
                AppearanceSettings.SeoDescription,
                ct);

        var faviconUrl =
            await settings.GetAsync<string>(
                AppearanceSettings.FaviconUrl,
                ct);

        var tenantInfo =
            await db.Set<Tenant>()
                .AsNoTracking()
                .Where(t =>
                    t.Id == tenant.TenantId &&
                    t.Status == TenantStatus.Active)
                .Select(t => new
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
                    Normalize(faviconUrl),
                    Normalize(theme),
                    Normalize(color),
                    Normalize(custom),
                    Normalize(contactPhone),
                    Normalize(contactEmail),
                    Normalize(contactAddress),
                    Normalize(websiteUrl),
                    Normalize(seoTitle),
                    Normalize(seoDescription)));
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
                Normalize(faviconUrl),
                Normalize(theme),
                Normalize(color),
                Normalize(custom),
                Normalize(contactPhone),
                Normalize(contactEmail),
                Normalize(contactAddress),
                Normalize(websiteUrl),
                Normalize(seoTitle),
                Normalize(seoDescription)));
    }

    private static async Task<IResult> Update(
        UpdateAppearanceRequest req,
        ISettingService settings,
        ITenantContext tenant,
        AppDbContext db,
        CancellationToken ct)
    {
        var storeName =
            NormalizeInput(req.StoreName);

        var logoUrl =
            NormalizeInput(req.LogoUrl);

        var faviconUrl =
            NormalizeInput(req.FaviconUrl);

        var theme =
            NormalizeInput(req.Theme);

        var color =
            NormalizeInput(req.BrandColor);

        var contactPhone =
            NormalizeInput(req.ContactPhone);

        var contactEmail =
            NormalizeInput(req.ContactEmail);

        var contactAddress =
            NormalizeInput(req.ContactAddress);

        var websiteUrl =
            NormalizeInput(req.WebsiteUrl);

        var seoTitle =
            NormalizeInput(req.SeoTitle);

        var seoDescription =
            NormalizeInput(req.SeoDescription);

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

        ValidateImageUrl(
            logoUrl,
            "Logo URL");

        // --------------------------------------------------------
        // Favicon
        // --------------------------------------------------------

        ValidateImageUrl(
            faviconUrl,
            "Favicon URL");

        // --------------------------------------------------------
        // Theme
        // --------------------------------------------------------

        if (theme.Length > 32 ||
            !IsSafeValue(theme))
        {
            throw new BadRequestException(
                "Invalid appearance theme.");
        }

        // --------------------------------------------------------
        // Brand color
        // --------------------------------------------------------

        if (color.Length > 64 ||
            !IsSafeValue(color))
        {
            throw new BadRequestException(
                "Invalid appearance brand color.");
        }

        // --------------------------------------------------------
        // Contact phone
        // --------------------------------------------------------

        if (contactPhone.Length > 64 ||
            !IsSafeText(contactPhone))
        {
            throw new BadRequestException(
                "Invalid contact phone.");
        }

        // --------------------------------------------------------
        // Contact email
        // --------------------------------------------------------

        if (contactEmail.Length > 256)
        {
            throw new BadRequestException(
                "Contact email cannot exceed 256 characters.");
        }

        if (contactEmail.Length > 0)
        {
            try
            {
                var mail =
                    new MailAddress(contactEmail);

                if (!string.Equals(
                        mail.Address,
                        contactEmail,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException();
                }
            }
            catch
            {
                throw new BadRequestException(
                    "Invalid contact email.");
            }
        }

        // --------------------------------------------------------
        // Contact address
        // --------------------------------------------------------

        if (contactAddress.Length > 1000 ||
            !IsSafeText(contactAddress))
        {
            throw new BadRequestException(
                "Invalid contact address.");
        }

        // --------------------------------------------------------
        // Website
        // --------------------------------------------------------

        if (websiteUrl.Length > 2048)
        {
            throw new BadRequestException(
                "Website URL cannot exceed 2048 characters.");
        }

        if (websiteUrl.Length > 0 &&
            !IsHttpUrl(websiteUrl))
        {
            throw new BadRequestException(
                "Website URL must use HTTP or HTTPS.");
        }

        // --------------------------------------------------------
        // SEO
        // --------------------------------------------------------

        if (seoTitle.Length > 160 ||
            !IsSafeText(seoTitle))
        {
            throw new BadRequestException(
                "SEO title cannot exceed 160 characters.");
        }

        if (seoDescription.Length > 320 ||
            !IsSafeText(seoDescription))
        {
            throw new BadRequestException(
                "SEO description cannot exceed 320 characters.");
        }

        // --------------------------------------------------------
        // Tenant
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

        if (storeName.Length > 0)
        {
            tenantEntity.Name =
                storeName;
        }

        tenantEntity.LogoUrl =
            logoUrl.Length == 0
                ? null
                : logoUrl;

        tenantEntity.PrimaryColor =
            color.Length == 0
                ? null
                : color;

        // --------------------------------------------------------
        // Settings
        // --------------------------------------------------------

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

        await settings.SetAsync(
            AppearanceSettings.ContactPhone,
            contactPhone,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await settings.SetAsync(
            AppearanceSettings.ContactEmail,
            contactEmail,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await settings.SetAsync(
            AppearanceSettings.ContactAddress,
            contactAddress,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await settings.SetAsync(
            AppearanceSettings.WebsiteUrl,
            websiteUrl,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await settings.SetAsync(
            AppearanceSettings.SeoTitle,
            seoTitle,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await settings.SetAsync(
            AppearanceSettings.SeoDescription,
            seoDescription,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await settings.SetAsync(
            AppearanceSettings.FaviconUrl,
            faviconUrl,
            SettingScope.Tenant,
            tenant.TenantId,
            ct);

        await db.SaveChangesAsync(ct);

        return Results.Ok(
            new AppearanceDto(
                Normalize(tenantEntity.Name),
                Normalize(tenantEntity.LogoUrl),
                Normalize(faviconUrl),
                Normalize(theme),
                Normalize(color),
                Normalize(custom),
                Normalize(contactPhone),
                Normalize(contactEmail),
                Normalize(contactAddress),
                Normalize(websiteUrl),
                Normalize(seoTitle),
                Normalize(seoDescription)));
    }

    private static string NormalizeInput(
        string? value)
    {
        return
            (value ?? string.Empty)
                .Trim();
    }

    private static void ValidateImageUrl(
        string value,
        string fieldName)
    {
        if (value.Length > 2048)
        {
            throw new BadRequestException(
                $"{fieldName} cannot exceed 2048 characters.");
        }

        if (value.Length == 0)
        {
            return;
        }

        if (!IsSafeImageUrl(value))
        {
            throw new BadRequestException(
                $"{fieldName} must be an HTTP, HTTPS or root-relative URL.");
        }
    }

    private static bool IsSafeImageUrl(
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

    private static bool IsHttpUrl(
        string value)
    {
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
                '"',
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
                '"',
                '\'',
                '\r',
                '\n'
            ]) < 0;
    }

    private static string ValidateCustomTheme(
        string? value)
    {
        var normalized =
            (value ?? string.Empty)
                .Trim();

        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        if (normalized.Length > 4000)
        {
            throw new BadRequestException(
                "Custom theme is too large.");
        }

        try
        {
            using var _ =
                JsonDocument.Parse(normalized);

            if (_.RootElement.ValueKind !=
                JsonValueKind.Object)
            {
                throw new BadRequestException(
                    "Custom theme must be a JSON object.");
            }
        }
        catch (JsonException)
        {
            throw new BadRequestException(
                "Custom theme must be valid JSON.");
        }

        return normalized;
    }

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
