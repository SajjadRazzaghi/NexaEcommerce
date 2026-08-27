// NexaEcommerce.Modules.Catalog/Domain/Interfaces/IProductRepository.cs
using NexaEcommerce.Modules.Catalog.Domain.Entities;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugAsync(
    string slug,
    CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> GetFeaturedAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? categoryId = null,
        Guid? brandId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isFeatured = null,
        bool? isInStock = null,
        bool? isActive = null,
        bool? isPublished = null,
        bool includeInactive = false,
        bool includeUnpublished = false,
        string? sortBy = null,
        bool desc = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);

    void Update(Product product);

    void Delete(Product product);
}
