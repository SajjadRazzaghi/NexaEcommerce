// NexaECommerce.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Catalog.Infrastructure.SeedData;
using NexaECommerce.Server.Extensions;
using NexaECommerce.Server.Data;
using NexaECommerce.Server.Platform;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. Serilog (Logging)
// ============================================================
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console();
});

// ============================================================
// 2. Services
// ============================================================
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "NexaEcommerce API",
        Version = "v1",
        Description = "NexaEcommerce API"
    });
});

// ============================================================
// 3. Platform - Authentication & Authorization
// ============================================================
builder.Services.AddPlatform(builder.Configuration);

// ============================================================
// 4. AppDbContext
// ============================================================
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("Default");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'Default' was not found.");
    }

    options.UseSqlServer(connectionString);
});

// ============================================================
// 5. CORS
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
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
// 7. Background Workers
// ============================================================
//
// Inventory expiration and reconciliation are operational
// background processes. Integration tests use isolated databases
// and deterministic request flows, so these workers must not run
// in the Testing environment. Running them during tests can mutate
// the same inventory records that Checkout is validating and
// reserving.
//
// They remain enabled in Development/Production.
//
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<
        NexaECommerce.Server.Features.Inventory
            .ReservationExpirationWorker>();

    builder.Services.AddHostedService<
        NexaECommerce.Server.Features.Inventory
            .InventoryOrderReconciliationWorker>();
}

// ============================================================
// 8. Build Application
// ============================================================
var app = builder.Build();

// ============================================================
// 9. Exception Handling
// ============================================================
app.UseExceptionHandler();

// ============================================================
// 10. Request Logging
// ============================================================
app.UseSerilogRequestLogging();

// ============================================================
// 11. Static Files
// ============================================================
app.UseDefaultFiles();
app.MapStaticAssets();
app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(
            builder.Environment.ContentRootPath,
            "wwwroot")),
    RequestPath = "/uploads"
});

// ============================================================
// 12. Swagger / Scalar
// ============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "NexaEcommerce API V1");

        c.RoutePrefix = "swagger";
    });

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("NexaEcommerce API")
            .WithOpenApiRoutePattern(
                "/swagger/{documentName}/swagger.json");
    });
}

// ============================================================
// 13. HTTPS Redirection
// ============================================================
app.UseHttpsRedirection();

// ============================================================
// 14. Routing
// ============================================================
app.UseRouting();

// ============================================================
// 15. CORS
// ============================================================
app.UseCors("AllowReactApp");

// ============================================================
// 16. Authentication
// ============================================================
app.UseAuthentication();

app.UseMiddleware<TenantResolutionMiddleware>();

// ============================================================
// 17. Authorization
// ============================================================
app.UseAuthorization();

// ============================================================
// 18. Endpoints
// ============================================================
app.MapControllers();
app.MapAllFeatures();

// ============================================================
// 19. Database Migration
// ============================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var catalogContext =
            scope.ServiceProvider
                .GetRequiredService<CatalogDbContext>();

        await catalogContext.Database.MigrateAsync();

        await DbInitializer.InitializeAsync(
            catalogContext);

        app.Logger.LogInformation(
            "Catalog database migration completed successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Catalog database initialization failed");
    }
}

// ============================================================
// 20. Run
// ============================================================
app.Run();

public partial class Program;
