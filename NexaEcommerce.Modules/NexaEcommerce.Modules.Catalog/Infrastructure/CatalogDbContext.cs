using Microsoft.EntityFrameworkCore;

using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

namespace NexaEcommerce.Modules.Catalog.Infrastructure;

public sealed class CatalogDbContext : DbContext
{
    // =========================================================
    // Products
    // =========================================================

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants =>
        Set<ProductVariant>();

    public DbSet<ProductImage> ProductImages =>
        Set<ProductImage>();

    public DbSet<ProductReview> ProductReviews =>
        Set<ProductReview>();

    public DbSet<ProductCategory> ProductCategories =>
        Set<ProductCategory>();

    // =========================================================
    // Catalog
    // =========================================================

    public DbSet<Category> Categories =>
        Set<Category>();

    public DbSet<Brand> Brands =>
        Set<Brand>();

    public DbSet<Manufacturer> Manufacturers =>
        Set<Manufacturer>();

    // =========================================================
    // Product Attributes
    // =========================================================

    public DbSet<ProductAttribute> ProductAttributes =>
        Set<ProductAttribute>();

    public DbSet<AttributeValue> AttributeValues =>
        Set<AttributeValue>();

    public DbSet<VariantAttributeValue> VariantAttributeValues =>
        Set<VariantAttributeValue>();

    // =========================================================
    // Catalog Attributes
    // =========================================================

    public DbSet<CatalogAttribute> CatalogAttributes =>
        Set<CatalogAttribute>();

    public DbSet<CatalogAttributeValue> CatalogAttributeValues =>
        Set<CatalogAttributeValue>();

    // =========================================================
    // Constructor
    // =========================================================

    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    // =========================================================
    // Model Configuration
    // =========================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("Catalog");

        // =====================================================
        // Core Catalog
        // =====================================================

        ConfigureProduct(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureBrand(modelBuilder);
        ConfigureManufacturer(modelBuilder);

        // =====================================================
        // Product Relations
        // =====================================================

        ConfigureProductCategory(modelBuilder);

        ConfigureProductVariant(modelBuilder);

        ConfigureProductImage(modelBuilder);

        ConfigureProductReview(modelBuilder);

        // =====================================================
        // Product Attribute System
        // =====================================================

        ConfigureProductAttribute(modelBuilder);

        ConfigureAttributeValue(modelBuilder);

        ConfigureVariantAttributeValue(modelBuilder);

        // =====================================================
        // Catalog Attribute System
        // =====================================================

        ConfigureCatalogAttribute(modelBuilder);

        ConfigureCatalogAttributeValue(modelBuilder);
    }

    // =========================================================
    // Product
    // =========================================================

    private static void ConfigureProduct(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(x => x.Id);

            // -------------------------------------------------
            // Basic
            // -------------------------------------------------

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Sku)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(5000);

            entity.Property(x => x.ShortDescription)
                .HasMaxLength(1000);

            // -------------------------------------------------
            // Price
            // -------------------------------------------------

            entity.Property(x => x.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.ComparePrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.DiscountPercentage)
                .HasPrecision(5, 2);

            entity.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            // -------------------------------------------------
            // Status
            // -------------------------------------------------

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.Property(x => x.IsPublished)
                .IsRequired();

            entity.Property(x => x.IsFeatured)
                .IsRequired();

            // -------------------------------------------------
            // Indexes
            // -------------------------------------------------

            entity.HasIndex(x => x.Sku)
                .IsUnique();

            entity.HasIndex(x => x.Slug)
                .IsUnique();

            entity.HasIndex(x => x.Name);

            entity.HasIndex(x => new
            {
                x.IsActive,
                x.IsPublished
            });

            // -------------------------------------------------
            // Brand
            // -------------------------------------------------

            entity.HasOne(x => x.Brand)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            // -------------------------------------------------
            // Manufacturer
            // -------------------------------------------------

