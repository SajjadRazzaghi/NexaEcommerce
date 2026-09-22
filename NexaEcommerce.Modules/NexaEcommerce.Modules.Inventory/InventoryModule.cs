using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Inventory.Infrastructure.Repositories;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaEcommerce.Modules.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(
         this IServiceCollection services,
         string connectionString)
    {
        services.AddDbContext<InventoryDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sql =>
                    {
                        sql.MigrationsHistoryTable(
                            "__EFMigrationsHistory",
                            "Inventory");
                    });
            });

        services.AddScoped<
            IInventoryUnitOfWork,
            InventoryUnitOfWork>();

        services.AddScoped<
            IInventoryRepository,
            InventoryRepository>();

        services.AddScoped<
            IWarehouseRepository,
            WarehouseRepository>();

        services.AddScoped<
            IWarehouseStockRepository,
            WarehouseStockRepository>();

        services.AddScoped<
            IWarehouseStockReservationRepository,
            WarehouseStockReservationRepository>();

        services.AddScoped<
            IInventoryService,
            InventoryService>();

        services.AddScoped<
            IInventoryReportService,
            InventoryReportService>();

        services.AddScoped<
            IWarehouseService,
            WarehouseService>();

        services.AddScoped<
            IWarehouseStockService,
            WarehouseStockService>();

        services.AddScoped<
            IWarehouseTransferService,
            WarehouseTransferService>();

        services.AddScoped<
            IInventoryStockReader,
            InventoryStockReader>();

        services.AddScoped<
            IStockReader,
            WarehouseStockReader>();

        return services;
    }
}