using Microsoft.AspNetCore.Mvc;
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
        [FromServices] ISettingService settings,
        [FromServices] ITenantContext tenant,
        [FromServices] AppDbContext db,
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
        [FromServices] ISettingService settings,
        [FromServices] ITenantContext tenant,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        // Null/omitted fields mean "leave unchanged". An empty string explicitly clears
        // optional values such as logo, favicon, color, or SEO fields.
        var storeName = req.StoreName is null ? null : NormalizeInput(req.StoreName);
        var logoUrl = req.LogoUrl is null ? null : NormalizeInput(req.LogoUrl);
        var faviconUrl = req.FaviconUrl is null ? null : NormalizeInput(req.FaviconUrl);
        var theme = req.Theme is null ? null : NormalizeInput(req.Theme);
        var color = req.BrandColor is null ? null : NormalizeInput(req.BrandColor);
        var contactPhone = req.ContactPhone is null ? null : NormalizeInput(req.ContactPhone);
        var contactEmail = req.ContactEmail is null ? null : NormalizeInput(req.ContactEmail);
        var contactAddress = req.ContactAddress is null ? null : NormalizeInput(req.ContactAddress);
        var websiteUrl = req.WebsiteUrl is null ? null : NormalizeInput(req.WebsiteUrl);
        var seoTitle = req.SeoTitle is null ? null : NormalizeInput(req.SeoTitle);
        var seoDescription = req.SeoDescription is null ? null : NormalizeInput(req.SeoDescription);
        var custom = req.CustomTheme is null ? null : ValidateCustomTheme(req.CustomTheme);

        // --------------------------------------------------------
        // Validate only fields sent by the client.
        // --------------------------------------------------------

        if (storeName is not null && storeName.Length > 128)
            throw new BadRequestException("Store name cannot exceed 128 characters.");

        if (storeName is { Length: > 0 } && !IsSafeText(storeName))
            throw new BadRequestException("Store name contains invalid characters.");

        if (logoUrl is not null)
            ValidateImageUrl(logoUrl, "Logo URL");

        if (faviconUrl is not null)
            ValidateImageUrl(faviconUrl, "Favicon URL");

        if (theme is not null && (theme.Length > 32 || !IsSafeValue(theme)))
            throw new BadRequestException("Invalid appearance theme.");

        if (color is not null && (color.Length > 64 || !IsSafeValue(color)))
            throw new BadRequestException("Invalid appearance brand color.");

        if (contactPhone is not null &&
            (contactPhone.Length > 64 || !IsSafeText(contactPhone)))
            throw new BadRequestException("Invalid contact phone.");

        if (contactEmail is not null)
        {
            if (contactEmail.Length > 256)
                throw new BadRequestException("Contact email cannot exceed 256 characters.");

            if (contactEmail.Length > 0)
            {
                try
                {
                    var mail = new MailAddress(contactEmail);
                    if (!string.Equals(mail.Address, contactEmail, StringComparison.OrdinalIgnoreCase))
                        throw new FormatException();
                }
                catch
                {
                    throw new BadRequestException("Invalid contact email.");
                }
            }
        }

        if (contactAddress is not null &&
            (contactAddress.Length > 1000 || !IsSafeText(contactAddress)))
            throw new BadRequestException("Invalid contact address.");

        if (websiteUrl is not null)
        {
            if (websiteUrl.Length > 2048)
                throw new BadRequestException("Website URL cannot exceed 2048 characters.");

            if (websiteUrl.Length > 0 && !IsHttpUrl(websiteUrl))
                throw new BadRequestException("Website URL must use HTTP or HTTPS.");
        }

        if (seoTitle is not null &&
            (seoTitle.Length > 160 || !IsSafeText(seoTitle)))
            throw new BadRequestException("SEO title cannot exceed 160 characters.");

        if (seoDescription is not null &&
            (seoDescription.Length > 320 || !IsSafeText(seoDescription)))
            throw new BadRequestException("SEO description cannot exceed 320 characters.");

        // --------------------------------------------------------
        // Tenant. The default tenant is part of the platform contract;
        // older databases may be missing its seeded row, so repair that
        // one known tenant instead of failing with KeyNotFoundException.
        // Never silently create arbitrary tenant IDs.
        // --------------------------------------------------------

        var tenantId = string.IsNullOrWhiteSpace(tenant.TenantId)
            ? TenancyOptions.DefaultTenant
            : tenant.TenantId.Trim().ToLowerInvariant();

        var tenantEntity = await db.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenantEntity is null &&
            string.Equals(tenantId, TenancyOptions.DefaultTenant, StringComparison.OrdinalIgnoreCase))
        {
            var defaultTenant = new Tenant
            {
                Id = TenancyOptions.DefaultTenant,
                Name = storeName is { Length: > 0 } ? storeName : "Default",
                Status = TenantStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.Set<Tenant>().Add(defaultTenant);
            try
            {
                await db.SaveChangesAsync(ct);
                tenantEntity = defaultTenant;
            }
            catch (DbUpdateException)
            {
                // A concurrent request may have inserted the seed row first.
                db.Entry(defaultTenant).State = EntityState.Detached;
                tenantEntity = await db.Set<Tenant>()
                    .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
                if (tenantEntity is null)
                    throw;
            }
        }

        if (tenantEntity is null)
            throw new NotFoundException("Tenant", tenantId);

        if (tenantEntity.Status != TenantStatus.Active)
            throw new ForbiddenException("The selected tenant is suspended.");

        if (storeName is { Length: > 0 })
            tenantEntity.Name = storeName;

        if (logoUrl is not null)
            tenantEntity.LogoUrl = logoUrl.Length == 0 ? null : logoUrl;

        if (color is not null)
            tenantEntity.PrimaryColor = color.Length == 0 ? null : color;

        await db.SaveChangesAsync(ct);

        // --------------------------------------------------------
        // Persist only the settings explicitly provided by the caller.
        // --------------------------------------------------------

        if (theme is not null)
            await settings.SetAsync(AppearanceSettings.Theme, theme, SettingScope.Tenant, tenantId, ct);

        if (color is not null)
            await settings.SetAsync(AppearanceSettings.BrandColor, color, SettingScope.Tenant, tenantId, ct);

        if (custom is not null)
            await settings.SetAsync(AppearanceSettings.CustomTheme, custom, SettingScope.Tenant, tenantId, ct);

        if (contactPhone is not null)
            await settings.SetAsync(AppearanceSettings.ContactPhone, contactPhone, SettingScope.Tenant, tenantId, ct);

        if (contactEmail is not null)
            await settings.SetAsync(AppearanceSettings.ContactEmail, contactEmail, SettingScope.Tenant, tenantId, ct);

        if (contactAddress is not null)
            await settings.SetAsync(AppearanceSettings.ContactAddress, contactAddress, SettingScope.Tenant, tenantId, ct);

        if (websiteUrl is not null)
            await settings.SetAsync(AppearanceSettings.WebsiteUrl, websiteUrl, SettingScope.Tenant, tenantId, ct);

        if (seoTitle is not null)
            await settings.SetAsync(AppearanceSettings.SeoTitle, seoTitle, SettingScope.Tenant, tenantId, ct);

        if (seoDescription is not null)
            await settings.SetAsync(AppearanceSettings.SeoDescription, seoDescription, SettingScope.Tenant, tenantId, ct);

        if (faviconUrl is not null)
            await settings.SetAsync(AppearanceSettings.FaviconUrl, faviconUrl, SettingScope.Tenant, tenantId, ct);

        // Return the persisted, canonical appearance (including untouched fields).
        var responseTheme = await settings.GetAsync<string>(AppearanceSettings.Theme, ct);
        var responseColor = await settings.GetAsync<string>(AppearanceSettings.BrandColor, ct);
        if (string.IsNullOrWhiteSpace(responseColor))
            responseColor = tenantEntity.PrimaryColor;

        var responseCustom = await settings.GetAsync<string>(AppearanceSettings.CustomTheme, ct);
        var responseContactPhone = await settings.GetAsync<string>(AppearanceSettings.ContactPhone, ct);
        var responseContactEmail = await settings.GetAsync<string>(AppearanceSettings.ContactEmail, ct);
        var responseContactAddress = await settings.GetAsync<string>(AppearanceSettings.ContactAddress, ct);
        var responseWebsiteUrl = await settings.GetAsync<string>(AppearanceSettings.WebsiteUrl, ct);
        var responseSeoTitle = await settings.GetAsync<string>(AppearanceSettings.SeoTitle, ct);
        var responseSeoDescription = await settings.GetAsync<string>(AppearanceSettings.SeoDescription, ct);
        var responseFaviconUrl = await settings.GetAsync<string>(AppearanceSettings.FaviconUrl, ct);

        return Results.Ok(new AppearanceDto(
            Normalize(tenantEntity.Name),
            Normalize(tenantEntity.LogoUrl),
            Normalize(responseFaviconUrl),
            Normalize(responseTheme),
            Normalize(responseColor),
            Normalize(responseCustom),
            Normalize(responseContactPhone),
            Normalize(responseContactEmail),
            Normalize(responseContactAddress),
            Normalize(responseWebsiteUrl),
            Normalize(responseSeoTitle),
            Normalize(responseSeoDescription)));
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
