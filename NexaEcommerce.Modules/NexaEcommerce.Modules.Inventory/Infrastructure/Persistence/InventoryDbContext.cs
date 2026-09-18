using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Domain.Entities;

namespace NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Warehouse> Warehouses =>
    Set<Warehouse>();
    public DbSet<WarehouseTransfer> WarehouseTransfers => Set<WarehouseTransfer>();
    public DbSet<WarehouseStockMovement> WarehouseStockMovements => Set<WarehouseStockMovement>();
    public DbSet<WarehouseLocation> WarehouseLocations =>
        Set<WarehouseLocation>();

    public DbSet<WarehouseStock> WarehouseStocks =>
        Set<WarehouseStock>();
    public DbSet<StockItem> StockItems =>
        Set<StockItem>();

    public DbSet<StockReservation> StockReservations =>
        Set<StockReservation>();

    public DbSet<InventoryMovement> InventoryMovements =>
        Set<InventoryMovement>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);
        modelBuilder.Entity<WarehouseTransfer>(entity => { entity.ToTable("WarehouseTransfers"); 
            entity.HasKey(x => x.Id); 
            entity.Property(x => x.TenantId).IsRequired().HasMaxLength(64); 
            entity.Property(x => x.SourceWarehouseId).IsRequired(); 
            entity.Property(x => x.SourceLocationId).IsRequired();
            entity.Property(x => x.DestinationWarehouseId).IsRequired();
            entity.Property(x => x.DestinationLocationId).IsRequired(); 
            entity.Property(x => x.ProductVariantId).IsRequired(); 
            entity.Property(x => x.Quantity).IsRequired(); 
            entity.Property(x => x.Status).IsRequired(); 
            entity.Property(x => x.Reason).HasMaxLength(512);
            entity.Property(x => x.RequestedAt).IsRequired();
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.SourceLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.DestinationLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.TenantId, x.RequestedAt }); entity.HasIndex(x => new { x.TenantId, x.ProductVariantId, x.RequestedAt }); });
        modelBuilder.Entity<WarehouseStockMovement>(entity => { entity.ToTable("WarehouseStockMovements");
            entity.HasKey(x => x.Id); entity.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(x => x.WarehouseId).IsRequired(); entity.Property(x => x.LocationId).IsRequired(); entity.Property(x => x.ProductVariantId).IsRequired();
            entity.Property(x => x.Type).IsRequired(); entity.Property(x => x.QuantityDelta).IsRequired(); 
            entity.Property(x => x.BalanceAfter).IsRequired();
            entity.Property(x => x.ReferenceType).HasMaxLength(64);
            entity.Property(x => x.ReferenceId).HasMaxLength(128);
            entity.Property(x => x.Reason).HasMaxLength(512); 
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.TenantId, x.WarehouseId, x.LocationId, x.ProductVariantId, x.OccurredAt });
            entity.HasIndex(x => new { x.TenantId, x.ReferenceType, x.ReferenceId }); });

        modelBuilder.Entity<Warehouse>(
    entity =>
    {
        entity.ToTable(
            "Warehouses");

        entity.HasKey(
            x => x.Id);

        entity.Property(
            x => x.TenantId)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(
            x => x.Code)
            .IsRequired()
            .HasMaxLength(32);

        entity.Property(
            x => x.Name)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(
            x => x.AddressLine)
            .HasMaxLength(512);

        entity.Property(
            x => x.City)
            .HasMaxLength(128);

        entity.Property(
            x => x.PostalCode)
            .HasMaxLength(32);

        entity.Property(
            x => x.Phone)
            .HasMaxLength(64);

        entity.Property(
            x => x.IsDefault)
            .IsRequired();

        entity.Property(
            x => x.IsActive)
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
                x.IsDefault
            });
    });

        modelBuilder.Entity<WarehouseLocation>(
            entity =>
            {
                entity.ToTable(
                    "WarehouseLocations");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.WarehouseId)
                    .IsRequired();

                entity.Property(
                    x => x.Code)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.Name)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(
                    x => x.Zone)
                    .HasMaxLength(64);

                entity.Property(
                    x => x.Rack)
                    .HasMaxLength(64);

                entity.Property(
                    x => x.Shelf)
                    .HasMaxLength(64);

                entity.Property(
                    x => x.Bin)
                    .HasMaxLength(64);

                entity.Property(
                    x => x.IsActive)
                    .IsRequired();

                entity.HasOne<Warehouse>()
                    .WithMany()
                    .HasForeignKey(
                        x => x.WarehouseId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.WarehouseId,
                        x.Code
                    })
                    .IsUnique();
            });

        modelBuilder.Entity<WarehouseStock>(
            entity =>
            {
                entity.ToTable(
                    "WarehouseStocks");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.WarehouseId)
                    .IsRequired();

                entity.Property(
                    x => x.LocationId)
                    .IsRequired();

                entity.Property(
                    x => x.ProductVariantId)
                    .IsRequired();

                entity.Property(
                    x => x.OnHandQuantity)
                    .IsRequired();

                entity.Property(
                    x => x.ReservedQuantity)
                    .IsRequired();

                entity.Property(
                    x => x.IncomingQuantity)
                    .IsRequired();

                entity.Property(
                    x => x.DamagedQuantity)
                    .IsRequired();

                entity.Property(
                    x => x.ReorderPoint)
                    .IsRequired();

                entity.Property(
                    x => x.Version)
                    .IsRequired()
                    .IsConcurrencyToken();

                entity.Ignore(
                    x => x.AvailableQuantity);

                entity.Ignore(
                    x => x.IsLowStock);

                entity.HasOne<Warehouse>()
                    .WithMany()
                    .HasForeignKey(
                        x => x.WarehouseId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasOne<WarehouseLocation>()
                    .WithMany()
                    .HasForeignKey(
                        x => x.LocationId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.WarehouseId,
                        x.LocationId,
                        x.ProductVariantId
                    })
                    .IsUnique();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.ProductVariantId
                    });
            });
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

        modelBuilder.Entity<InventoryMovement>(
            entity =>
            {
                entity.ToTable(
                    "InventoryMovements");

                entity.HasKey(
                    x => x.Id);

                entity.Property(
                    x => x.TenantId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(
                    x => x.StockItemId)
                    .IsRequired();

                entity.Property(
                    x => x.ProductVariantId)
                    .IsRequired();

                entity.Property(
                    x => x.Type)
                    .IsRequired();

                entity.Property(
                    x => x.AvailableDelta)
                    .IsRequired();

                entity.Property(
                    x => x.ReservedDelta)
                    .IsRequired();

                entity.Property(
                    x => x.AvailableBalance)
                    .IsRequired();

                entity.Property(
                    x => x.ReservedBalance)
                    .IsRequired();

                entity.Ignore(
                    x => x.TotalBalance);

                entity.Property(
                    x => x.ReferenceType)
                    .HasMaxLength(64);

                entity.Property(
                    x => x.ReferenceId)
                    .HasMaxLength(128);

                entity.Property(
                    x => x.Reason)
                    .HasMaxLength(500);

                entity.Property(
                    x => x.OccurredAt)
                    .IsRequired();

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.ProductVariantId,
                        x.OccurredAt
                    });

                entity.HasIndex(
                    x => new
                    {
                        x.TenantId,
                        x.ReferenceType,
                        x.ReferenceId
                    });

                entity.HasOne<StockItem>()
                    .WithMany()
                    .HasForeignKey(
                        x => x.StockItemId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });
    }
}
