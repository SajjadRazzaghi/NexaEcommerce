using NexaEcommerce.Modules.Catalog.Application.Brands.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.SharedKernel.Pagination;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Brand?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Brand>> GetPagedAsync(
        BrandFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Brand>> GetLookupAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<Brand?> GetDeletedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Brand brand,
        CancellationToken cancellationToken = default);

    void Update(
        Brand brand);

    void Delete(
        Brand brand);
}