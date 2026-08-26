using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public sealed class ProductReview : BaseEntity
{
    public Guid ProductId { get; private set; }

    public Guid? UserId { get; private set; }

    public string? Title { get; private set; }

    public string? Comment { get; private set; }

    public int Rating { get; private set; }

    public bool IsApproved { get; private set; }

    public Product Product { get; private set; } = null!;

    // EF Core
    private ProductReview()
    {
    }

    public ProductReview(
        Guid productId,
        int rating,
        string? title = null,
        string? comment = null,
        Guid? userId = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException(
                "ProductId is required.",
                nameof(productId));

        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(
                nameof(rating),
                "Rating must be between 1 and 5.");

        ProductId = productId;
        Rating = rating;
        Title = title;
        Comment = comment;
        UserId = userId;
        IsApproved = false;
    }

    public void Update(
        int rating,
        string? title,
        string? comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(
                nameof(rating),
                "Rating must be between 1 and 5.");

        Rating = rating;
        Title = title;
        Comment = comment;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        IsApproved = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        IsApproved = false;
        UpdatedAt = DateTime.UtcNow;
    }
}