using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Infrastructure.Repositories;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Infrastructure;

namespace NexaEcommerce.Modules.Orders;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string cannot be null or empty.",
                nameof(connectionString));
        }

        services.AddDbContext<OrdersDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                        sqlOptions
                            .MigrationsHistoryTable(
                                "__EFMigrationsHistory_Orders",
                                "Orders")
                            .EnableRetryOnFailure(
                                5,
                                TimeSpan.FromSeconds(10),
                                null));
            });

        services.AddScoped<
            IOrderRepository,
            OrderRepository>();

        services.AddScoped<
            IOrderService,
            OrderService>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork<OrdersDbContext>>();

        return services;
    }
}