using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public sealed class ProductVariantImage : BaseEntity
{
    public Guid ProductVariantId { get; private set; }

    public string ImageUrl { get; private set; } = null!;

    public string? AltText { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    public ProductVariant ProductVariant { get; private set; } = null!;

    private ProductVariantImage()
    {
    }

    public ProductVariantImage(
        Guid productVariantId,
        string imageUrl,
        string? altText = null,
        int displayOrder = 0,
        bool isPrimary = false)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException(
                "Image URL is required.",
                nameof(imageUrl));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));
        }

        ProductVariantId =
            productVariantId;

        ImageUrl =
            imageUrl.Trim();

        AltText =
            string.IsNullOrWhiteSpace(altText)
                ? null
                : altText.Trim();

        DisplayOrder =
            displayOrder;

        IsPrimary =
            isPrimary;
    }

    public void Update(
        string imageUrl,
        string? altText,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException(
                "Image URL is required.",
                nameof(imageUrl));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));
        }

        ImageUrl =
            imageUrl.Trim();

        AltText =
            string.IsNullOrWhiteSpace(altText)
                ? null
                : altText.Trim();

        DisplayOrder =
            displayOrder;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void SetPrimary(
        bool isPrimary)
    {
        IsPrimary =
            isPrimary;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void SetDisplayOrder(
        int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));
        }

        DisplayOrder =
            displayOrder;

        UpdatedAt =
            DateTime.UtcNow;
    }
}