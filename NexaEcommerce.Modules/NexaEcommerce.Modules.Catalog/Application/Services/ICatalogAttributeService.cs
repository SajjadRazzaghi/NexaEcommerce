using NexaEcommerce.Modules.Catalog.Application.CatalogAttributes.DTOs;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public interface ICatalogAttributeService
{
    Task<IReadOnlyCollection<CatalogAttributeDto>> GetAllAsync(
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<CatalogAttributeDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CatalogAttributeDto> CreateAsync(
        CreateCatalogAttributeDto dto,
        CancellationToken cancellationToken = default);

    Task<CatalogAttributeDto?> UpdateAsync(
        Guid id,
        UpdateCatalogAttributeDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CatalogAttributeValueDto?> AddValueAsync(
        Guid attributeId,
        CreateCatalogAttributeValueDto dto,
        CancellationToken cancellationToken = default);

    Task<CatalogAttributeValueDto?> UpdateValueAsync(
        Guid attributeId,
        Guid valueId,
        UpdateCatalogAttributeValueDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteValueAsync(
        Guid attributeId,
        Guid valueId,
        CancellationToken cancellationToken = default);
}