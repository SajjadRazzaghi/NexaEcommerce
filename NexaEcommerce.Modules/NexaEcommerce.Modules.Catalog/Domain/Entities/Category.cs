using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public class Category : AggregateRoot
{
    // =========================================================
    // Basic Information
    // =========================================================

    public string Name { get; private set; } = null!;

    public string? Slug { get; private set; }

    public string? Description { get; private set; }

    public string? ImageUrl { get; private set; }

    // =========================================================
    // SEO
    // =========================================================

    public string? SeoTitle { get; private set; }

    public string? SeoDescription { get; private set; }

    public string? SeoKeywords { get; private set; }

    // =========================================================
    // Category Hierarchy
    // =========================================================

    public Guid? ParentCategoryId { get; private set; }

    // =========================================================
    // Display / Status
    // =========================================================

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPublished { get; private set; }

    public bool IsFeatured { get; private set; }

    // =========================================================
    // Navigation Properties
    // =========================================================

    public Category? ParentCategory { get; private set; }

    public ICollection<Category> SubCategories { get; private set; }
        = new List<Category>();

    public ICollection<ProductCategory> ProductCategories { get; private set; }
        = new List<ProductCategory>();

    // =========================================================
    // EF Constructor
    // =========================================================

    private Category()
    {
    }

    // =========================================================
    // Constructor
    // =========================================================

    public Category(
        string name,
        string? slug = null,
        string? description = null)
    {
        ValidateName(name);

        Name =
            name.Trim();

        Slug =
            string.IsNullOrWhiteSpace(slug)
                ? GenerateSlug(Name)
                : GenerateSlug(slug);

        Description =
            NormalizeNullable(description);

        DisplayOrder = 0;

        IsActive = true;

        IsPublished = false;

        IsFeatured = false;
    }

    // =========================================================
    // Update
    // =========================================================

    public void Update(
        string name,
        string? slug,
        string? description,
        string? imageUrl,
        string? seoTitle,
        string? seoDescription,
        string? seoKeywords,
        bool isActive)
    {
        ValidateName(name);

        Name =
            name.Trim();

        Slug =
            string.IsNullOrWhiteSpace(slug)
                ? GenerateSlug(Name)
                : GenerateSlug(slug);

        Description =
            NormalizeNullable(description);

        ImageUrl =
            NormalizeNullable(imageUrl);

        ChangeSeo(
            seoTitle,
            seoDescription,
            seoKeywords);

        IsActive =
            isActive;

        // An inactive category cannot remain
        // published or featured.
        if (!IsActive)
        {
            IsPublished = false;
            IsFeatured = false;
        }

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // Slug
    // =========================================================

    public void ChangeSlug(
        string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException(
                "Slug is required.",
                nameof(slug));
        }

        Slug =
            GenerateSlug(slug);

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // Description
    // =========================================================

    public void ChangeDescription(
        string? description)
    {
        Description =
            NormalizeNullable(description);

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // Image
    // =========================================================

    public void SetImage(
        string? imageUrl)
    {
        ImageUrl =
            NormalizeNullable(imageUrl);

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // SEO
    // =========================================================

    public void ChangeSeo(
        string? title,
        string? description,
        string? keywords)
    {
        var normalizedTitle =
            NormalizeNullable(title);

        var normalizedDescription =
            NormalizeNullable(description);

        var normalizedKeywords =
            NormalizeNullable(keywords);

        // -----------------------------------------------------
        // SEO Title
        // -----------------------------------------------------

        if (normalizedTitle is not null &&
            normalizedTitle.Length > 200)
        {
            throw new ArgumentException(
                "SEO title cannot exceed 200 characters.",
                nameof(title));
        }

        // -----------------------------------------------------
        // SEO Description
        // -----------------------------------------------------

        if (normalizedDescription is not null &&
            normalizedDescription.Length > 500)
        {
            throw new ArgumentException(
                "SEO description cannot exceed 500 characters.",
                nameof(description));
        }

        // -----------------------------------------------------
        // SEO Keywords
        // -----------------------------------------------------

        if (normalizedKeywords is not null &&
            normalizedKeywords.Length > 1000)
        {
            throw new ArgumentException(
                "SEO keywords cannot exceed 1000 characters.",
                nameof(keywords));
        }

        SeoTitle =
            normalizedTitle;

        SeoDescription =
            normalizedDescription;

        SeoKeywords =
            normalizedKeywords;

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // Parent Category
    // =========================================================

    public void SetParentCategory(
        Category? parentCategory)
    {
        // Prevent direct self-parenting.
        if (parentCategory is not null &&
            parentCategory.Id == Id)
        {
            throw new InvalidOperationException(
                "A category cannot be its own parent.");
        }

        ParentCategory =
            parentCategory;

        ParentCategoryId =
            parentCategory?.Id;

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // Sub Categories
    // =========================================================

    public void AddSubCategory(
        Category subCategory)
    {
        ArgumentNullException.ThrowIfNull(
            subCategory);

        if (subCategory.Id == Id)
        {
            throw new InvalidOperationException(
                "A category cannot be its own subcategory.");
        }

        subCategory.SetParentCategory(
            this);

        if (!SubCategories.Contains(
                subCategory))
        {
            SubCategories.Add(
                subCategory);
        }

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void RemoveSubCategory(
        Category subCategory)
    {
        ArgumentNullException.ThrowIfNull(
            subCategory);

        if (SubCategories.Remove(
                subCategory))
        {
            subCategory.SetParentCategory(
                null);

            UpdatedAt =
                DateTime.UtcNow;
        }
    }

    // =========================================================
    // Display Order
    // =========================================================

    public void SetDisplayOrder(
        int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                "Display order cannot be negative.");
        }

        DisplayOrder =
            displayOrder;

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // Active
    // =========================================================

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;

        // Inactive categories must not
        // remain visible as published/featured.
        IsPublished = false;

        IsFeatured = false;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void SetActive(
        bool isActive)
    {
        if (isActive)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    // =========================================================
    // Publishing
    // =========================================================

    public void Publish()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                "An inactive category cannot be published.");
        }

        IsPublished = true;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;

        // A non-published category cannot be featured.
        IsFeatured = false;

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // Featured
    // =========================================================

    public void SetFeatured(
        bool isFeatured)
    {
        if (isFeatured)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException(
                    "An inactive category cannot be featured.");
            }

            if (!IsPublished)
            {
                throw new InvalidOperationException(
                    "An unpublished category cannot be featured.");
            }
        }

        IsFeatured =
            isFeatured;

        UpdatedAt =
            DateTime.UtcNow;
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

        DeletedAt =
            DateTime.UtcNow;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;

        DeletedAt = null;

        IsActive = true;

        IsPublished = false;

        IsFeatured = false;

        UpdatedAt =
            DateTime.UtcNow;
    }

    // =========================================================
    // Validation
    // =========================================================

    private static void ValidateName(
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));
        }
    }

    // =========================================================
    // Helpers
    // =========================================================

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
        {
            throw new ArgumentException(
                "Slug source cannot be empty.",
                nameof(value));
        }

        var slug =
            value
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("?", "")
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace(".", "-");

        while (slug.Contains("--"))
        {
            slug =
                slug.Replace(
                    "--",
                    "-");
        }

        return slug.Trim('-');
    }
}