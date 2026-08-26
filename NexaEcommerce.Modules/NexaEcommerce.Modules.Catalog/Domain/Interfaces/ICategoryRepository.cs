using NexaEcommerce.Modules.Catalog.Domain.Entities;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Category?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetRootCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetSubCategoriesAsync(
        Guid parentCategoryId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default);

    void Update(Category category);

    void Remove(Category category);
}