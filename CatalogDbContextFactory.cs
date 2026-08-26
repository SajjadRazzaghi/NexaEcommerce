using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NexaEcommerce.Modules.Catalog.Infrastructure;

public sealed class CatalogDbContextFactory
    : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(
                "appsettings.json",
                optional: true)
            .AddJsonFile(
                "appsettings.Development.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString(
                "CatalogDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
             connectionString = configuration.GetConnectionString("Default");

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Catalog database connection string was not found. " +
                "Configure ConnectionStrings:CatalogDb " +
                "or ConnectionStrings:DefaultConnection.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<CatalogDbContext>();

        optionsBuilder.UseSqlServer(
            connectionString,
            sql =>
            {
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });

        return new CatalogDbContext(
            optionsBuilder.Options);
    }
}