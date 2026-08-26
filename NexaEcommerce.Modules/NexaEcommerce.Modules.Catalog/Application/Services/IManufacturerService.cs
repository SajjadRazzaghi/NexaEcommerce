using NexaEcommerce.Modules.Catalog.Application.Manufacturers.DTOs;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public interface IManufacturerService
{
    Task<object> GetPagedAsync(
        ManufacturerFilterDto filter,
        CancellationToken ct);

    Task<List<ManufacturerLookupDto>> GetLookupAsync(
        CancellationToken ct);

    Task<ManufacturerDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct);

    Task<ManufacturerDetailsDto?> GetBySlugAsync(
        string slug,
        CancellationToken ct);

    Task<Guid> CreateAsync(
        CreateManufacturerDto request,
        CancellationToken ct);

    Task<ManufacturerDetailsDto> UpdateAsync(
        Guid id,
        UpdateManufacturerDto request,
        CancellationToken ct);

    Task DeleteAsync(
        Guid id,
        CancellationToken ct);

    Task RestoreAsync(
        Guid id,
        CancellationToken ct);

    Task ActivateAsync(
        Guid id,
        CancellationToken ct);

    Task DeactivateAsync(
        Guid id,
        CancellationToken ct);

    Task PublishAsync(
        Guid id,
        CancellationToken ct);

    Task UnPublishAsync(
        Guid id,
        CancellationToken ct);

    Task FeatureAsync(
        Guid id,
        CancellationToken ct);

    Task UnFeatureAsync(
        Guid id,
        CancellationToken ct);
}