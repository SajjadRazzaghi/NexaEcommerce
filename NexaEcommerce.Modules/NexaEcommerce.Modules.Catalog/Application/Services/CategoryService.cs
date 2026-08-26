using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.Modules.Catalog.Infrastructure;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CatalogDbContext _context;

    public CategoryService(
        ICategoryRepository categoryRepository,
        CatalogDbContext context)
    {
        _categoryRepository = categoryRepository;
        _context = context;
    }

    // =========================================================
    // Get All
    // =========================================================

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var categories =
            await _categoryRepository.GetAllAsync(
                cancellationToken);

        return categories
            .Select(Map)
            .ToList();
    }

    // =========================================================
    // Get Root Categories
    // =========================================================

    public async Task<IReadOnlyList<CategoryDto>>
        GetRootCategoriesAsync(
            CancellationToken cancellationToken = default)
    {
        var categories =
            await _categoryRepository.GetRootCategoriesAsync(
                cancellationToken);

        return categories
            .Select(Map)
            .ToList();
    }

    // =========================================================
    // Get Sub Categories
    // =========================================================

    public async Task<IReadOnlyList<CategoryDto>>
        GetSubCategoriesAsync(
            Guid parentCategoryId,
            CancellationToken cancellationToken = default)
    {
        var categories =
            await _categoryRepository.GetSubCategoriesAsync(
                parentCategoryId,
                cancellationToken);

        return categories
            .Select(Map)
            .ToList();
    }

    // =========================================================
    // Get By Id
    // =========================================================

    public async Task<CategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        var category =
            await _categoryRepository.GetByIdAsync(
                id,
                cancellationToken);

        return category is null
            ? null
            : Map(category);
    }

    // =========================================================
    // Get By Slug
    // =========================================================

    public async Task<CategoryDto?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var category =
            await _categoryRepository.GetBySlugAsync(
                slug.Trim(),
                cancellationToken);

        return category is null
            ? null
            : Map(category);
    }

    // =========================================================
    // Create
    // =========================================================

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException(
                "نام دسته‌بندی الزامی است.",
                nameof(dto.Name));
        }

        Category? parent = null;

        // -----------------------------------------------------
        // Parent
        // -----------------------------------------------------

        if (dto.ParentCategoryId.HasValue)
        {
            parent =
                await _categoryRepository.GetByIdAsync(
                    dto.ParentCategoryId.Value,
                    cancellationToken);

            if (parent is null)
            {
                throw new KeyNotFoundException(
                    "دسته‌بندی والد یافت نشد.");
            }
        }

        // -----------------------------------------------------
        // Create
        // -----------------------------------------------------

        var category =
            new Category(
                dto.Name.Trim(),
                null,
                NormalizeNullable(dto.Description));

        // -----------------------------------------------------
        // Image
        // -----------------------------------------------------

        category.SetImage(
            NormalizeNullable(dto.ImageUrl));

        // -----------------------------------------------------
        // Parent
        // -----------------------------------------------------

        if (parent is not null)
        {
            await EnsureNoCycleAsync(
                category.Id,
                parent,
                cancellationToken);

            category.SetParentCategory(parent);
        }

        // -----------------------------------------------------
        // Save
        // -----------------------------------------------------

        await _categoryRepository.AddAsync(
            category,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);

        return Map(category);
    }

    // =========================================================
    // Update
    // =========================================================

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (id == Guid.Empty)
            return false;

        var category =
            await _categoryRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (category is null)
            return false;

        // -----------------------------------------------------
        // Self Parent Check
        // -----------------------------------------------------

        if (dto.ParentCategoryId == id)
        {
            throw new InvalidOperationException(
                "دسته‌بندی نمی‌تواند والد خودش باشد.");
        }

        // -----------------------------------------------------
        // Find Parent
        // -----------------------------------------------------

        Category? parent = null;

        if (dto.ParentCategoryId.HasValue)
        {
            parent =
                await _categoryRepository.GetByIdAsync(
                    dto.ParentCategoryId.Value,
                    cancellationToken);

            if (parent is null)
            {
                throw new KeyNotFoundException(
                    "دسته‌بندی والد یافت نشد.");
            }

            // -------------------------------------------------
            // Cycle Detection
            // -------------------------------------------------

            await EnsureNoCycleAsync(
                id,
                parent,
                cancellationToken);
        }

        // -----------------------------------------------------
        // Update Basic Information
        // -----------------------------------------------------

        category.Update(
            dto.Name,
            dto.Description,
            dto.ImageUrl,
            dto.IsActive);

        // -----------------------------------------------------
        // Parent
        // -----------------------------------------------------

        category.SetParentCategory(parent);

        // -----------------------------------------------------
        // Save
        // -----------------------------------------------------

        await _context.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    // =========================================================
    // Delete
    // =========================================================

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return false;

        var category =
            await _categoryRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (category is null)
            return false;

        // -----------------------------------------------------
        // Check Children
        // -----------------------------------------------------

        var hasChildren =
            await _context.Categories
                .AnyAsync(
                    x => x.ParentCategoryId == id,
                    cancellationToken);

        if (hasChildren)
        {
            throw new InvalidOperationException(
                "دسته‌بندی دارای زیرمجموعه است و قابل حذف نیست.");
        }

        // -----------------------------------------------------
        // Check Products
        // -----------------------------------------------------

        var hasProducts =
            await _context.ProductCategories
                .AnyAsync(
                    x => x.CategoryId == id,
                    cancellationToken);

        if (hasProducts)
        {
            throw new InvalidOperationException(
                "این دسته‌بندی دارای محصول است و قابل حذف نیست.");
        }

        // -----------------------------------------------------
        // Soft Delete
        // -----------------------------------------------------

        category.IsDeleted = true;

        category.DeletedAt =
            DateTime.UtcNow;

        // اینجا مستقیماً IsActive را تغییر نمی‌دهیم
        // چون setter آن private است.
        category.Deactivate();

        // -----------------------------------------------------
        // Save
        // -----------------------------------------------------

        await _context.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    // =========================================================
    // Cycle Detection
    // =========================================================

    private async Task EnsureNoCycleAsync(
        Guid categoryId,
        Category parent,
        CancellationToken cancellationToken)
    {
        var current = parent;

        while (true)
        {
            // -------------------------------------------------
            // Current parent is the category itself
            // -------------------------------------------------

            if (current.Id == categoryId)
            {
                throw new InvalidOperationException(
                    "ایجاد چرخه در دسته‌بندی مجاز نیست.");
            }

            // -------------------------------------------------
            // No more parents
            // -------------------------------------------------

            if (!current.ParentCategoryId.HasValue)
            {
                break;
            }

            // -------------------------------------------------
            // Load next parent
            // -------------------------------------------------

            var next =
                await _categoryRepository.GetByIdAsync(
                    current.ParentCategoryId.Value,
                    cancellationToken);

            // اگر والد بعدی پیدا نشد، مسیر تمام شده است.
            if (next is null)
            {
                break;
            }

            current = next;
        }
    }

    // =========================================================
    // Mapping
    // =========================================================

    private static CategoryDto Map(
        Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,

            Name = category.Name,

            Slug =
                category.Slug ?? string.Empty,

            Description =
                category.Description,

            ImageUrl =
                category.ImageUrl,

            ParentCategoryId =
                category.ParentCategoryId,

            ParentCategoryName =
                category.ParentCategory?.Name,

            IsActive =
                category.IsActive,

            ProductCount =
                category.ProductCategories.Count,

            SubCategories =
                category.SubCategories
                    .Select(Map)
                    .ToList()
        };
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
}