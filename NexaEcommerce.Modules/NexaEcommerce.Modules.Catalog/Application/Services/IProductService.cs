using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.SharedKernel.Pagination;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
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

    Task<IEnumerable<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ProductDto?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ProductDto>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ProductDto>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ProductDto>> GetFeaturedAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(
        CreateProductDto createDto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        UpdateProductDto updateDto,
        CancellationToken cancellationToken = default);

    Task UpdateStockAsync(
        Guid id,
        int quantity,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task SetFeaturedAsync(
        Guid id,
        bool isFeatured,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}