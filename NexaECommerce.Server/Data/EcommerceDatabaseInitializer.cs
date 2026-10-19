
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Catalog.Infrastructure.SeedData;
using NexaEcommerce.Modules.Customers.Infrastructure.Persistence;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

namespace NexaECommerce.Server.Data;

public sealed class EcommerceDatabaseInitializer(
    AppDbContext appDb,
    CatalogDbContext catalogDb,
    CustomerDbContext customerDb,
    InventoryDbContext inventoryDb,
    ShoppingCartDbContext shoppingCartDb,
    OrdersDbContext ordersDb,
    ILogger<EcommerceDatabaseInitializer> logger)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await MigrateAsync(
            "App/Identity",
            appDb,
            cancellationToken);

        await MigrateAsync(
            "Catalog",
            catalogDb,
            cancellationToken);

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

        await DbInitializer.InitializeDisplayTypesAsync(
            catalogDb);

        logger.LogInformation(
            "Database migrations completed successfully.");
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
            ////------serverbased changes
            ///
            //await context.Database.MigrateAsync(
            //    cancellationToken);

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
}
