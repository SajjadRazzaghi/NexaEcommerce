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
    var connectionString = builder.Configuration.GetConnectionString("Default");

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
// 6. Catalog Module
// ============================================================
builder.Services.AddEcommerceModules(builder.Configuration);
builder.Services.AddHostedService<
    NexaECommerce.Server.Features.Inventory
        .ReservationExpirationWorker>();
// ============================================================
// 7. Build Application
// ============================================================
var app = builder.Build();

// ============================================================
// 8. Exception Handling
// ============================================================
app.UseExceptionHandler();

// ============================================================
// 9. Request Logging
// ============================================================
app.UseSerilogRequestLogging();

// ============================================================
// 10. Static Files
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
// 11. Swagger / Scalar
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
// 12. HTTPS Redirection
// ============================================================
app.UseHttpsRedirection();

// ============================================================
// 13. Routing
// ============================================================
app.UseRouting();

// ============================================================
// 14. CORS
// ============================================================
app.UseCors("AllowReactApp");

// ============================================================
// 15. Authentication
// ============================================================
app.UseAuthentication();

app.UseMiddleware<TenantResolutionMiddleware>();

// ============================================================
// 16. Authorization
// ============================================================
app.UseAuthorization();

// ============================================================
// 17. Endpoints
// ============================================================
app.MapControllers();
app.MapAllFeatures();

// ============================================================
// 18. Database Migration
// ============================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var catalogContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await catalogContext.Database.MigrateAsync();

        await DbInitializer.InitializeAsync(catalogContext);

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
// 19. Run
// ============================================================
app.Run();

public partial class Program;