            entity.HasOne(x => x.Manufacturer)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.ManufacturerId)
                .OnDelete(DeleteBehavior.SetNull);

            // -------------------------------------------------
            // Soft Delete
            // -------------------------------------------------

            entity.HasQueryFilter(x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Category
    // =========================================================

    private static void ConfigureCategory(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.ImageUrl)
                .HasMaxLength(1000);

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.HasIndex(x => x.Slug)
                .IsUnique();

            entity.HasIndex(x => x.Name);

            // -------------------------------------------------
            // Parent / Child
            // -------------------------------------------------

            entity.HasOne(x => x.ParentCategory)
                .WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------------------------
            // Soft Delete
            // -------------------------------------------------

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Brand
    // =========================================================

    private static void ConfigureBrand(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brands");

            entity.HasKey(x => x.Id);

            // -------------------------------------------------
            // Basic
            // -------------------------------------------------

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(5000);

            entity.Property(x => x.Website)
                .HasMaxLength(1000);

            // -------------------------------------------------
            // Media
            // -------------------------------------------------

            entity.Property(x => x.LogoUrl)
                .HasMaxLength(1000);

            entity.Property(x => x.CoverImageUrl)
                .HasMaxLength(1000);

            // -------------------------------------------------
            // SEO
            // -------------------------------------------------

            entity.Property(x => x.SeoTitle)
                .HasMaxLength(200);

            entity.Property(x => x.SeoDescription)
                .HasMaxLength(500);

            entity.Property(x => x.SeoKeywords)
                .HasMaxLength(1000);

            // -------------------------------------------------
            // Status
            // -------------------------------------------------

            entity.Property(x => x.DisplayOrder)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.Property(x => x.IsPublished)
                .IsRequired();

            entity.Property(x => x.IsFeatured)
                .IsRequired();

            // -------------------------------------------------
            // Concurrency
            // -------------------------------------------------

            entity.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // -------------------------------------------------
            // Indexes
            // -------------------------------------------------

            entity.HasIndex(x => x.Slug)
                .IsUnique();

            entity.HasIndex(x => x.Name);

            entity.HasIndex(x => new
            {
                x.IsActive,
                x.IsPublished,
                x.IsFeatured
            });

            entity.HasIndex(x => new
            {
                x.DisplayOrder,
                x.Name
            });

            // -------------------------------------------------
            // Product Relationship
            // -------------------------------------------------

            entity.HasMany(x => x.Products)
                .WithOne(x => x.Brand)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            // -------------------------------------------------
            // Soft Delete
            // -------------------------------------------------

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Manufacturer
    // =========================================================

    private static void ConfigureManufacturer(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.ToTable("Manufacturers");

            entity.HasKey(x => x.Id);

            // -------------------------------------------------
            // Basic
            // -------------------------------------------------

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(5000);

            entity.Property(x => x.Website)
                .HasMaxLength(1000);

            // -------------------------------------------------
            // Media
            // -------------------------------------------------

            entity.Property(x => x.LogoUrl)
                .HasMaxLength(1000);

            entity.Property(x => x.CoverImageUrl)
                .HasMaxLength(1000);

            // -------------------------------------------------
            // SEO
            // -------------------------------------------------

            entity.Property(x => x.SeoTitle)
                .HasMaxLength(200);

            entity.Property(x => x.SeoDescription)
                .HasMaxLength(500);

            entity.Property(x => x.SeoKeywords)
                .HasMaxLength(1000);

            // -------------------------------------------------
            // Status
            // -------------------------------------------------

            entity.Property(x => x.DisplayOrder)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.Property(x => x.IsPublished)
                .IsRequired();

            entity.Property(x => x.IsFeatured)
                .IsRequired();

            // -------------------------------------------------
            // Concurrency
            // -------------------------------------------------

            entity.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // -------------------------------------------------
            // Indexes
            // -------------------------------------------------

            entity.HasIndex(x => x.Slug)
                .IsUnique();

            entity.HasIndex(x => x.Name);

            entity.HasIndex(x => new
            {
                x.IsActive,
                x.IsPublished,
                x.IsFeatured
            });

            entity.HasIndex(x => new
            {
                x.DisplayOrder,
                x.Name
            });

            // -------------------------------------------------
            // Product Relationship
            // -------------------------------------------------

            entity.HasMany(x => x.Products)
                .WithOne(x => x.Manufacturer)
                .HasForeignKey(x => x.ManufacturerId)
                .OnDelete(DeleteBehavior.SetNull);

            // -------------------------------------------------
            // Soft Delete
            // -------------------------------------------------

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Product Category
    // =========================================================

    private static void ConfigureProductCategory(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("ProductCategories");

            entity.HasKey(x => new
            {
                x.ProductId,
                x.CategoryId
            });

            entity.HasOne(x => x.Product)
                .WithMany(x => x.ProductCategories)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.ProductCategories)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // =========================================================
    // Product Variant
    // =========================================================

    private static void ConfigureProductVariant(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariants");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Sku)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.PriceOverride)
                .HasPrecision(18, 2);

            entity.Property(x => x.ComparePrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.StockQuantity)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.HasIndex(x => x.Sku)
                .IsUnique();

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Variants)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Product Attribute
    // =========================================================

    private static void ConfigureProductAttribute(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.ToTable("ProductAttributes");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => new
            {
                x.ProductId,
                x.Code
            })
            .IsUnique();

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Attributes)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Attribute Value
    // =========================================================

    private static void ConfigureAttributeValue(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttributeValue>(entity =>
        {
            entity.ToTable("AttributeValues");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Value)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.DisplayValue)
                .HasMaxLength(200);

            entity.Property(x => x.ColorHex)
                .HasMaxLength(20);

            entity.HasIndex(x => new
            {
                x.ProductAttributeId,
                x.Value
            })
            .IsUnique();

            entity.HasOne(x => x.ProductAttribute)
                .WithMany(x => x.Values)
                .HasForeignKey(x => x.ProductAttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Variant Attribute Value
    // =========================================================

    private static void ConfigureVariantAttributeValue(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VariantAttributeValue>(entity =>
        {
            entity.ToTable("VariantAttributeValues");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.ProductVariantId,
                x.AttributeValueId
            })
            .IsUnique();

            entity.HasOne(x => x.ProductVariant)
                .WithMany(x => x.AttributeValues)
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AttributeValue)
                .WithMany()
                .HasForeignKey(x => x.AttributeValueId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Catalog Attribute
    // =========================================================

    private static void ConfigureCatalogAttribute(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogAttribute>(entity =>
        {
            entity.ToTable("CatalogAttributes");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.HasIndex(x => x.Name);

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Catalog Attribute Value
    // =========================================================

    private static void ConfigureCatalogAttributeValue(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogAttributeValue>(entity =>
        {
            entity.ToTable("CatalogAttributeValues");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Value)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.DisplayValue)
                .HasMaxLength(200);

            entity.HasIndex(x => new
            {
                x.CatalogAttributeId,
                x.Value
            })
            .IsUnique();

            entity.HasOne(x => x.CatalogAttribute)
                .WithMany(x => x.Values)
                .HasForeignKey(x => x.CatalogAttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Product Image
    // =========================================================

    private static void ConfigureProductImage(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ImageUrl)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.AltText)
                .HasMaxLength(300);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }

    // =========================================================
    // Product Review
    // =========================================================

    private static void ConfigureProductReview(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.ToTable("ProductReviews");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(200);

            entity.Property(x => x.Comment)
                .HasMaxLength(2000);

            entity.Property(x => x.Rating)
                .IsRequired();

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(
                x => !x.IsDeleted);
        });
    }
}