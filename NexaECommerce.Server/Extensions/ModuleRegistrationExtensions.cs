// NexaEcommerce.Server/Extensions/ModuleRegistrationExtensions.cs
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog;
using NexaEcommerce.Modules.Catalog.Infrastructure;

namespace NexaEcommerce.Server.Extensions;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddEcommerceModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Data Source=App_Data/NexaEcommerce.db";
        }

        services.AddCatalogModule(connectionString);

        return services;
    }

    // ✅ متد UseEcommerceModules
    public static IApplicationBuilder UseEcommerceModules(
        this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        try
        {
            var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            catalogContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
            logger?.LogError(ex, "Failed to migrate Catalog database");
        }

        return app;
    }
}