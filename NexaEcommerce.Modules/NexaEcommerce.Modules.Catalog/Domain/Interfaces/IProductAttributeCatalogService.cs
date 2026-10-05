using NexaEcommerce.Modules.Catalog.Application.CatalogAttributes.DTOs;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface IProductAttributeCatalogService
{
    Task<IReadOnlyList<CatalogAttributeDto>>
        GetForCategoryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);
}