using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Customers.Domain.Entities;

namespace NexaEcommerce.Modules.Customers.Infrastructure.Persistence;

public sealed class CustomerDbContext(
    DbContextOptions<CustomerDbContext> options)
    : DbContext(options)
{
    public DbSet<CustomerAddress> CustomerAddresses =>
        Set<CustomerAddress>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);

        modelBuilder.Entity<CustomerAddress>(
            entity =>
            {
                entity.ToTable(
                    "CustomerAddresses");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                        x => x.TenantId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(
                        x => x.UserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(
                        x => x.Title)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(
                        x => x.RecipientName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(
                        x => x.PhoneNumber)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(
                        x => x.Country)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(
                        x => x.Province)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(
                        x => x.City)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(
                        x => x.AddressLine)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(
                        x => x.PostalCode)
                    .HasMaxLength(30);

                entity.HasIndex(
                    x =>
                        new
                        {
                            x.TenantId,
                            x.UserId
                        });
                entity.HasIndex(x => new { x.TenantId, x.UserId, x.IsDefault })
                .HasDatabaseName("IX_CustomerAddresses_Default")
                .HasFilter("[IsDefault] = 1").IsUnique();
            });
    }
}
