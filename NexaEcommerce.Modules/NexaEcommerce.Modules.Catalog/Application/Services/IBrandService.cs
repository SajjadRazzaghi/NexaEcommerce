using NexaEcommerce.Modules.Catalog.Application.Brands.DTOs;
using NexaEcommerce.SharedKernel.Pagination;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public interface IBrandService
{
    // =========================================================
    // Queries
    // =========================================================

    Task<BrandDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BrandDetailsDto?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BrandListDto>> GetPagedAsync(
        BrandFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BrandLookupDto>> GetLookupAsync(
        CancellationToken cancellationToken = default);

    // =========================================================
    // CRUD
    // =========================================================

    Task<Guid> CreateAsync(
        CreateBrandDto dto,
        CancellationToken cancellationToken = default);

    Task<BrandDetailsDto> UpdateAsync(
        Guid id,
        UpdateBrandDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // =========================================================
    // Status
    // =========================================================

    Task ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task PublishAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task UnPublishAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task FeatureAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task UnFeatureAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}