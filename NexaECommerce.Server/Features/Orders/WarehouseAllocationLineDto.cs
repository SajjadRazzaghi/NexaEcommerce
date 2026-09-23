using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaECommerce.Server.Features.Orders;

public sealed record WarehouseAllocationLineDto(
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    int RequestedQuantity,
    int WarehouseAvailableQuantity,
    int? PickingLocationAvailableQuantity);

public sealed record WarehouseAllocationResultDto(
    Guid OrderId,
    string OrderNumber,
    Guid FulfillmentId,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid? PickingLocationId,
    string? PickingLocationCode,
    bool AlreadyAllocated,
    IReadOnlyList<WarehouseAllocationLineDto> Lines);

public sealed class WarehouseAllocationOrchestrator(
    IOrderRepository orderRepository,
    IFulfillmentRepository fulfillmentRepository,
    IWarehouseRepository warehouseRepository,
    IWarehouseStockRepository warehouseStockRepository,
    [FromServices] IFulfillmentService fulfillmentService)
{
    public async Task<WarehouseAllocationResultDto> AllocateAsync(
        string tenantId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

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

        if (order.Status is
     OrderStatus.Cancelled or
     OrderStatus.Shipped or
     OrderStatus.Delivered)
        {
            throw new InvalidOperationException(
                "This order cannot be allocated to a warehouse in its current state.");
        }

        if (order.Items.Count == 0)
        {
            throw new InvalidOperationException(
                "The order does not contain any items.");
        }

        var requestedQuantities =
            BuildRequestedQuantities(order);

        var fulfillment =
            await fulfillmentRepository.GetByOrderAsync(
                normalizedTenantId,
                orderId,
                cancellationToken);

        if (fulfillment is null)
        {
            throw new KeyNotFoundException(
                "Fulfillment was not found for this order. Start fulfillment first.");
        }

        /*
         * ------------------------------------------------------------
         * Idempotency:
         * Once a warehouse has been assigned, we never silently move
         * the order to another warehouse from a second allocation call.
         * ------------------------------------------------------------
         */
        if (fulfillment.WarehouseId.HasValue)
        {
            return await BuildAlreadyAllocatedResultAsync(
                normalizedTenantId,
                order,
                fulfillment,
                requestedQuantities,
                cancellationToken);
        }

        var warehouses =
            await warehouseRepository.GetWarehousesAsync(
                normalizedTenantId,
                includeInactive: false,
                cancellationToken);

        if (warehouses.Count == 0)
        {
            throw new InvalidOperationException(
                "No active warehouse is available for fulfillment.");
        }

        var allWarehouseStocks =
            await warehouseStockRepository.GetAllAsync(
                normalizedTenantId,
                includeZeroStock: false,
                cancellationToken);

        /*
         * Active locations are part of the allocation decision.
         * Stock in an inactive location must not make a warehouse
         * look fulfillable.
         */
        var activeLocationMap =
            new Dictionary<Guid, IReadOnlyList<WarehouseLocation>>();

        foreach (var warehouse in warehouses)
        {
            var locations =
                await warehouseRepository.GetLocationsAsync(
                    normalizedTenantId,
                    warehouse.Id,
                    includeInactive: false,
                    cancellationToken);

            activeLocationMap[warehouse.Id] =
                locations;
        }

        var candidates =
            new List<WarehouseCandidate>();

        foreach (var warehouse in warehouses)
        {
            var activeLocationIds =
                activeLocationMap[warehouse.Id]
                    .Select(x => x.Id)
                    .ToHashSet();

            var warehouseStocks =
                allWarehouseStocks
                    .Where(
                        x =>
                            x.WarehouseId == warehouse.Id &&
                            activeLocationIds.Contains(x.LocationId))
                    .ToList();

            var availabilityByVariant =
                AggregateAvailableQuantities(
                    warehouseStocks);

            if (!CanFulfill(
                    requestedQuantities,
                    availabilityByVariant))
            {
                continue;
            }

            var surplus =
                CalculateSurplus(
                    requestedQuantities,
                    availabilityByVariant);

            candidates.Add(
                new WarehouseCandidate(
                    warehouse,
                    warehouseStocks,
                    availabilityByVariant,
                    surplus));
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                "No active warehouse can fulfill the complete order from a single warehouse.");
        }

        /*
         * Selection policy:
         *
         * 1. Default warehouse first.
         * 2. Lowest stock surplus after satisfying the order.
         * 3. Warehouse name.
         * 4. Warehouse id for deterministic tie-breaking.
         */
        var selectedCandidate =
            candidates
                .OrderByDescending(
                    x => x.Warehouse.IsDefault)
                .ThenBy(
                    x => x.Surplus)
                .ThenBy(
                    x => x.Warehouse.Name)
                .ThenBy(
                    x => x.Warehouse.Id)
                .First();

        var selectedLocations =
            activeLocationMap[
                selectedCandidate.Warehouse.Id];

        var locationCandidates =
            BuildLocationCandidates(
                selectedCandidate.WarehouseStocks,
                selectedLocations,
                requestedQuantities);

        /*
         * A single picking location is optional.
         *
         * The warehouse itself is still valid when the order can be
         * fulfilled across multiple active locations inside that
         * warehouse. In that case PickingLocationId remains null and
         * warehouse staff can pick from multiple locations.
         */
        var selectedLocation =
            locationCandidates
                .OrderBy(
                    x => x.Surplus)
                .ThenBy(
                    x => x.Location.Code)
                .ThenBy(
                    x => x.Location.Id)
                .FirstOrDefault();

        var fulfillmentDto =
            await fulfillmentService.AssignWarehouseAsync(
                normalizedTenantId,
                orderId,
                selectedCandidate.Warehouse.Id,
                selectedLocation?.Location.Id,
                cancellationToken);

        return new WarehouseAllocationResultDto(
            order.Id,
            order.OrderNumber,
            fulfillmentDto.Id,
            selectedCandidate.Warehouse.Id,
            selectedCandidate.Warehouse.Code,
            selectedCandidate.Warehouse.Name,
            selectedLocation?.Location.Id,
            selectedLocation?.Location.Code,
            AlreadyAllocated: false,
            BuildLines(
                order,
                requestedQuantities,
                selectedCandidate.AvailabilityByVariant,
                selectedLocation?.AvailabilityByVariant));
    }

    private async Task<WarehouseAllocationResultDto>
        BuildAlreadyAllocatedResultAsync(
            string tenantId,
            Order order,
            Fulfillment fulfillment,
            IReadOnlyDictionary<Guid, int> requestedQuantities,
            CancellationToken cancellationToken)
    {
        var warehouse =
            await warehouseRepository.GetWarehouseAsync(
                tenantId,
                fulfillment.WarehouseId!.Value,
                cancellationToken);

        if (warehouse is null)
        {
            throw new InvalidOperationException(
                "The warehouse assigned to this fulfillment no longer exists.");
        }

        var activeLocations =
            await warehouseRepository.GetLocationsAsync(
                tenantId,
                warehouse.Id,
                includeInactive: false,
                cancellationToken);

        var activeLocationIds =
            activeLocations
                .Select(x => x.Id)
                .ToHashSet();

        var warehouseStocks =
            await warehouseStockRepository.GetByWarehouseAsync(
                tenantId,
                warehouse.Id,
                locationId: null,
                productVariantId: null,
                includeZeroStock: false,
                cancellationToken);

        var activeStocks =
            warehouseStocks
                .Where(
                    x =>
                        activeLocationIds.Contains(
                            x.LocationId))
                .ToList();

        var availabilityByVariant =
            AggregateAvailableQuantities(
                activeStocks);

        IReadOnlyDictionary<Guid, int>? locationAvailability =
            null;

        if (fulfillment.PickingLocationId.HasValue)
        {
            var selectedLocation =
                activeLocations.FirstOrDefault(
                    x =>
                        x.Id ==
                        fulfillment.PickingLocationId.Value);

            if (selectedLocation is not null)
            {
                var selectedLocationStocks =
                    activeStocks
                        .Where(
                            x =>
                                x.LocationId ==
                                selectedLocation.Id)
                        .ToList();

                locationAvailability =
                    AggregateAvailableQuantities(
                        selectedLocationStocks);
            }
        }

        return new WarehouseAllocationResultDto(
            order.Id,
            order.OrderNumber,
            fulfillment.Id,
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            fulfillment.PickingLocationId,
            activeLocations
                .FirstOrDefault(
                    x =>
                        x.Id ==
                        fulfillment.PickingLocationId)
                ?.Code,
            AlreadyAllocated: true,
            BuildLines(
                order,
                requestedQuantities,
                availabilityByVariant,
                locationAvailability));
    }

    private static IReadOnlyList<LocationCandidate>
        BuildLocationCandidates(
            IReadOnlyCollection<WarehouseStock> stocks,
            IReadOnlyCollection<WarehouseLocation> locations,
            IReadOnlyDictionary<Guid, int> requestedQuantities)
    {
        var candidates =
            new List<LocationCandidate>();

        foreach (var location in locations)
        {
            var locationStocks =
                stocks
                    .Where(
                        x =>
                            x.LocationId ==
                            location.Id)
                    .ToList();

            var availabilityByVariant =
                AggregateAvailableQuantities(
                    locationStocks);

            if (!CanFulfill(
                    requestedQuantities,
                    availabilityByVariant))
            {
                continue;
            }

            var surplus =
                CalculateSurplus(
                    requestedQuantities,
                    availabilityByVariant);

            candidates.Add(
                new LocationCandidate(
                    location,
                    availabilityByVariant,
                    surplus));
        }

        return candidates;
    }

    private static IReadOnlyList<WarehouseAllocationLineDto>
        BuildLines(
            Order order,
            IReadOnlyDictionary<Guid, int> requestedQuantities,
            IReadOnlyDictionary<Guid, int> warehouseAvailability,
            IReadOnlyDictionary<Guid, int>? pickingLocationAvailability)
    {
        return order.Items
            .Select(
                item =>
                {
                    warehouseAvailability.TryGetValue(
                        item.ProductVariantId,
                        out var warehouseAvailable);

                    int? locationAvailable = null;

                    if (pickingLocationAvailability is not null &&
                        pickingLocationAvailability.TryGetValue(
                            item.ProductVariantId,
                            out var selectedLocationAvailable))
                    {
                        locationAvailable =
                            selectedLocationAvailable;
                    }

                    return new WarehouseAllocationLineDto(
                        item.ProductVariantId,
                        item.Sku,
                        item.ProductName,
                        requestedQuantities[
                            item.ProductVariantId],
                        warehouseAvailable,
                        locationAvailable);
                })
            .ToList();
    }

    private static Dictionary<Guid, int>
        BuildRequestedQuantities(
            Order order)
    {
        var result =
            new Dictionary<Guid, int>();

        foreach (var item in order.Items)
        {
            if (result.TryGetValue(
                    item.ProductVariantId,
                    out var current))
            {
                result[item.ProductVariantId] =
                    checked(current + item.Quantity);
            }
            else
            {
                result[item.ProductVariantId] =
                    item.Quantity;
            }
        }

        return result;
    }

    private static Dictionary<Guid, int>
        AggregateAvailableQuantities(
            IEnumerable<WarehouseStock> stocks)
    {
        var result =
            new Dictionary<Guid, int>();

        foreach (var stock in stocks)
        {
            if (stock.AvailableQuantity <= 0)
            {
                continue;
            }

            if (result.TryGetValue(
                    stock.ProductVariantId,
                    out var current))
            {
                result[stock.ProductVariantId] =
                    checked(
                        current +
                        stock.AvailableQuantity);
            }
            else
            {
                result[stock.ProductVariantId] =
                    stock.AvailableQuantity;
            }
        }

        return result;
    }

    private static bool CanFulfill(
        IReadOnlyDictionary<Guid, int> requested,
        IReadOnlyDictionary<Guid, int> available)
    {
        foreach (var requirement in requested)
        {
            if (!available.TryGetValue(
                    requirement.Key,
                    out var quantity))
            {
                return false;
            }

            if (quantity < requirement.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static int CalculateSurplus(
        IReadOnlyDictionary<Guid, int> requested,
        IReadOnlyDictionary<Guid, int> available)
    {
        var surplus = 0;

        foreach (var requirement in requested)
        {
            available.TryGetValue(
                requirement.Key,
                out var availableQuantity);

            surplus =
                checked(
                    surplus +
                    Math.Max(
                        0,
                        availableQuantity -
                        requirement.Value));
        }

        return surplus;
    }

    private static void ValidateTenant(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }

    private sealed record WarehouseCandidate(
        Warehouse Warehouse,
        IReadOnlyCollection<WarehouseStock> WarehouseStocks,
        IReadOnlyDictionary<Guid, int> AvailabilityByVariant,
        int Surplus);

    private sealed record LocationCandidate(
        WarehouseLocation Location,
        IReadOnlyDictionary<Guid, int> AvailabilityByVariant,
        int Surplus);
}

