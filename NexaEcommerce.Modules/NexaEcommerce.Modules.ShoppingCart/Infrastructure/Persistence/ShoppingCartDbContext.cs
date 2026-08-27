using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;

namespace NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

public sealed class ShoppingCartDbContext(
    DbContextOptions<ShoppingCartDbContext> options)
    : DbContext(options)
{
    public DbSet<Cart> Carts =>
        Set<Cart>();

    public DbSet<CartItem> CartItems =>
        Set<CartItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);

        modelBuilder.HasDefaultSchema(
            "ShoppingCart");

        modelBuilder.Entity<Cart>(
            entity =>
            {
                entity.ToTable(
                    "Carts");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                        x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                        x => x.UserId)
                    .HasMaxLength(450);

                entity.Property(
                        x => x.GuestToken)
                    .HasMaxLength(128);

                entity.Property(
                        x => x.CreatedAt)
                    .IsRequired();

                entity.Property(
                        x => x.UpdatedAt)
                    .IsRequired();

                entity.HasIndex(
                        x =>
                            new
                            {
                                x.TenantId,
                                x.UserId
                            })
                    .HasDatabaseName(
                        "IX_Carts_Tenant_User");

                entity.HasIndex(
                        x =>
                            new
                            {
                                x.TenantId,
                                x.GuestToken
                            })
                    .HasDatabaseName(
                        "IX_Carts_Tenant_Guest");

                entity.HasMany(
                        x => x.Items)
                    .WithOne(
                        x => x.Cart)
                    .HasForeignKey(
                        x => x.CartId)
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });

        modelBuilder.Entity<CartItem>(
            entity =>
            {
                entity.ToTable(
                    "CartItems");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                        x => x.ProductVariantId)
                    .IsRequired();

                entity.Property(
                        x => x.ProductName)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(
                        x => x.ImageUrl)
                    .HasMaxLength(1000);

                entity.Property(
                        x => x.UnitPrice)
                    .HasPrecision(
                        18,
                        2)
                    .IsRequired();

                entity.Property(
                        x => x.Quantity)
                    .IsRequired();

                entity.HasIndex(
                        x =>
                            new
                            {
                                x.CartId,
                                x.ProductVariantId
                            })
                    .IsUnique()
                    .HasDatabaseName(
                        "UX_CartItems_Cart_Variant");
            });
    }
}