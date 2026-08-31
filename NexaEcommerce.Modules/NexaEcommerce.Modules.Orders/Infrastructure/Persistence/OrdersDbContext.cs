
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

    public DbSet<OrderInventoryReservation>
        OrderInventoryReservations =>
        Set<OrderInventoryReservation>();

    public DbSet<PaymentAttempt>
        PaymentAttempts =>
        Set<PaymentAttempt>();

    public DbSet<Shipment>
        Shipments =>
        Set<Shipment>();
    public DbSet<ShippingMethod>
    ShippingMethods =>
    Set<ShippingMethod>();
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);

        modelBuilder.HasDefaultSchema(
            "Orders");

        modelBuilder.Entity<Order>(
            entity =>
            {
                entity.ToTable(
                    "Orders");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(
                    x => x.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.IdempotencyKey)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(
                    x => x.Status)
                    .IsRequired();

                entity.Property(
                    x => x.Currency)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(
                    x => x.Subtotal)
                    .HasPrecision(18, 2);

                entity.Property(
                    x => x.ShippingAmount)
                    .HasPrecision(18, 2);

                entity.Property(
                    x => x.DiscountAmount)
                    .HasPrecision(18, 2);

                entity.Property(
                    x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(
                    x => x.ShippingFullName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(
                    x => x.ShippingPhone)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(
                    x => x.ShippingAddress)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(
                    x => x.ShippingCity)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(
                    x => x.ShippingPostalCode)
                    .HasMaxLength(30);

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.OrderNumber
                    })
                    .IsUnique();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.UserId,
                        x.IdempotencyKey
                    })
                    .IsUnique();

                entity.HasMany(
                    x => x.Items)
                    .WithOne(
                        x => x.Order)
                    .HasForeignKey(
                        x => x.OrderId)
                    .OnDelete(
                        DeleteBehavior.Cascade);

                entity.HasMany(
                    x => x.InventoryReservations)
                    .WithOne()
                    .HasForeignKey(
                        x => x.OrderId)
                    .OnDelete(
                        DeleteBehavior.Cascade);
               
modelBuilder.Entity<ShippingMethod>(
    entity =>
    {
        entity.ToTable(
            "ShippingMethods");

        entity.HasKey(
            x => x.Id);

        entity.Property(
            x => x.TenantId)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(
            x => x.Code)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(
            x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(
            x => x.Carrier)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(
            x => x.Price)
            .HasPrecision(
                18,
                2)
            .IsRequired();

        entity.Property(
            x => x.SortOrder)
            .IsRequired();

        entity.Property(
            x => x.IsActive)
            .IsRequired();

        entity.Property(
            x => x.CreatedAt)
            .IsRequired();

        entity.HasIndex(
                x => new
                {
                    x.TenantId,
                    x.Code
                })
            .IsUnique();

        entity.HasIndex(
            x => new
            {
                x.TenantId,
                x.IsActive,
                x.SortOrder
            });
    });
            });

        modelBuilder.Entity<OrderItem>(
            entity =>
            {
                entity.ToTable(
                    "OrderItems");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.Sku)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(
                    x => x.ProductName)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(
                    x => x.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(
                    x => x.Quantity)
                    .IsRequired();
            });

        modelBuilder.Entity<OrderInventoryReservation>(
            entity =>
            {
                entity.ToTable(
                    "OrderInventoryReservations");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.ReservationKey)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(
                    x => x.ProductVariantId)
                    .IsRequired();

                entity.Property(
                    x => x.Quantity)
                    .IsRequired();

                entity.Property(
                    x => x.Status)
                    .IsRequired();

                entity.Property(
                    x => x.ExpiresAt)
                    .IsRequired();

                entity.Property(
                    x => x.CreatedAt)
                    .IsRequired();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.ReservationKey
                    })
                    .IsUnique();

                entity.HasIndex(
                    x => new
                    {
                        x.OrderId,
                        x.ProductVariantId
                    });

                entity.HasIndex(
                    x => new
                    {
                        x.Status,
                        x.ExpiresAt
                    });
            });

        modelBuilder.Entity<PaymentAttempt>(
            entity =>
            {
                entity.ToTable(
                    "PaymentAttempts");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.OrderId)
                    .IsRequired();

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(
                    x => x.IdempotencyKey)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(
                    x => x.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(
                    x => x.Currency)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(
                    x => x.Status)
                    .IsRequired();

                entity.Property(
                    x => x.GatewayName)
                    .HasMaxLength(100);

                entity.Property(
                    x => x.GatewayReference)
                    .HasMaxLength(200);

                entity.Property(
                    x => x.FailureCode)
                    .HasMaxLength(100);

                entity.Property(
                    x => x.FailureMessage)
                    .HasMaxLength(1000);

                entity.Property(
                    x => x.CreatedAt)
                    .IsRequired();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.UserId,
                        x.IdempotencyKey
                    })
                    .IsUnique();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.OrderId
                    });
            });

        modelBuilder.Entity<Shipment>(
            entity =>
            {
                entity.ToTable(
                    "Shipments");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.OrderId)
                    .IsRequired();

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.ShippingMethod)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(
                    x => x.Carrier)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(
                    x => x.TrackingNumber)
                    .HasMaxLength(200);

                entity.Property(
                    x => x.Status)
                    .IsRequired();

                entity.Property(
                    x => x.CreatedAt)
                    .IsRequired();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.OrderId
                    })
                    .IsUnique();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.TrackingNumber
                    })
                    .IsUnique()
                    .HasFilter(
                        "[TrackingNumber] IS NOT NULL");
            });
    }
}
