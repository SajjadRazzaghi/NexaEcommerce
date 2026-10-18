using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Data;

public sealed class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(
                    "Server=.;Database=NexaECommerceDB;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;" +
                    "MultipleActiveResultSets=true")
                .Options;

        return new AppDbContext(
            options,
            new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext
        : ITenantContext
    {
        public string TenantId => "default";

        public bool IsMultiTenant => false;
    }
}