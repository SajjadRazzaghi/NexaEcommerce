using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(
    DbContextOptions<OrdersDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders =>
        Set<Order>();

    public DbSet<OrderItem> OrderItems =>
        Set<OrderItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("Orders");

        modelBuilder.Entity<Order>(
            entity =>
            {
                entity.ToTable("Orders");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(x => x.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(x => x.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(x => x.Subtotal)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ShippingAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DiscountAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ShippingFullName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.ShippingPhone)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.ShippingAddress)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(x => x.ShippingCity)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.ShippingPostalCode)
                    .HasMaxLength(30);

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.OrderNumber
                    })
                    .IsUnique();

                entity.HasMany(x => x.Items)
                    .WithOne(x => x.Order)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder.Entity<OrderItem>(
            entity =>
            {
                entity.ToTable("OrderItems");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Sku)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.ProductName)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(x => x.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Quantity)
                    .IsRequired();
            });
    }
}