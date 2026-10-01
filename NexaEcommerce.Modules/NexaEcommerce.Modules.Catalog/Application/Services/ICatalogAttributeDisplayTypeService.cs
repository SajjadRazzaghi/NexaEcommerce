using NexaEcommerce.Modules.Catalog.Application.CatalogAttributes.DTOs;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public interface ICatalogAttributeDisplayTypeService
{
    Task<IReadOnlyCollection<CatalogAttributeDisplayTypeDto>>
        GetAllActiveAsync(
            CancellationToken cancellationToken = default);
}