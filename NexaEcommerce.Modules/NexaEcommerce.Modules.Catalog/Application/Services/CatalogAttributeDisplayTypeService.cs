using NexaEcommerce.Modules.Catalog.Application.CatalogAttributes.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class CatalogAttributeDisplayTypeService
    : ICatalogAttributeDisplayTypeService
{
    private readonly ICatalogAttributeDisplayTypeRepository _repository;

    public CatalogAttributeDisplayTypeService(
        ICatalogAttributeDisplayTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<CatalogAttributeDisplayTypeDto>>
        GetAllActiveAsync(
            CancellationToken cancellationToken = default)
    {
        var items =
            await _repository.GetAllActiveAsync(
                cancellationToken);

        return items
            .Select(x =>
                new CatalogAttributeDisplayTypeDto(
                    x.Id,
                    x.Code,
                    x.NameEn,
                    x.NameFa,
                    x.DisplayOrder))
            .ToArray();
    }
}