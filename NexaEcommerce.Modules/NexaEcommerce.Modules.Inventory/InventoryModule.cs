using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Inventory.Infrastructure.Repositories;

namespace NexaEcommerce.Modules.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string cannot be null or empty.",
                nameof(connectionString));
        }

        services.AddDbContext<InventoryDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                        sqlOptions
                            .MigrationsHistoryTable(
                                "__EFMigrationsHistory_Inventory",
                                "Inventory")
                            .EnableRetryOnFailure(
                                5,
                                TimeSpan.FromSeconds(10),
                                null));
            });

        services.AddScoped<
            IInventoryRepository,
            InventoryRepository>();

        services.AddScoped<
            IInventoryService,
            InventoryService>();

        services.AddScoped<
            IInventoryUnitOfWork,
            InventoryUnitOfWork>();

        return services;
    }
}