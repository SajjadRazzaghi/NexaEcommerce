using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities;

public class Category : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string? Slug { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }

    public Guid? ParentCategoryId { get; private set; }

    // Navigation Properties
    public Category? ParentCategory { get; private set; }

    public ICollection<Category> SubCategories { get; private set; }
        = new List<Category>();

    public ICollection<ProductCategory> ProductCategories { get; private set; }
        = new List<ProductCategory>();

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    private Category()
    {
    }

    public Category(
        string name,
        string? slug = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));

        Name = name;
        Slug = slug ?? GenerateSlug(name);
        Description = description;

        IsActive = true;
        DisplayOrder = 0;
    }

    public void Update(
        string name,
        string? description,
        string? imageUrl,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));

        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetParentCategory(Category? parentCategory)
    {
        // جلوگیری از اینکه دسته‌بندی والد خودش شود
        if (parentCategory is not null && parentCategory.Id == Id)
            throw new InvalidOperationException(
                "A category cannot be its own parent.");

        ParentCategory = parentCategory;
        ParentCategoryId = parentCategory?.Id;

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSubCategory(Category subCategory)
    {
        ArgumentNullException.ThrowIfNull(subCategory);

        if (subCategory.Id == Id)
            throw new InvalidOperationException(
                "A category cannot be its own subcategory.");

        subCategory.SetParentCategory(this);

        if (!SubCategories.Contains(subCategory))
            SubCategories.Add(subCategory);
    }

    public void RemoveSubCategory(Category subCategory)
    {
        if (SubCategories.Remove(subCategory))
        {
            subCategory.SetParentCategory(null);
        }
    }

    public void SetDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder));

        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

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

    public void SetImage(string? imageUrl)
    {
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

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