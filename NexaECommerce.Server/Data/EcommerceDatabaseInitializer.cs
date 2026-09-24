using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Catalog.Infrastructure.SeedData;
using NexaEcommerce.Modules.Customers.Infrastructure.Persistence;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;
using NexaECommerce.Server.Data.Seed;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Data;

public sealed class EcommerceDatabaseInitializer(
    CatalogDbContext catalogDb,
    CustomerDbContext customerDb,
    InventoryDbContext inventoryDb,
    ShoppingCartDbContext shoppingCartDb,
    OrdersDbContext ordersDb,
    IServiceProvider serviceProvider,
    ILogger<EcommerceDatabaseInitializer> logger)
{
    private const string DefaultTenant =
        TenancyOptions.DefaultTenant;

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        // Platform/AppDbContext is intentionally not migrated here yet.
        // Its historical migration chain was generated from the old SQLite/Sales
        // model and is not safe to execute against the current SQL Server model.
        //
        // IdentitySeeder still uses the current AppDbContext model directly.
        // This is safe as long as the existing Platform tables are already present.
        await IdentitySeeder.SeedAsync(serviceProvider);

        await MigrateAsync(
            "Catalog",
            catalogDb,
            cancellationToken);

        await CatalogSeedAsync();

        await MigrateAsync(
            "Customers",
            customerDb,
            cancellationToken);

        await MigrateAsync(
            "Inventory",
            inventoryDb,
            cancellationToken);

        await MigrateAsync(
            "ShoppingCart",
            shoppingCartDb,
            cancellationToken);

        await MigrateAsync(
            "Orders",
            ordersDb,
            cancellationToken);

        await SeedCommerceDefaultsAsync(
            cancellationToken);

        logger.LogInformation(
            "Ecommerce database initialization completed successfully.");
    }

    private async Task MigrateAsync(
        string databaseName,
        DbContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Applying {DatabaseName} database migrations...",
                databaseName);

            await context.Database.MigrateAsync(
                cancellationToken);

            logger.LogInformation(
                "{DatabaseName} database migrations completed.",
                databaseName);
        }
        catch (Exception ex)
        {
            logger.LogCritical(
                ex,
                "{DatabaseName} database migration failed. Application startup will stop safely.",
                databaseName);

            throw;
        }
    }

    private async Task CatalogSeedAsync()
    {
        await DbInitializer.InitializeAsync(
            catalogDb);
    }

    private async Task SeedCommerceDefaultsAsync(
        CancellationToken cancellationToken)
    {
        var inventoryChanged = false;

        var mainWarehouse =
            await inventoryDb.Warehouses.SingleOrDefaultAsync(
                x =>
                    x.TenantId == DefaultTenant &&
                    x.Code == "MAIN",
                cancellationToken);

        if (mainWarehouse is null)
        {
            mainWarehouse = Warehouse.Create(
                DefaultTenant,
                "MAIN",
                "Main Warehouse",
                isDefault: true);

            await inventoryDb.Warehouses.AddAsync(
                mainWarehouse,
                cancellationToken);

            inventoryChanged = true;

            logger.LogInformation(
                "Created default warehouse {WarehouseCode}.",
                mainWarehouse.Code);
        }

        var mainLocation =
            await inventoryDb.WarehouseLocations.SingleOrDefaultAsync(
                x =>
                    x.TenantId == DefaultTenant &&
                    x.WarehouseId == mainWarehouse.Id &&
                    x.Code == "MAIN",
                cancellationToken);

        if (mainLocation is null)
        {
            mainLocation = WarehouseLocation.Create(
                DefaultTenant,
                mainWarehouse.Id,
                "MAIN",
                "Main Location");

            await inventoryDb.WarehouseLocations.AddAsync(
                mainLocation,
                cancellationToken);

            inventoryChanged = true;

            logger.LogInformation(
                "Created default warehouse location {LocationCode}.",
                mainLocation.Code);
        }

        var hasDefaultWarehouse =
            await inventoryDb.Warehouses.AnyAsync(
                x =>
                    x.TenantId == DefaultTenant &&
                    x.IsDefault &&
                    x.IsActive,
                cancellationToken);

        if (!hasDefaultWarehouse &&
            mainWarehouse.IsActive &&
            !mainWarehouse.IsDefault)
        {
            mainWarehouse.SetDefault(true);

            inventoryChanged = true;

            logger.LogInformation(
                "Marked warehouse {WarehouseCode} as the default warehouse.",
                mainWarehouse.Code);
        }

        if (inventoryChanged)
        {
            await inventoryDb.SaveChangesAsync(
                cancellationToken);
        }

        var standardShipping =
            await ordersDb.ShippingMethods.SingleOrDefaultAsync(
                x =>
                    x.TenantId == DefaultTenant &&
                    x.Code == "STANDARD",
                cancellationToken);

        if (standardShipping is null)
        {
            standardShipping = ShippingMethod.Create(
                DefaultTenant,
                "STANDARD",
                "Standard Shipping",
                "Nexa Delivery",
                0m,
                0);

            await ordersDb.ShippingMethods.AddAsync(
                standardShipping,
                cancellationToken);

            await ordersDb.SaveChangesAsync(
                cancellationToken);

            logger.LogInformation(
                "Created default shipping method {ShippingMethodCode}.",
                standardShipping.Code);
        }
    }
}