using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Repositories;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Infrastructure;

namespace NexaEcommerce.Modules.ShoppingCart;

public static class ShoppingCartModule
{
    public static IServiceCollection AddShoppingCartModule(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string cannot be null or empty.",
                nameof(connectionString));
        }

        services.AddDbContext<ShoppingCartDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                        sqlOptions
                            .MigrationsHistoryTable(
                                "__EFMigrationsHistory_ShoppingCart",
                                "ShoppingCart")
                            .EnableRetryOnFailure(
                                5,
                                TimeSpan.FromSeconds(10),
                                null));
            });

        services.AddScoped<ICartRepository, CartRepository>();

        services.AddScoped<ICartService, CartService>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork<ShoppingCartDbContext>>();

        return services;
    }
}