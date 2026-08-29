using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Domain.Entities;

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<StockItem> StockItems =>
        Set<StockItem>();

    public DbSet<StockReservation> StockReservations =>
        Set<StockReservation>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);

        modelBuilder.HasDefaultSchema(
            "Inventory");

        modelBuilder.Entity<StockItem>(
            entity =>
            {
                entity.ToTable(
                    "StockItems");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.ProductVariantId)
                    .IsRequired();

                entity.Property(
                    x => x.AvailableQuantity)
                    .IsRequired();

                entity.Property(
                    x => x.ReservedQuantity)
                    .IsRequired();

                entity.Property(
                    x => x.Version)
                    .IsRequired()
                    .IsConcurrencyToken();

                entity.Ignore(
                    x => x.TotalQuantity);

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.ProductVariantId
                    })
                    .IsUnique();
            });

        modelBuilder.Entity<StockReservation>(
            entity =>
            {
                entity.ToTable(
                    "StockReservations");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.ReservationKey)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(
                    x => x.ProductVariantId)
                    .IsRequired();

                entity.Property(
                    x => x.StockItemId)
                    .IsRequired();

                entity.Property(
                    x => x.Quantity)
                    .IsRequired();

                entity.Property(
                    x => x.ExpiresAt)
                    .IsRequired();

                entity.Property(
                    x => x.Status)
                    .IsRequired();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.ReservationKey
                    })
                    .IsUnique();

                entity.HasOne(
                    x => x.StockItem)
                    .WithMany()
                    .HasForeignKey(
                        x => x.StockItemId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });
    }
}