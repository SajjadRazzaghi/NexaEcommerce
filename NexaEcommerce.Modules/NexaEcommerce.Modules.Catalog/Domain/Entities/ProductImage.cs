using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public sealed class ProductImage : BaseEntity
{
    public Guid ProductId { get; private set; }

    public string ImageUrl { get; private set; } = null!;

    public string? AltText { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    public Product Product { get; private set; } = null!;

    // EF Core
    private ProductImage()
    {
    }

    public ProductImage(
        Guid productId,
        string imageUrl,
        string? altText,
        int displayOrder,
        bool isPrimary)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException(
                "ProductId is required.",
                nameof(productId));

        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException(
                "Image URL is required.",
                nameof(imageUrl));

        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));

        ProductId = productId;
        ImageUrl = imageUrl;
        AltText = altText;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }

    public void Update(
        string imageUrl,
        string? altText,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException(
                "Image URL is required.",
                nameof(imageUrl));

        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));

        ImageUrl = imageUrl;
        AltText = altText;
        DisplayOrder = displayOrder;

        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateOrder(int displayOrder)
    {
        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));

        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrimary()
    {
        IsPrimary = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnsetPrimary()
    {
        IsPrimary = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAltText(string? altText)
    {
        AltText = altText;
        UpdatedAt = DateTime.UtcNow;
    }
}