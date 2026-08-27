using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.ShoppingCart;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Repositories;
using NexaECommerce.Server.Features.Cart;
using NexaECommerce.Server.Platform.MultiTenancy;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaECommerce.Server.Extensions;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddEcommerceModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "Default");

        if (string.IsNullOrEmpty(
                connectionString))
        {
            connectionString =
                configuration.GetConnectionString(
                    "DefaultConnection");
        }

        if (string.IsNullOrEmpty(
                connectionString))
        {
            connectionString =
                "Data Source=App_Data/NexaEcommerce.db";
        }

        services.AddCatalogModule(
            connectionString);

        services.AddShoppingCartModule(
            connectionString);

        services.AddScoped<
            IProductVariantReader,
            CatalogProductVariantReader>();

        services.AddScoped<
            ICurrentTenant,
            CurrentTenant>();

        return services;
    }

    public static IApplicationBuilder UseEcommerceModules(
        this IApplicationBuilder app)
    {
        using var scope =
            app.ApplicationServices
                .CreateScope();

        try
        {
            var catalogContext =
                scope.ServiceProvider
                    .GetRequiredService<
                        CatalogDbContext>();

            catalogContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger =
                scope.ServiceProvider
                    .GetService<
                        ILogger<Program>>();

            logger?.LogError(
                ex,
                "Failed to migrate Catalog database");
        }

        return app;
    }
}