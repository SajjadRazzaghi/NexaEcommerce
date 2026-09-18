using NexaEcommerce.Modules.Orders.Application.DTOs;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public interface IPackageService
{
    Task<PackageDto> CreateAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageDto>> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<PackageDto> StartPackingAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default);

    Task<PackageDto> UpdateDimensionsAsync(
        string tenantId,
        Guid packageId,
        UpdatePackageDimensionsRequest request,
        CancellationToken cancellationToken = default);

    Task<PackageDto> MarkPackedAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default);

    Task<PackageDto> MarkLabelPrintedAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default);

    Task<PackageDto> SetTrackingAsync(
        string tenantId,
        Guid packageId,
        SetPackageTrackingRequest request,
        CancellationToken cancellationToken = default);

    Task<PackageDto> MarkShippedAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default);

    Task<PackageDto> MarkDeliveredAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default);

    Task<PackageLabelDto> GetLabelAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default);
}
