
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Customers;
using NexaEcommerce.Modules.Inventory;
using NexaEcommerce.Modules.Orders;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.ShoppingCart;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Repositories;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Features.Cart;
using NexaECommerce.Server.Features.Inventory;
using NexaECommerce.Server.Features.Orders;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Extensions;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddEcommerceModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString =
                configuration.GetConnectionString(
                    "DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Default' was not found.");
        }

        // ========================================================
        // Catalog
        // ========================================================

        services.AddCatalogModule(
            connectionString);

        // ========================================================
        // Customers
        // ========================================================

        services.AddCustomersModule(
            configuration);

        // ========================================================
        // Inventory
        // ========================================================

        services.AddInventoryModule(
            connectionString);

        // ========================================================
        // Shopping Cart
        // ========================================================

        services.AddShoppingCartModule(
            connectionString);

        // ========================================================
        // Orders
        // ========================================================

        services.AddOrdersModule(
            connectionString);

        // ========================================================
        // Cross-module readers
        // ========================================================

        services.AddScoped<
            IOrderProductReader,
            CatalogOrderProductReader>();

        services.AddScoped<
            IProductVariantReader,
            CatalogProductVariantReader>();

        // ========================================================
        // Tenant
        // ========================================================

        services.AddScoped<
            ICurrentTenant,
            CurrentTenant>();

        // ========================================================
        // Checkout / Payment orchestration
        // ========================================================

        services.AddScoped<
       CheckoutOrchestrator>();

        services.AddScoped<
            PaymentCompletionOrchestrator>();

        services.AddScoped<
            OrderCancellationOrchestrator>();

        services.AddScoped<
            PaymentEndpoints>();
        services.AddScoped<
    PaymentFailureOrchestrator>();

        services.AddScoped<
            PaymentRetryOrchestrator>();
        services.AddScoped<
    InventoryOrderReconciliationService>();
        return services;
    }

    public static IApplicationBuilder UseEcommerceModules(
        this IApplicationBuilder app)
    {
        using var scope =
            app.ApplicationServices.CreateScope();

        try
        {
            var catalogContext =
                scope.ServiceProvider
                    .GetRequiredService<CatalogDbContext>();

            catalogContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger =
                scope.ServiceProvider
                    .GetService<ILogger<Program>>();

            logger?.LogError(
                ex,
                "Failed to migrate Catalog database");
        }

        return app;
    }
}

