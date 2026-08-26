// NexaEcommerce.Modules.Catalog/CatalogModule.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Catalog.Application.Mappings;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Catalog.Infrastructure.Repositories;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Infrastructure;

namespace NexaEcommerce.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // ثبت DbContext
        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory_Catalog", "Catalog")
                    .EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
        });



        // ثبت Repository‌ها
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ManufacturerRepository>();

        // ثبت Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IManufacturerService,ManufacturerService>();



        // ✅ ثبت UnitOfWork با مشخص کردن نوع DbContext
        services.AddScoped<IUnitOfWork, UnitOfWork<CatalogDbContext>>();
        // ثبت AutoMapper
        services.AddAutoMapper(typeof(ProductProfile).Assembly);
        // ثبت MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CatalogModule).Assembly));

        return services;
    }
}