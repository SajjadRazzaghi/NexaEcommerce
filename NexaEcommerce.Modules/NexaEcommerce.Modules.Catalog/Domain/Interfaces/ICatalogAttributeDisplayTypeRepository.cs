using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface ICatalogAttributeDisplayTypeRepository
{
    Task<List<CatalogAttributeDisplayType>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    Task<CatalogAttributeDisplayType?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);
}