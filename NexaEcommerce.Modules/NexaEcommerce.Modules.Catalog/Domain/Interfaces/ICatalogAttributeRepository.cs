using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface ICatalogAttributeRepository
{
    Task<CatalogAttribute?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CatalogAttribute?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<List<CatalogAttribute>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<List<CatalogAttribute>> SearchAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CatalogAttribute attribute,
        CancellationToken cancellationToken = default);

    void Update(CatalogAttribute attribute);

    void Remove(CatalogAttribute attribute);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}