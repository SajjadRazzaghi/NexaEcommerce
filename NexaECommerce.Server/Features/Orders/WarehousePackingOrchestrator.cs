using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed class WarehousePackingOrchestrator(
    IOrderRepository orderRepository,
    IFulfillmentRepository fulfillmentRepository,
    IPackageRepository packageRepository,
    IOrderUnitOfWork unitOfWork)
{
    public async Task<WarehousePackingResultDto> StartAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(
            tenantId,
            orderId);

        var normalizedTenantId =
            tenantId.Trim();

        var order =
            await orderRepository.GetByIdAsync(
                normalizedTenantId,
                orderId,
                null,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        if (order.Status != OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "Order must be in Processing status before packing starts.");
        }

        var fulfillment =
            await fulfillmentRepository.GetByOrderAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        if (fulfillment is null)
        {
            throw new InvalidOperationException(
                "Fulfillment must exist before packing starts.");
        }

        // Idempotency:
        // اگر قبلاً Packing شروع شده، دوباره package جدید نساز.
        if (fulfillment.Status == FulfillmentStatus.Packing)
        {
            await EnsurePackingPackageAsync(
                normalizedTenantId,
                orderId,
                fulfillment,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            var existingPackages =
                await packageRepository.GetByOrderIdAsync(
                    normalizedTenantId,
                    orderId,
                    cancellationToken);

            return MapResult(
                order,
                fulfillment,
                existingPackages,
                alreadyProcessed: true);
        }

        if (fulfillment.Status != FulfillmentStatus.Picked)
        {
            throw new InvalidOperationException(
                "Fulfillment must be in Picked status before packing starts.");
        }

        await EnsurePackingPackageAsync(
            normalizedTenantId,
            orderId,
            fulfillment,
            cancellationToken);

        fulfillment.StartPacking();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        var packages =
            await packageRepository.GetByOrderIdAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        return MapResult(
            order,
            fulfillment,
            packages,
            alreadyProcessed: false);
    }

    public async Task<WarehousePackingResultDto> CompleteAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(
            tenantId,
            orderId);

        var normalizedTenantId =
            tenantId.Trim();

        var order =
            await orderRepository.GetByIdAsync(
                normalizedTenantId,
                orderId,
                null,
                cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        if (order.Status != OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "Order must be in Processing status while packing is completed.");
        }

        var fulfillment =
            await fulfillmentRepository.GetByOrderAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        if (fulfillment is null)
        {
            throw new InvalidOperationException(
                "Fulfillment was not found.");
        }

        var packages =
            (await packageRepository.GetByOrderIdAsync(
                normalizedTenantId,
                orderId,
                cancellationToken))
            .Where(
                x =>
                    x.Status != PackageStatus.Cancelled)
            .OrderBy(
                x => x.PackageNumber)
            .ThenBy(
                x => x.Id)
            .ToList();

        if (packages.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one active package is required before packing can be completed.");
        }

        // Idempotency
        if (fulfillment.Status == FulfillmentStatus.Packed)
        {
            EnsurePackagesPacked(
                packages);

            return MapResult(
                order,
                fulfillment,
                packages,
                alreadyProcessed: true);
        }

        if (fulfillment.Status != FulfillmentStatus.Packing)
        {
            throw new InvalidOperationException(
                "Fulfillment must be in Packing status before packing can be completed.");
        }

        foreach (var packageSummary in packages)
        {
            if (packageSummary.Status == PackageStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Every active package must be started before packing can be completed.");
            }

            if (packageSummary.Status is
                PackageStatus.Shipped or
                PackageStatus.Delivered)
            {
                throw new InvalidOperationException(
                    "A shipped or delivered package cannot be completed through packing.");
            }

            if (packageSummary.Status is
                PackageStatus.Packed or
                PackageStatus.LabelPrinted)
            {
                continue;
            }

            if (packageSummary.Status != PackageStatus.Packing)
            {
                throw new InvalidOperationException(
                    $"Package '{packageSummary.Id}' is not in a valid state for packing completion.");
            }

            var package =
                await packageRepository.GetByIdAsync(
                    normalizedTenantId,
                    packageSummary.Id,
                    cancellationToken);

            if (package is null)
            {
                throw new KeyNotFoundException(
                    "Package was not found.");
            }

            package.MarkPacked(
                DateTime.UtcNow);
        }

        fulfillment.MarkPacked();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        var resultPackages =
            await packageRepository.GetByOrderIdAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        return MapResult(
            order,
            fulfillment,
            resultPackages,
            alreadyProcessed: false);
    }

    private async Task EnsurePackingPackageAsync(
        string tenantId,
        Guid orderId,
        Fulfillment fulfillment,
        CancellationToken cancellationToken)
    {
        var packages =
            (await packageRepository.GetByOrderIdAsync(
                tenantId,
                orderId,
                cancellationToken))
            .Where(
                x =>
                    x.Status != PackageStatus.Cancelled)
            .OrderBy(
                x => x.PackageNumber)
            .ThenBy(
                x => x.Id)
            .ToList();

        if (packages.Count == 0)
        {
            var nextNumber =
                await packageRepository.GetNextPackageNumberAsync(
                    tenantId,
                    orderId,
                    cancellationToken);

            var package =
                Package.Create(
                    tenantId,
                    orderId,
                    fulfillment.Id,
                    nextNumber,
                    DateTime.UtcNow);

            package.StartPacking(
                DateTime.UtcNow);

            await packageRepository.AddAsync(
                package,
                cancellationToken);

            return;
        }

        foreach (var packageSummary in packages)
        {
            if (packageSummary.Status == PackageStatus.Draft)
            {
                var package =
                    await packageRepository.GetByIdAsync(
                        tenantId,
                        packageSummary.Id,
                        cancellationToken);

                if (package is null)
                {
                    throw new KeyNotFoundException(
                        "Package was not found.");
                }

                package.StartPacking(
                    DateTime.UtcNow);

                continue;
            }

            if (packageSummary.Status is
                PackageStatus.Packed or
                PackageStatus.LabelPrinted or
                PackageStatus.Shipped or
                PackageStatus.Delivered)
            {
                throw new InvalidOperationException(
                    "An active package has already progressed beyond the packing start stage.");
            }
        }
    }

    private static void EnsurePackagesPacked(
        IReadOnlyCollection<Package> packages)
    {
        foreach (var package in packages)
        {
            if (package.Status is
                PackageStatus.Packed or
                PackageStatus.LabelPrinted)
            {
                continue;
            }

            throw new InvalidOperationException(
                "All active packages must be packed.");
        }
    }

    private static WarehousePackingResultDto MapResult(
        Order order,
        Fulfillment fulfillment,
        IEnumerable<Package> packages,
        bool alreadyProcessed)
    {
        var packageDtos =
            packages
                .Where(
                    x =>
                        x.Status !=
                        PackageStatus.Cancelled)
                .OrderBy(
                    x => x.PackageNumber)
                .ThenBy(
                    x => x.Id)
                .Select(
                    MapPackage)
                .ToList();

        return new WarehousePackingResultDto(
            order.Id,
            order.OrderNumber,
            fulfillment.Id,
            fulfillment.Status.ToString(),
            alreadyProcessed,
            packageDtos);
    }

    private static PackageDto MapPackage(
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
        Guid orderId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }
    }

    public sealed record WarehousePackingResultDto(
        Guid OrderId,
        string OrderNumber,
        Guid FulfillmentId,
        string Status,
        bool AlreadyProcessed,
        IReadOnlyList<PackageDto> Packages);
}

