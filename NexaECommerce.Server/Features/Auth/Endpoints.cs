using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexaECommerce.Server.Data;
using NexaECommerce.Server.Platform;
using NexaECommerce.Server.Platform.Auditing;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Email;
using NexaECommerce.Server.Platform.Errors;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;
using NexaECommerce.Server.Platform.Identity;
using NexaECommerce.Server.Platform.MultiTenancy;
using NexaECommerce.Server.Platform.RateLimiting;
using NexaECommerce.Server.Platform.Settings;

namespace NexaECommerce.Server.Features.Auth;

/// <summary>
/// Cookie-based auth flows: register → confirm → login → forgot/reset → change password, plus
/// /me for the SPA to bootstrap its session. 2FA and OAuth endpoints live in sibling files of
/// this slice. Auth failures throw typed <see cref="DomainException"/>s mapped to ProblemDetails.
/// </summary>
public sealed class AuthEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        // Unauthenticated credential endpoints carry a strict per-IP rate limit on top.
        // /me and authenticated writes are exempt and rely on the global /api policy.
        var credentials = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .RequireRateLimiting(RateLimitSetup.Auth);

        credentials.MapPost("/register", Register);
        credentials.MapPost("/confirm-email", ConfirmEmail);
        credentials.MapPost("/resend-confirmation", ResendConfirmation);
        credentials.MapPost("/login", Login);
        credentials.MapPost("/forgot-password", ForgotPassword);
        credentials.MapPost("/reset-password", ResetPassword);

        group.MapPost("/logout", Logout);
        group.MapGet("/public-config", PublicConfig);

        group.MapGet("/me", Me)
            .RequireAuthorization();

        group.MapGet("/tenants", MyTenants)
            .RequireAuthorization();

        group.MapPut("/tenant", SwitchTenant)
            .RequireAuthorization();

        group.MapPut("/profile", UpdateProfile)
            .RequireAuthorization();

        group.MapPut("/preferences", UpdatePreferences)
            .RequireAuthorization();

        group.MapPost("/change-password", ChangePassword)
            .RequireAuthorization();
    }

    // ============================================================
    // Public configuration
    // ============================================================

    /// <summary>
    /// Anonymous bootstrap for the sign-in / register screens.
    /// </summary>
    private static async Task<IResult> PublicConfig(
        ISettingService settings,
        IOptions<AppOptions> appOptions,
        CancellationToken ct)
    {
        var demo = appOptions.Value.DemoLogin;

        object? demoLogin =
            !string.IsNullOrWhiteSpace(demo?.Email) &&
            !string.IsNullOrWhiteSpace(demo?.Password)
                ? new
                {
                    email = demo!.Email!.Trim(),
                    password = demo.Password
                }
                : null;

        return Results.Ok(new
        {
            allowRegistration =
                await settings.GetAsync<bool>(
                    AccountSettings.AllowRegistration,
                    ct),

            demoLogin
        });
    }

    // ============================================================
    // Current user profile
    // ============================================================

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest req,
        UserManager<AppUser> users,
        ITenantRoleService tenantRoles,
        PermissionResolver permissions,
        HttpContext http)
    {
        var user = await users.GetUserAsync(http.User);

        if (user is null)
            return Results.Unauthorized();

        user.DisplayName =
            string.IsNullOrWhiteSpace(req.DisplayName)
                ? null
                : req.DisplayName.Trim();

        var result = await users.UpdateAsync(user);

        if (!result.Succeeded)
            throw IdentityErrors(result);

        return Results.Ok(
            await user.ToAuthDtoAsync(
                tenantRoles,
                permissions));
    }

    private static async Task<IResult> UpdatePreferences(
        UpdatePreferencesRequest req,
        UserManager<AppUser> users,
        ITenantRoleService tenantRoles,
        PermissionResolver permissions,
        HttpContext http)
    {
        var user = await users.GetUserAsync(http.User);

        if (user is null)
            return Results.Unauthorized();

        user.Locale =
            string.IsNullOrWhiteSpace(req.Locale)
                ? null
                : req.Locale.Trim();

        user.TimeZone =
            string.IsNullOrWhiteSpace(req.TimeZone)
                ? null
                : req.TimeZone.Trim();

        var result = await users.UpdateAsync(user);

        if (!result.Succeeded)
            throw IdentityErrors(result);

        return Results.Ok(
            await user.ToAuthDtoAsync(
                tenantRoles,
                permissions));
    }

    // ============================================================
    // Tenant management
    // ============================================================

    /// <summary>
    /// Returns the active tenants available to the authenticated user.
    /// The current tenant is marked with Current = true.
    /// </summary>
    private static async Task<IResult> MyTenants(
        UserManager<AppUser> users,
        ITenantRoleService tenantRoles,
        AppDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(http.User);

        if (user is null)
            return Results.Unauthorized();

        var tenantIds =
            await tenantRoles.TenantIdsForUserAsync(
                user.Id,
                ct);

        // In single-tenant mode the default tenant is the only valid tenant.
        if (tenantIds.Count == 0)
        {
            tenantIds =
            [
                TenancyOptions.DefaultTenant
            ];
        }

        var tenants =
            await db.Set<Tenant>()
                .AsNoTracking()
                .Where(t =>
                    tenantIds.Contains(t.Id) &&
                    t.Status == TenantStatus.Active)
                .OrderBy(t => t.Name)
                .Select(t => new TenantDto(
                    t.Id,
                    t.Name,
                    t.PrimaryColor,
                    t.LogoUrl,
                    t.Id == user.TenantId))
                .ToListAsync(ct);

        return Results.Ok(tenants);
    }

    /// <summary>
    /// Changes the authenticated user's active tenant.
    ///
    /// The target tenant must:
    /// 1. Exist.
    /// 2. Be active.
    /// 3. Be a tenant the authenticated user belongs to.
    ///
    /// Changing the active tenant updates AppUser.TenantId and refreshes the
    /// authentication cookie so the claims factory rebuilds the tenant-scoped
    /// roles and permissions.
    /// </summary>
    private static async Task<IResult> SwitchTenant(
        SwitchTenantRequest req,
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn,
        ITenantRoleService tenantRoles,
        PermissionResolver permissions,
        AppDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(http.User);

        if (user is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(req.TenantId))
            throw new BadRequestException(
                "Tenant id is required.");

        var tenantId =
            req.TenantId.Trim().ToLowerInvariant();

        var tenant =
            await db.Set<Tenant>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.Id == tenantId,
                    ct);

        if (tenant is null)
        {
            throw new NotFoundException(
                "Tenant",
                tenantId);
        }

        if (tenant.Status != TenantStatus.Active)
        {
            throw new ForbiddenException(
                "The selected tenant is suspended.");
        }

        // In single-tenant mode only the default tenant is legal.
        var tenancyOptions =
            http.RequestServices
                .GetRequiredService<
                    IOptions<TenancyOptions>>()
                .Value;

        if (tenancyOptions.Mode == TenancyMode.SingleTenant &&
            tenantId != TenancyOptions.DefaultTenant)
        {
            throw new ForbiddenException(
                "Tenant switching is disabled in single-tenant mode.");
        }

        var memberships =
            await tenantRoles.TenantIdsForUserAsync(
                user.Id,
                ct);

        // The default tenant can be used in single-tenant mode even when
        // no explicit TenantUserRole row is present.
        var isAllowed =
            tenantId == TenancyOptions.DefaultTenant &&
            tenancyOptions.Mode == TenancyMode.SingleTenant
                ? true
                : memberships.Contains(
                    tenantId,
                    StringComparer.OrdinalIgnoreCase);

        if (!isAllowed)
        {
            throw new ForbiddenException(
                "You are not a member of the selected tenant.");
        }

        // No state change required.
        if (string.Equals(
                user.TenantId,
                tenantId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok(
                await user.ToAuthDtoAsync(
                    tenantRoles,
                    permissions));
        }

        user.TenantId = tenantId;

        var update =
            await users.UpdateAsync(user);

        if (!update.Succeeded)
            throw IdentityErrors(update);

        // Refresh the cookie so AppUserClaimsPrincipalFactory runs again and
        // projects the roles + permissions belonging to the new active tenant.
        await signIn.RefreshSignInAsync(user);

        return Results.Ok(
            await user.ToAuthDtoAsync(
                tenantRoles,
                permissions));
    }

    // ============================================================
    // Registration
    // ============================================================

    private static async Task<IResult> Register(
        RegisterRequest req,
        UserManager<AppUser> users,
        RoleManager<IdentityRole> roles,
        IEmailSender email,
        ISettingService settings,
        ITenantRoleService tenantRoles,
        ITenantContext tenant,
        IOptions<AppOptions> appOptions,
        HttpContext http,
        CancellationToken ct)
    {
        if (await settings.GetAsync<bool>(
                AccountSettings.AllowRegistration,
                ct) is false)
        {
            throw new ForbiddenException(
                "Registration is currently disabled. Contact an administrator for an invitation.");
        }

        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.DisplayName
        };

        var result =
            await users.CreateAsync(
                user,
                req.Password);

        if (!result.Succeeded)
            throw IdentityErrors(result);

        await AssignDefaultRoleAsync(
            user,
            settings,
            roles,
            tenantRoles,
            tenant,
            ct);

        var token =
            await users.GenerateEmailConfirmationTokenAsync(
                user);

        await AuthEmails.SendEmailConfirmationAsync(
            email,
            user,
            token,
            AuthUrls.ClientBaseUrl(
                http,
                appOptions.Value),
            appOptions.Value.ProductName,
            appOptions.Value.BrandColor,
            ct);

        return Results.Ok(new
        {
            message =
                "Account created. Check your email to confirm your address."
        });
    }

    /// <summary>
    /// Grants the configured Account.DefaultRole to a brand-new account.
    /// </summary>
    internal static async Task AssignDefaultRoleAsync(
        AppUser user,
        ISettingService settings,
        RoleManager<IdentityRole> roles,
        ITenantRoleService tenantRoles,
        ITenantContext tenant,
        CancellationToken ct)
    {
        var roleName =
            await settings.GetAsync<string>(
                AccountSettings.DefaultRole,
                ct);

        if (string.IsNullOrWhiteSpace(roleName))
            return;

        var role =
            await roles.FindByNameAsync(roleName);

        if (role is not null)
        {
            await tenantRoles.GrantRoleAsync(
                user.Id,
                tenant.TenantId,
                role.Id,
                ct);
        }
    }

    // ============================================================
    // Email confirmation
    // ============================================================

    private static async Task<IResult> ConfirmEmail(
        ConfirmEmailRequest req,
        UserManager<AppUser> users)
    {
        var user =
            await users.FindByIdAsync(req.UserId);

        if (user is null)
            throw new BadRequestException(
                "Invalid confirmation link.");

        // Idempotent by design.
        if (user.EmailConfirmed)
        {
            return Results.Ok(new
            {
                message =
                    "Email confirmed. You can now sign in."
            });
        }

        var result =
            await users.ConfirmEmailAsync(
                user,
                Decode(req.Token));

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                "Invalid or expired confirmation link.");
        }

        return Results.Ok(new
        {
            message =
                "Email confirmed. You can now sign in."
        });
    }

    private static async Task<IResult> ResendConfirmation(
        ResendConfirmationRequest req,
        UserManager<AppUser> users,
        IEmailSender email,
        IOptions<AppOptions> appOptions,
        HttpContext http,
        CancellationToken ct)
    {
        var user =
            await users.FindByEmailAsync(
                req.Email);

        if (user is { EmailConfirmed: false })
        {
            var token =
                await users.GenerateEmailConfirmationTokenAsync(
                    user);

            await AuthEmails.SendEmailConfirmationAsync(
                email,
                user,
                token,
                AuthUrls.ClientBaseUrl(
                    http,
                    appOptions.Value),
                appOptions.Value.ProductName,
                appOptions.Value.BrandColor,
                ct);
        }

        // Never reveal whether the email exists.
        return Results.Ok(new
        {
            message =
                "If that address needs confirmation, a new link is on its way."
        });
    }

    // ============================================================
    // Login
    // ============================================================

    private static async Task<IResult> Login(
        LoginRequest req,
        SignInManager<AppUser> signIn,
        UserManager<AppUser> users,
        ITenantRoleService tenantRoles,
        PermissionResolver permissions,
        IAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        var user =
            await users.FindByEmailAsync(
                req.Email);

        if (user is null)
        {
            throw new UnauthorizedException(
                "Invalid email or password.",
                "INVALID_CREDENTIALS");
        }

        var result =
            await signIn.PasswordSignInAsync(
                user,
                req.Password,
                req.RememberMe,
                lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            return Results.Ok(
                new LoginResultDto(
                    RequiresTwoFactor: true,
                    User: null));
        }

        if (result.IsLockedOut)
        {
            throw new UnauthorizedException(
                "This account is temporarily locked. Try again later.",
                "ACCOUNT_LOCKED");
        }

        if (result.IsNotAllowed)
        {
            throw new UnauthorizedException(
                "Confirm your email before signing in.",
                "EMAIL_NOT_CONFIRMED");
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizedException(
                "Invalid email or password.",
                "INVALID_CREDENTIALS");
        }

        await audit.LogAsync(
            "Auth",
            "Login",
            "AppUser",
            user.Id,
            cancellationToken: ct);

        return Results.Ok(
            new LoginResultDto(
                RequiresTwoFactor: false,
                User:
                    await user.ToAuthDtoAsync(
                        tenantRoles,
                        permissions)));
    }

    // ============================================================
    // Logout
    // ============================================================

    private static async Task<IResult> Logout(
        SignInManager<AppUser> signIn,
        IAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            http.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        await audit.LogAsync(
            "Auth",
            "Logout",
            "AppUser",
            userId,
            cancellationToken: ct);

        await signIn.SignOutAsync();

        return Results.Ok();
    }

    // ============================================================
    // Password reset
    // ============================================================

    private static async Task<IResult> ForgotPassword(
        ForgotPasswordRequest req,
        UserManager<AppUser> users,
        IEmailSender email,
        IOptions<AppOptions> appOptions,
        HttpContext http,
        CancellationToken ct)
    {
        var user =
            await users.FindByEmailAsync(
                req.Email);

        if (user is not null)
        {
            var token =
                await users.GeneratePasswordResetTokenAsync(
                    user);

            await AuthEmails.SendPasswordResetAsync(
                email,
                user,
                token,
                AuthUrls.ClientBaseUrl(
                    http,
                    appOptions.Value),
                appOptions.Value.ProductName,
                appOptions.Value.BrandColor,
                ct);
        }

        // Constant response regardless of existence.
        return Results.Ok(new
        {
            message =
                "If an account exists for that email, a reset link is on its way."
        });
    }

    private static async Task<IResult> ResetPassword(
        ResetPasswordRequest req,
        UserManager<AppUser> users)
    {
        var user =
            await users.FindByEmailAsync(
                req.Email);

        if (user is null)
        {
            throw new BadRequestException(
                "Invalid or expired reset link.");
        }

        var result =
            await users.ResetPasswordAsync(
                user,
                Decode(req.Token),
                req.NewPassword);

        if (!result.Succeeded)
        {
            throw IdentityErrors(
                result,
                passwordField: "newPassword");
        }

        return Results.Ok(new
        {
            message =
                "Your password has been reset. You can now sign in."
        });
    }

    // ============================================================
    // Current authenticated user
    // ============================================================

    private static async Task<IResult> Me(
        UserManager<AppUser> users,
        ITenantRoleService tenantRoles,
        PermissionResolver permissions,
        HttpContext http)
    {
        var user =
            await users.GetUserAsync(
                http.User);

        return user is null
            ? Results.Unauthorized()
            : Results.Ok(
                await user.ToAuthDtoAsync(
                    tenantRoles,
                    permissions));
    }

    // ============================================================
    // Change password
    // ============================================================

    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest req,
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn,
        IAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        var user =
            await users.GetUserAsync(
                http.User);

        if (user is null)
            return Results.Unauthorized();

        // OAuth-only account can set its first password.
        var hadPassword =
            await users.HasPasswordAsync(user);

        var result =
            hadPassword
                ? await users.ChangePasswordAsync(
                    user,
                    req.CurrentPassword ?? string.Empty,
                    req.NewPassword)
                : await users.AddPasswordAsync(
                    user,
                    req.NewPassword);

        if (!result.Succeeded)
        {
            throw IdentityErrors(
                result,
                passwordField: "newPassword");
        }

        // Refresh the current cookie because changing a password rotates
        // the security stamp.
        await signIn.RefreshSignInAsync(user);

        await audit.LogAsync(
            "Auth",
            hadPassword
                ? "PasswordChanged"
                : "PasswordSet",
            "AppUser",
            user.Id,
            cancellationToken: ct);

        return Results.Ok(new
        {
            message =
                hadPassword
                    ? "Password changed."
                    : "Password set."
        });
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static string Decode(string encoded)
    {
        try
        {
            return Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(encoded));
        }
        catch (FormatException)
        {
            throw new BadRequestException(
                "Invalid or malformed token.");
        }
    }

    /// <summary>
    /// Maps Identity errors to form fields so the SPA can display
    /// inline validation messages.
    /// </summary>
    private static ValidationException IdentityErrors(
        IdentityResult result,
        string passwordField = "password")
    {
        var byField =
            new Dictionary<string, List<string>>();

        foreach (var error in result.Errors)
        {
            var field =
                error.Code switch
                {
                    "PasswordMismatch"
                        => "currentPassword",

                    _
                        when error.Code.StartsWith(
                            "Password",
                            StringComparison.Ordinal)
                        => passwordField,

                    "DuplicateEmail"
                    or "DuplicateUserName"
                    or "InvalidEmail"
                        => "email",

                    _
                        => string.Empty
                };

            if (!byField.TryGetValue(
                    field,
                    out var list))
            {
                byField[field] = list = [];
            }

            list.Add(error.Description);
        }

        return new ValidationException(
            byField.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToArray()));
    }
}