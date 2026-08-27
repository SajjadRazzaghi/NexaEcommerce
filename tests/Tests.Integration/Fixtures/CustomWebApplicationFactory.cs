using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NexaECommerce.Server.Data;
using NexaECommerce.Server.Platform.Authorization;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

namespace NexaECommerce.Tests.Integration.Fixtures;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string LoginEmail =
        "integration-admin@nexaecommerce.test";

    public const string LoginPassword =
        "IntegrationP@ss1";

    private readonly string _dbPath =
        Path.Combine(
            Path.GetTempPath(),
            $"nexaecommerce-test-{Guid.NewGuid():N}.db");

    private readonly string _cartDbPath =
        Path.Combine(
            Path.GetTempPath(),
            $"nexaecommerce-test-cart-{Guid.NewGuid():N}.db");

    private readonly string _hangfirePath =
        Path.Combine(
            Path.GetTempPath(),
            $"nexaecommerce-test-hangfire-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:Hangfire",
            _hangfirePath);

        builder.ConfigureTestServices(
            services =>
            {
                // ========================================================
                // AppDbContext / Identity
                // ========================================================

                services.RemoveAll(
                    typeof(AppDbContext));

                services.RemoveAll(
                    typeof(
                        DbContextOptions<AppDbContext>));

                services.RemoveAll(
                    typeof(
                        IDbContextOptionsConfiguration
                            <AppDbContext>));

                services.AddDbContext<AppDbContext>(
                    options =>
                    {
                        options.UseSqlite(
                            $"Data Source={_dbPath}");
                    });

                // ========================================================
                // ShoppingCartDbContext
                // ========================================================

                services.RemoveAll(
                    typeof(ShoppingCartDbContext));

                services.RemoveAll(
                    typeof(
                        DbContextOptions
                            <ShoppingCartDbContext>));

                services.RemoveAll(
                    typeof(
                        IDbContextOptionsConfiguration
                            <ShoppingCartDbContext>));

                services.AddDbContext<ShoppingCartDbContext>(
                    options =>
                    {
                        options.UseSqlite(
                            $"Data Source={_cartDbPath}");
                    });

                // ========================================================
                // Test authentication
                // ========================================================

                services.AddAuthentication(
                    options =>
                    {
                        options.DefaultScheme =
                            TestAuthHandler.SchemeName;

                        options.DefaultAuthenticateScheme =
                            TestAuthHandler.SchemeName;

                        options.DefaultChallengeScheme =
                            TestAuthHandler.SchemeName;
                    })
                    .AddScheme<
                        AuthenticationSchemeOptions,
                        TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });
            });
    }

    public HttpClient CreateAuthenticatedClient(
        string userId = "test-user",
        params string[] permissions)
    {
        var client =
            CreateClient();

        client.DefaultRequestHeaders.Add(
            TestAuthHandler.UserIdHeader,
            userId);

        if (permissions.Length > 0)
        {
            client.DefaultRequestHeaders.Add(
                TestAuthHandler.PermissionsHeader,
                string.Join(
                    ',',
                    permissions));
        }

        return client;
    }

    public async ValueTask InitializeAsync()
    {
        using var scope =
            Services.CreateScope();

        var sp =
            scope.ServiceProvider;

        // ========================================================
        // Identity database
        // ========================================================

        var db =
            sp.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        // ========================================================
        // Shopping Cart database
        // ========================================================

        var cartDb =
            sp.GetRequiredService<ShoppingCartDbContext>();

        await cartDb.Database.EnsureCreatedAsync();

        // ========================================================
        // System roles
        // ========================================================

        var roles =
            sp.GetRequiredService<
                RoleManager<IdentityRole>>();

        await EnsureAdminRoleAsync(roles);
        await EnsureMemberRoleAsync(roles);

        // ========================================================
        // Integration user
        // ========================================================

        var users =
            sp.GetRequiredService<
                UserManager<AppUser>>();

        if (await users.FindByEmailAsync(
                LoginEmail) is null)
        {
            var user =
                new AppUser
                {
                    UserName = LoginEmail,
                    Email = LoginEmail,
                    EmailConfirmed = true,
                    DisplayName = "Integration Admin"
                };

            var result =
                await users.CreateAsync(
                    user,
                    LoginPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Seed user creation failed: " +
                    string.Join(
                        "; ",
                        result.Errors.Select(
                            e => e.Description)));
            }
        }
    }

    private static async Task EnsureAdminRoleAsync(
        RoleManager<IdentityRole> roles)
    {
        var role =
            await roles.FindByNameAsync(
                SystemRoles.Admin);

        if (role is null)
        {
            role =
                new IdentityRole(
                    SystemRoles.Admin);

            var created =
                await roles.CreateAsync(role);

            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin role creation failed: " +
                    string.Join(
                        "; ",
                        created.Errors.Select(
                            e => e.Description)));
            }
        }

        var claims =
            await roles.GetClaimsAsync(role);

        var hasAllPermission =
            claims.Any(
                c =>
                    c.Type ==
                        PermissionClaims.ClaimType &&
                    c.Value ==
                        PermissionClaims.All);

        if (!hasAllPermission)
        {
            var result =
                await roles.AddClaimAsync(
                    role,
                    new Claim(
                        PermissionClaims.ClaimType,
                        PermissionClaims.All));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin permission seeding failed: " +
                    string.Join(
                        "; ",
                        result.Errors.Select(
                            e => e.Description)));
            }
        }
    }

    private static async Task EnsureMemberRoleAsync(
        RoleManager<IdentityRole> roles)
    {
        if (await roles.FindByNameAsync(
                SystemRoles.Member) is not null)
        {
            return;
        }

        var role =
            new IdentityRole(
                SystemRoles.Member);

        var result =
            await roles.CreateAsync(role);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Member role creation failed: " +
                string.Join(
                    "; ",
                    result.Errors.Select(
                        e => e.Description)));
        }
    }

    protected override void Dispose(
        bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        TryDelete(_dbPath);
        TryDelete(_cartDbPath);
        TryDelete(_hangfirePath);

        static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
    }
}