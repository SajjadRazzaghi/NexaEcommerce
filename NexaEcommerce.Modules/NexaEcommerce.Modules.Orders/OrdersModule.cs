using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Orders.Application.Payments;
using NexaEcommerce.Modules.Orders.Application.Pricing;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Infrastructure.Payments;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Infrastructure.Repositories;

namespace NexaEcommerce.Modules.Orders;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new ArgumentException(
                "Connection string cannot be null or empty.",
                nameof(connectionString));
        }

        // ========================================================
        // Database
        // ========================================================

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

        // ========================================================
        // Orders
        // ========================================================

        services.AddScoped<
            IOrderRepository,
            OrderRepository>();

        services.AddScoped<
            IOrderService,
            OrderService>();

        services.AddScoped<
            IOrderUnitOfWork,
            OrderUnitOfWork>();

        // ========================================================
        // Pricing
        // ========================================================

        services.AddScoped<
            IPricingCalculator,
            PricingCalculator>();

        // ========================================================
        // Coupons
        // ========================================================

        services.AddScoped<
            ICouponRepository,
            CouponRepository>();

        services.AddScoped<
            ICouponRedemptionRepository,
            CouponRedemptionRepository>();

        services.AddScoped<
            ICouponService,
            CouponService>();

        // ========================================================
        // Tax Rates
        // ========================================================

        services.AddScoped<
            ITaxRateRepository,
            TaxRateRepository>();

        services.AddScoped<
            ITaxRateService,
            TaxRateService>();

        // ========================================================
        // Payments
        // ========================================================

        services.AddScoped<
            IPaymentAttemptRepository,
            PaymentAttemptRepository>();

        services.AddScoped<
            IPaymentAttemptService,
            PaymentAttemptService>();

        services.AddScoped<
            PaymentGatewayService>();

        services.AddScoped<
            IPaymentService,
            PaymentService>();

        /*
         * ZarinPal is the real payment gateway used by the storefront.
         * TestGateway is kept available for local development.
         */
        services.AddHttpClient<
            ZarinPalPaymentGateway>();

        services.AddScoped<IPaymentGateway>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    ZarinPalPaymentGateway>());

        services.AddScoped<IPaymentGateway>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    TestPaymentGateway>());

        // ========================================================
        // Shipping Methods
        // ========================================================

        services.AddScoped<
            IShippingMethodRepository,
            ShippingMethodRepository>();

        services.AddScoped<
            IShippingMethodService,
            ShippingMethodService>();

        // ========================================================
        // Shipments
        // ========================================================

        services.AddScoped<
            IShipmentRepository,
            ShipmentRepository>();

        services.AddScoped<
            IShipmentService,
            ShipmentService>();

        return services;
    }
}
