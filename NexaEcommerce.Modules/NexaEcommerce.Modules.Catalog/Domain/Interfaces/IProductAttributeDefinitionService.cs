using NexaEcommerce.Modules.Catalog.Application.DTOs;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface IProductAttributeDefinitionService
{
    Task<IReadOnlyList<ProductAttributeDefinitionDto>>
        GetForCategoriesAsync(
            IReadOnlyCollection<Guid> categoryIds,
            CancellationToken cancellationToken = default);
}