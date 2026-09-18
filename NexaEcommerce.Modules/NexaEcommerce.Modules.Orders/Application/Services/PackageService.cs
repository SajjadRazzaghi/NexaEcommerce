using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class PackageService(
    IPackageRepository packages,
    IOrderRepository orders,
    IFulfillmentRepository fulfillments,
    IOrderUnitOfWork unitOfWork)
    : IPackageService
{
    public async Task<PackageDto> CreateAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(
            tenantId,
            orderId);

        var order =
            await orders.GetByIdAsync(
                tenantId,
                orderId,
                null,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        var fulfillment =
            await fulfillments.GetByOrderAsync(
                tenantId,
                orderId,
                cancellationToken);

        if (fulfillment is null)
        {
            throw new InvalidOperationException(
                "Fulfillment must exist before creating a package.");
        }

        if (fulfillment.Status != FulfillmentStatus.Packing &&
            fulfillment.Status != FulfillmentStatus.Packed &&
            fulfillment.Status != FulfillmentStatus.ReadyToShip)
        {
            throw new InvalidOperationException(
                "A package can only be created during the packing phase.");
        }

        var existing =
            await packages.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        var nextNumber =
            existing.Count == 0
                ? 1
                : existing.Max(x => x.PackageNumber) + 1;

        var package =
            Package.Create(
                tenantId,
                orderId,
                fulfillment.Id,
                nextNumber,
                DateTime.UtcNow);

        await packages.AddAsync(
            package,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(package);
    }

    public async Task<IReadOnlyList<PackageDto>> GetByOrderAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(
            tenantId,
            orderId);

        var packagesResult =
            await packages.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken);

        return packagesResult
            .Select(Map)
            .ToList();
    }

    public async Task<PackageDto> StartPackingAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package =
            await GetPackageAsync(
                tenantId,
                packageId,
                cancellationToken);

        package.StartPacking(
            DateTime.UtcNow);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(package);
    }

    public async Task<PackageDto> UpdateDimensionsAsync(
        string tenantId,
        Guid packageId,
        UpdatePackageDimensionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var package =
            await GetPackageAsync(
                tenantId,
                packageId,
                cancellationToken);

        package.SetDimensions(
            request.WeightKg,
            request.LengthCm,
            request.WidthCm,
            request.HeightCm,
            DateTime.UtcNow);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(package);
    }

    public async Task<PackageDto> MarkPackedAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package =
            await GetPackageAsync(
                tenantId,
                packageId,
                cancellationToken);

        package.MarkPacked(
            DateTime.UtcNow);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(package);
    }

    public async Task<PackageDto> MarkLabelPrintedAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package =
            await GetPackageAsync(
                tenantId,
                packageId,
                cancellationToken);

        package.MarkLabelPrinted(
            DateTime.UtcNow);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(package);
    }

    public async Task<PackageDto> SetTrackingAsync(
        string tenantId,
        Guid packageId,
        SetPackageTrackingRequest request,
        CancellationToken cancellationToken = default)
    {
        var package =
            await GetPackageAsync(
                tenantId,
                packageId,
                cancellationToken);

        package.SetTrackingNumber(
            request.TrackingNumber,
            DateTime.UtcNow);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(package);
    }

    public async Task<PackageDto> MarkShippedAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package =
            await GetPackageAsync(
                tenantId,
                packageId,
                cancellationToken);

        package.MarkShipped(
            DateTime.UtcNow);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(package);
    }

    public async Task<PackageDto> MarkDeliveredAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package =
            await GetPackageAsync(
                tenantId,
                packageId,
                cancellationToken);

        package.MarkDelivered(
            DateTime.UtcNow);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(package);
    }

    public async Task<PackageLabelDto> GetLabelAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package =
            await GetPackageAsync(
                tenantId,
                packageId,
                cancellationToken);

        var order =
            await orders.GetByIdAsync(
                tenantId,
                package.OrderId,
                null,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        return new PackageLabelDto(
            package.Id,
            package.OrderId,
            package.FulfillmentId,
            package.PackageNumber,
            order.OrderNumber,
            order.ShippingFullName,
            order.ShippingPhone,
            order.ShippingAddress,
            order.ShippingCity,
            order.ShippingPostalCode,
            package.TrackingNumber,
            package.WeightKg,
            package.Status.ToString(),
            DateTime.UtcNow);
    }

    private async Task<Package> GetPackageAsync(
        string tenantId,
        Guid packageId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (packageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Package id is required.",
                nameof(packageId));
        }

        var package =
            await packages.GetByIdAsync(
                tenantId,
                packageId,
                cancellationToken);

        if (package is null)
        {
            throw new KeyNotFoundException(
                "Package was not found.");
        }

        return package;
    }

    private static PackageDto Map(
        Package package)
    {
        return new PackageDto(
            package.Id,
            package.OrderId,
            package.FulfillmentId,
            package.PackageNumber,
            package.Status.ToString(),
            package.TrackingNumber,
            package.WeightKg,
            package.LengthCm,
            package.WidthCm,
            package.HeightCm,
            package.PackingStartedAt,
            package.PackedAt,
            package.LabelPrintedAt,
            package.ShippedAt,
            package.DeliveredAt,
            package.CreatedAt,
            package.UpdatedAt);
    }

    private static void ValidateScope(
        string tenantId,
        Guid id)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Id is required.",
                nameof(id));
        }
    }
}