using NexaEcommerce.Modules.Catalog.Application.DTOs;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetRootCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetSubCategoriesAsync(
        Guid parentCategoryId,
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}