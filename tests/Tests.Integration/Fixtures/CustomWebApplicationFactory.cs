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

namespace NexaECommerce.Tests.Integration.Fixtures;

/// <summary>
/// Boots the real application pipeline in-memory (every slice, filter, the global ProblemDetails
/// handler, authorization) against a throwaway SQLite database, so a test exercises an endpoint
/// exactly as production would.
///
/// The environment is "Testing" (not Development), so Program.cs skips its boot-time migrate +
/// development seeders. This fixture creates the schema from the model and seeds the required
/// integration-test user.
///
/// Authentication is overridden with TestAuthHandler for tests that need an explicitly controlled
/// principal, while the real Identity services remain registered.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string LoginEmail = "integration-admin@nexaecommerce.test";
    public const string LoginPassword = "IntegrationP@ss1";

    // A real file rather than ":memory:" because multiple connections can access it concurrently.
    private readonly string _dbPath =
        Path.Combine(
            Path.GetTempPath(),
            $"nexaecommerce-test-{Guid.NewGuid():N}.db");

    private readonly string _hangfirePath =
        Path.Combine(
            Path.GetTempPath(),
            $"nexaecommerce-test-hangfire-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Keep Hangfire's SQLite database out of the repository.
        builder.UseSetting(
            "ConnectionStrings:Hangfire",
            _hangfirePath);

        builder.ConfigureTestServices(services =>
        {
            // ------------------------------------------------------------
            // Replace the production AppDbContext provider.
            //
            // EF Core 9+ keeps the AddDbContext options action in
            // IDbContextOptionsConfiguration<TContext>. Removing only
            // DbContextOptions<TContext> leaves the original SQL Server
            // configuration alive.
            // ------------------------------------------------------------

            services.RemoveAll(typeof(AppDbContext));
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(
                    $"Data Source={_dbPath}");
            });

            // ------------------------------------------------------------
            // Test authentication
            // ------------------------------------------------------------

            // AddIdentity sets explicit default authentication/challenge
            // schemes to its application cookie. Override all three so
            // integration tests can create deterministic principals.
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
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

    /// <summary>
    /// Creates an HttpClient whose requests authenticate as the supplied user
    /// and permissions through the test authentication handler.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        string userId = "test-user",
        params string[] permissions)
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Add(
            TestAuthHandler.UserIdHeader,
            userId);

        if (permissions.Length > 0)
        {
            client.DefaultRequestHeaders.Add(
                TestAuthHandler.PermissionsHeader,
                string.Join(',', permissions));
        }

        return client;
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();

        // SQLite is the integration-test provider.
        // The schema comes directly from the current EF Core model.
        await db.Database.EnsureCreatedAsync();

        // ------------------------------------------------------------
        // Seed the user used by the real login-flow integration tests.
        // ------------------------------------------------------------

        var users = sp.GetRequiredService<UserManager<AppUser>>();

        if (await users.FindByEmailAsync(LoginEmail) is null)
        {
            var user = new AppUser
            {
                UserName = LoginEmail,
                Email = LoginEmail,
                EmailConfirmed = true,
                DisplayName = "Integration Admin"
            };

            var result = await users.CreateAsync(
                user,
                LoginPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Seed user creation failed: " +
                    string.Join(
                        "; ",
                        result.Errors.Select(e => e.Description)));
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        TryDelete(_dbPath);
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
                // A background writer may still hold the file.
                // The operating system will clean the temporary file.
            }
        }
    }
}