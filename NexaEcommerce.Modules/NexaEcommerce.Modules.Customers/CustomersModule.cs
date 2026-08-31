using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Customers.Application.Services;
using NexaEcommerce.Modules.Customers.Infrastructure.Persistence;
using NexaEcommerce.Modules.Customers.Infrastructure.Repositories;

namespace NexaEcommerce.Modules.Customers;

public static class CustomersModule
{
    public static IServiceCollection AddCustomersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Default' was not found.");
        }

        services.AddDbContext<CustomerDbContext>(
            options =>
                options.UseSqlServer(
                    connectionString));

        services.AddScoped<
            ICustomerAddressRepository,
            CustomerAddressRepository>();

        services.AddScoped<
            ICustomerAddressService,
            CustomerAddressService>();

        services.AddScoped<
            ICustomerUnitOfWork,
            CustomerUnitOfWork>();

        return services;
    }
}