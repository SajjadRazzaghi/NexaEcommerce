// NexaECommerce.Server/Program.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaECommerce.Server.Data;
using NexaECommerce.Server.Extensions;
using NexaECommerce.Server.Platform;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using Scalar.AspNetCore;
using Serilog;

var builder =
    WebApplication.CreateBuilder(
        args);

// ============================================================
// 1. Serilog (Logging)
// ============================================================

builder.Host.UseSerilog(
    (
        context,
        config) =>
    {
        config
            .ReadFrom.Configuration(
                context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console();
    });

// ============================================================
// 2. Services
// ============================================================

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(
    c =>
    {
        c.SwaggerDoc(
            "v1",
            new()
            {
                Title =
                    "NexaEcommerce API",

                Version =
                    "v1",

                Description =
                    "NexaEcommerce API"
            });
    });

// ============================================================
// 3. Platform - Authentication & Authorization
// ============================================================

builder.Services.AddPlatform(
    builder.Configuration);

// ============================================================
// 4. AppDbContext
// ============================================================

builder.Services.AddDbContext<AppDbContext>(
    (
        serviceProvider,
        options) =>
    {
        var connectionString =
            builder.Configuration
                .GetConnectionString(
                    "Default");

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Default' was not found.");
        }

        options.UseSqlServer(
            connectionString);
    });

// ============================================================
// 5. CORS
// ============================================================

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "AllowReactApp",
            policy =>
            {
                policy
                    .WithOrigins(
                        "https://localhost:3000",
                        "http://localhost:5173",
                        "https://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });

// ============================================================
// 6. Ecommerce Modules
// ============================================================

builder.Services.AddEcommerceModules(
    builder.Configuration);

// ============================================================
// 7. Ecommerce Database Initializer
// ============================================================

builder.Services.AddScoped<
    EcommerceDatabaseInitializer>();

// ============================================================
// 8. Background Workers
// ============================================================
//
// Inventory expiration and reconciliation are operational
// background processes. Integration tests use isolated databases
// and deterministic request flows, so these workers must not run
// in the Testing environment.
//
// They remain enabled in Development/Production.
//

if (!builder.Environment.IsEnvironment(
        "Testing"))
{
    builder.Services.AddHostedService<
        NexaECommerce.Server.Features.Inventory
            .ReservationExpirationWorker>();

    builder.Services.AddHostedService<
        NexaECommerce.Server.Features.Inventory
            .InventoryOrderReconciliationWorker>();
}

// ============================================================
// 9. Build Application
// ============================================================

var app =
    builder.Build();

// ============================================================
// 10. Exception Handling
// ============================================================

app.UseExceptionHandler();

// ============================================================
// 11. Request Logging
// ============================================================

app.UseSerilogRequestLogging();

// ============================================================
// 12. Static Files
// ============================================================

app.UseDefaultFiles();

app.MapStaticAssets();

app.UseStaticFiles();

app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider =
            new PhysicalFileProvider(
                Path.Combine(
                    builder.Environment.ContentRootPath,
                    "wwwroot")),

        RequestPath =
            "/uploads"
    });

// ============================================================
// 13. Swagger / Scalar
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(
        c =>
        {
            c.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "NexaEcommerce API V1");

            c.RoutePrefix =
                "swagger";
        });

    app.MapScalarApiReference(
        options =>
        {
            options
                .WithTitle(
                    "NexaEcommerce API")
                .WithOpenApiRoutePattern(
                    "/swagger/{documentName}/swagger.json");
        });
}

// ============================================================
// 14. HTTPS Redirection
// ============================================================

app.UseHttpsRedirection();

// ============================================================
// 15. Routing
// ============================================================

app.UseRouting();

// ============================================================
// 16. CORS
// ============================================================

app.UseCors(
    "AllowReactApp");

// ============================================================
// 17. Authentication
// ============================================================

app.UseAuthentication();

app.UseMiddleware<
    TenantResolutionMiddleware>();

// ============================================================
// 18. Authorization
// ============================================================

app.UseAuthorization();

// ============================================================
// 19. Endpoints
// ============================================================

app.MapControllers();

app.MapAllFeatures();

// ============================================================
// 20. Database + Ecommerce Bootstrap
// ============================================================
//
// This is intentionally performed before Run() so the application
// never starts serving checkout requests while one of its module
// databases is still missing or outdated.
//
// The initializer:
//   - migrates App/Identity
//   - seeds admin roles/user
//   - migrates Catalog
//   - runs existing Catalog seed
//   - migrates Customers
//   - migrates Inventory
//   - migrates ShoppingCart
//   - migrates Orders
//   - creates the default warehouse/location
//   - creates the default shipping method
//

await using (
    var scope =
        app.Services.CreateAsyncScope())
{
    try
    {
        var initializer =
            scope.ServiceProvider
                .GetRequiredService<
                    EcommerceDatabaseInitializer>();

        await initializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(
            ex,
            "NexaECommerce startup initialization failed. Application cannot start safely.");

        throw;
    }
}

// ============================================================
// 21. Run
// ============================================================

app.Run();

public partial class Program;