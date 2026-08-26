using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public sealed class Brand : AggregateRoot
{
    // =========================================================
    // Basic Information
    // =========================================================

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? Website { get; private set; }

    // =========================================================
    // Media
    // =========================================================

    public string? LogoUrl { get; private set; }

    public string? CoverImageUrl { get; private set; }

    // =========================================================
    // SEO
    // =========================================================

    public string? SeoTitle { get; private set; }

    public string? SeoDescription { get; private set; }

    public string? SeoKeywords { get; private set; }

    // =========================================================
    // Display / Status
    // =========================================================

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPublished { get; private set; }

    public bool IsFeatured { get; private set; }

    // =========================================================
    // Concurrency
    // =========================================================

    public byte[]? RowVersion { get; private set; }

    // =========================================================
    // Navigation
    // =========================================================

    public ICollection<Product> Products { get; private set; }
        = new List<Product>();

    // =========================================================
    // EF Constructor
    // =========================================================

    private Brand()
    {
    }

    // =========================================================
    // Constructor
    // =========================================================

    public Brand(
        string name,
        string? description = null,
        string? website = null)
    {
        ValidateName(name);

        Name = Normalize(name);

        Slug = GenerateSlug(Name);

        Description = NormalizeNullable(description);

        Website = NormalizeNullable(website);

        DisplayOrder = 0;

        IsActive = true;

        IsPublished = false;

        IsFeatured = false;
    }

    // =========================================================
    // Factory
    // =========================================================

    public static Brand Create(
        string name,
        string? description = null,
        string? website = null)
    {
        return new Brand(
            name,
            description,
            website);
    }

    // =========================================================
    // Name
    // =========================================================

    public void Rename(string name)
    {
        ValidateName(name);

        Name = Normalize(name);

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Slug
    // =========================================================

    public void ChangeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException(
                "Slug is required.",
                nameof(slug));

        Slug = GenerateSlug(slug);

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSlug(string slug)
    {
        ChangeSlug(slug);
    }

    // =========================================================
    // Description
    // =========================================================

    public void ChangeDescription(
        string? description)
    {
        Description = NormalizeNullable(description);

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDescription(
        string? description)
    {
        ChangeDescription(description);
    }

    // =========================================================
    // Website
    // =========================================================

    public void ChangeWebsite(
        string? website)
    {
        Website = NormalizeNullable(website);

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Logo
    // =========================================================

    public void ChangeLogo(
        string? logoUrl)
    {
        LogoUrl = NormalizeNullable(logoUrl);

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLogo(
        string? logoUrl)
    {
        ChangeLogo(logoUrl);
    }

    // =========================================================
    // Cover
    // =========================================================

    public void ChangeCover(
        string? coverImageUrl)
    {
        CoverImageUrl =
            NormalizeNullable(coverImageUrl);

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // SEO
    // =========================================================

    public void ChangeSeo(
        string? title,
        string? description,
        string? keywords)
    {
        SeoTitle = NormalizeNullable(title);

        SeoDescription =
            NormalizeNullable(description);

        SeoKeywords =
            NormalizeNullable(keywords);

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Display Order
    // =========================================================

    public void ChangeDisplayOrder(
        int displayOrder)
    {
        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));

        DisplayOrder = displayOrder;

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Active
    // =========================================================

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;

        IsPublished = false;

        IsFeatured = false;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(
        bool isActive)
    {
        if (isActive)
            Activate();
        else
            Deactivate();
    }

    // =========================================================
    // Published
    // =========================================================

    public void Publish()
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "An inactive brand cannot be published.");

        if (IsPublished)
            return;

        IsPublished = true;

        UpdatedAt = DateTime.UtcNow;
    }

    public void UnPublish()
    {
        if (!IsPublished)
            return;

        IsPublished = false;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        UnPublish();
    }

    // =========================================================
    // Featured
    // =========================================================

    public void Feature()
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "An inactive brand cannot be featured.");

        if (IsFeatured)
            return;

        IsFeatured = true;

        UpdatedAt = DateTime.UtcNow;
    }

    public void UnFeature()
    {
        if (!IsFeatured)
            return;

        IsFeatured = false;

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Update
    // =========================================================

    public void Update(
        string name,
        string? slug,
        string? logo,
        string? cover,
        string? website,
        string? description)
    {
        Rename(name);

        ChangeSlug(
            string.IsNullOrWhiteSpace(slug)
                ? GenerateSlug(name)
                : slug);

        ChangeLogo(logo);

        ChangeCover(cover);

        ChangeWebsite(website);

        ChangeDescription(description);

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Soft Delete
    // =========================================================

    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;

        IsActive = false;

        IsPublished = false;

        IsFeatured = false;

        DeletedAt = DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;

        DeletedAt = null;

        IsActive = true;

        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================
    // Validation
    // =========================================================

    private static void ValidateName(
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Brand name is required.",
                nameof(name));
    }

    // =========================================================
    // Helpers
    // =========================================================

    private static string Normalize(
        string value)
    {
        return value.Trim();
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string GenerateSlug(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Slug source cannot be empty.",
                nameof(value));

        var slug = value
            .Trim()
            .ToLowerInvariant();

        var chars = slug
            .Select(character =>
            {
                if (char.IsLetterOrDigit(character))
                    return character;

                return '-';
            })
            .ToArray();

        slug = new string(chars);

        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        return slug.Trim('-');
    }
}