using NexaEcommerce.SharedKernel.Domain;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public class Product : AggregateRoot
{
    // =========================================================
    // Basic Information
    // =========================================================

    public string Name { get; private set; } = null!;

    public string Sku { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? ShortDescription { get; private set; }

    // =========================================================
    // Pricing
    // =========================================================

    public decimal Price { get; private set; }

    public decimal? ComparePrice { get; private set; }

    public decimal DiscountPercentage { get; private set; }

    public string Currency { get; private set; } = "IRR";

    // =========================================================
    // Status
    // =========================================================

    public bool IsActive { get; private set; }

    public bool IsFeatured { get; private set; }

    public bool IsPublished { get; private set; }

    // =========================================================
    // Brand
    // =========================================================

    public Guid? BrandId { get; private set; }

    public Brand? Brand { get; private set; }
    // =========================================================
    // Manufacturer
    // =========================================================

    public Guid? ManufacturerId { get; private set; }

    public Manufacturer? Manufacturer { get; private set; }
    public void SetManufacturer(Guid? manufacturerId)
    {
        ManufacturerId = manufacturerId;
    }
    // =========================================================
    // Relations
    // =========================================================

    public ICollection<ProductCategory> ProductCategories { get; private set; }
        = new List<ProductCategory>();

    public ICollection<ProductImage> Images { get; private set; }
        = new List<ProductImage>();

    public ICollection<ProductReview> Reviews { get; private set; }
        = new List<ProductReview>();

    public ICollection<ProductVariant> Variants { get; private set; }
        = new List<ProductVariant>();

    public ICollection<ProductAttribute> Attributes { get; private set; }
        = new List<ProductAttribute>();

    // =========================================================
    // EF Constructor
    // =========================================================

    private Product()
    {
    }

    // =========================================================
    // Constructor
    // =========================================================

    public Product(
        string name,
        string sku,
        string slug,
        decimal price,
        string currency = "IRR",
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Product name is required.",
                nameof(name));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException(
                "Product SKU is required.",
                nameof(sku));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        Name = name;
        Sku = sku;
        Slug = string.IsNullOrWhiteSpace(slug)
            ? GenerateSlug(name)
            : slug;

        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency)
            ? "IRR"
            : currency;

        Description = description;

        IsActive = true;
        IsFeatured = false;
        IsPublished = true;

        DiscountPercentage = 0;
    }

    // =========================================================
    // Update
    // =========================================================

    public void Update(
        string name,
        string slug,
        string? description,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Product name is required.",
                nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        Name = name;
        Slug = string.IsNullOrWhiteSpace(slug)
            ? GenerateSlug(name)
            : slug;

        Description = description;
        Price = price;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetShortDescription(string? shortDescription)
    {
        ShortDescription = shortDescription;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCurrency(string? currency)
    {
        if (!string.IsNullOrWhiteSpace(currency))
            Currency = currency.Trim().ToUpperInvariant();

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Status
    // =========================================================

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        IsPublished = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Brand
    // =========================================================

    public void SetBrand(Guid? brandId)
    {
        BrandId = brandId;
        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Pricing
    // =========================================================

    public void ApplyDiscount(decimal percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new ArgumentOutOfRangeException(
                nameof(percentage));

        DiscountPercentage = percentage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveDiscount()
    {
        DiscountPercentage = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetFinalPrice()
    {
        if (DiscountPercentage <= 0)
            return Price;

        return Price - (Price * DiscountPercentage / 100m);
    }

    public void SetComparePrice(decimal? comparePrice)
    {
        if (comparePrice.HasValue && comparePrice.Value < 0)
            throw new ArgumentOutOfRangeException(
                nameof(comparePrice));

        ComparePrice = comparePrice;
        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Variants
    // =========================================================

    public ProductVariant AddVariant(
        string sku,
        decimal price,
        decimal? comparePrice = null)
    {
        var variant = new ProductVariant(
            Id,
            sku,
            price,
            0);

        if (comparePrice.HasValue)
            variant.SetComparePrice(comparePrice.Value);

        Variants.Add(variant);

        return variant;
    }

    // =========================================================
    // Images
    // =========================================================

    public ProductImage AddImage(
        string imageUrl,
        int displayOrder,
        bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException(
                "Image URL is required.",
                nameof(imageUrl));

        if (isPrimary)
        {
            foreach (var existingImage in Images)
                existingImage.UnsetPrimary();
        }

        var image = new ProductImage(
            Id,
            imageUrl,
            null,
            displayOrder,
            isPrimary);

        Images.Add(image);

        return image;
    }

    public void SetMainImage(Guid imageId)
    {
        foreach (var image in Images)
            image.UnsetPrimary();

        var selectedImage = Images
            .FirstOrDefault(x => x.Id == imageId);

        if (selectedImage == null)
            throw new InvalidOperationException(
                "Product image was not found.");

        selectedImage.SetPrimary();
    }

    public void UpdateImageOrder(
        Guid imageId,
        int displayOrder)
    {
        var image = Images
            .FirstOrDefault(x => x.Id == imageId);

        if (image == null)
            throw new InvalidOperationException(
                "Product image was not found.");

        image.UpdateOrder(displayOrder);
    }

    // =========================================================
    // Attributes
    // =========================================================

    public ProductAttribute AddAttribute(
        string name,
        string code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Attribute name is required.",
                nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(
                "Attribute code is required.",
                nameof(code));

        var existing = Attributes
            .FirstOrDefault(x =>
                x.Code.Equals(
                    code,
                    StringComparison.OrdinalIgnoreCase));

        if (existing != null)
            return existing;

        var attribute = new ProductAttribute(
            Id,
            name,
            code);

        Attributes.Add(attribute);

        return attribute;
    }

    // =========================================================
    // Slug
    // =========================================================

    private static string GenerateSlug(string name)
    {
        return name
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("?", "")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(".", "-");
    }
}