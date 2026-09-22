using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaECommerce.Server.Features.Inventory;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace NexaECommerce.Tests.Integration.Features.Inventory;

[Collection(IntegrationCollection.Name)]
public sealed class WarehouseTransferEndpointsTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task List_transfers_requires_read_permission()
    {
        using var client =
            factory.CreateAuthenticatedClient();

        var response =
            await client.GetAsync(
                "/api/inventory/transfers/?skip=0&take=20");

        response.StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_transfer_requires_manage_permission()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Read
                ]);

        var response =
            await client.PostAsJsonAsync(
                "/api/inventory/transfers/",
                new
                {
                    sourceWarehouseId = Guid.NewGuid(),
                    sourceLocationId = Guid.NewGuid(),
                    destinationWarehouseId = Guid.NewGuid(),
                    destinationLocationId = Guid.NewGuid(),
                    productVariantId = Guid.NewGuid(),
                    quantity = 1
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_transfer_moves_stock_and_creates_movements()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Read,
                    InventoryPermissions.Manage
                ]);

        var scenario =
            await CreateTransferScenarioAsync(
                sourceQuantity: 20);

        var request =
            new
            {
                sourceWarehouseId =
                    scenario.SourceWarehouseId,

                sourceLocationId =
                    scenario.SourceLocationId,

                destinationWarehouseId =
                    scenario.DestinationWarehouseId,

                destinationLocationId =
                    scenario.DestinationLocationId,

                productVariantId =
                    scenario.ProductVariantId,

                quantity = 7,

                reason = "Integration test transfer"
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/inventory/transfers/",
                request);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Created);

        var transfer =
            await response.Content
                .ReadFromJsonAsync<WarehouseTransferResponse>();

        transfer.ShouldNotBeNull();

        transfer!.SourceWarehouseId
            .ShouldBe(scenario.SourceWarehouseId);

        transfer.SourceLocationId
            .ShouldBe(scenario.SourceLocationId);

        transfer.DestinationWarehouseId
            .ShouldBe(scenario.DestinationWarehouseId);

        transfer.DestinationLocationId
            .ShouldBe(scenario.DestinationLocationId);

        transfer.ProductVariantId
            .ShouldBe(scenario.ProductVariantId);

        transfer.Quantity
            .ShouldBe(7);

        transfer.Status
            .ShouldBe("Completed");

        transfer.Reason
            .ShouldBe("Integration test transfer");

        transfer.CompletedAt
            .ShouldNotBeNull();

        using var scope =
            factory.Services.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        var sourceStock =
            await db.WarehouseStocks
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.TenantId == "default" &&
                        x.WarehouseId ==
                            scenario.SourceWarehouseId &&
                        x.LocationId ==
                            scenario.SourceLocationId &&
                        x.ProductVariantId ==
                            scenario.ProductVariantId);

        var destinationStock =
            await db.WarehouseStocks
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.TenantId == "default" &&
                        x.WarehouseId ==
                            scenario.DestinationWarehouseId &&
                        x.LocationId ==
                            scenario.DestinationLocationId &&
                        x.ProductVariantId ==
                            scenario.ProductVariantId);

        sourceStock.OnHandQuantity
            .ShouldBe(13);

        sourceStock.AvailableQuantity
            .ShouldBe(13);

        destinationStock.OnHandQuantity
            .ShouldBe(7);

        destinationStock.AvailableQuantity
            .ShouldBe(7);

        var movements =
            await db.WarehouseStockMovements
                .AsNoTracking()
                .Where(
                    x =>
                        x.TenantId == "default" &&
                        x.ReferenceType ==
                            "WarehouseTransfer" &&
                        x.ReferenceId ==
                            transfer.Id.ToString())
                .OrderBy(
                    x => x.QuantityDelta)
                .ToListAsync();

        movements.Count
            .ShouldBe(2);

        movements[0]
            .QuantityDelta
            .ShouldBe(-7);

        movements[0]
            .BalanceAfter
            .ShouldBe(13);

        movements[1]
            .QuantityDelta
            .ShouldBe(7);

        movements[1]
            .BalanceAfter
            .ShouldBe(7);

        var storedTransfer =
            await db.WarehouseTransfers
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        transfer.Id);

        storedTransfer.Status
            .ShouldBe(
                NexaEcommerce.Modules.Inventory.Domain.Entities
                    .WarehouseTransferStatus.Completed);
    }

    [Fact]
    public async Task Create_transfer_fails_when_source_available_stock_is_insufficient()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Manage
                ]);

        var scenario =
            await CreateTransferScenarioAsync(
                sourceQuantity: 5);

        var response =
            await client.PostAsJsonAsync(
                "/api/inventory/transfers/",
                new
                {
                    sourceWarehouseId =
                        scenario.SourceWarehouseId,

                    sourceLocationId =
                        scenario.SourceLocationId,

                    destinationWarehouseId =
                        scenario.DestinationWarehouseId,

                    destinationLocationId =
                        scenario.DestinationLocationId,

                    productVariantId =
                        scenario.ProductVariantId,

                    quantity = 6
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);

        var body =
            await response.Content
                .ReadAsStringAsync();

        body.ShouldContain(
            "Insufficient available source stock");
    }

    [Fact]
    public async Task Create_transfer_rejects_same_location()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Manage
                ]);

        var scenario =
            await CreateTransferScenarioAsync(
                sourceQuantity: 10);

        var response =
            await client.PostAsJsonAsync(
                "/api/inventory/transfers/",
                new
                {
                    sourceWarehouseId =
                        scenario.SourceWarehouseId,

                    sourceLocationId =
                        scenario.SourceLocationId,

                    destinationWarehouseId =
                        scenario.SourceWarehouseId,

                    destinationLocationId =
                        scenario.SourceLocationId,

                    productVariantId =
                        scenario.ProductVariantId,

                    quantity = 1
                });

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);

        var body =
            await response.Content
                .ReadAsStringAsync();

        body.ShouldContain(
            "Source and destination cannot be the same location");
    }

    [Fact]
    public async Task Get_transfers_returns_created_transfer()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Read,
                    InventoryPermissions.Manage
                ]);

        var scenario =
            await CreateTransferScenarioAsync(
                sourceQuantity: 10);

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/inventory/transfers/",
                new
                {
                    sourceWarehouseId =
                        scenario.SourceWarehouseId,

                    sourceLocationId =
                        scenario.SourceLocationId,

                    destinationWarehouseId =
                        scenario.DestinationWarehouseId,

                    destinationLocationId =
                        scenario.DestinationLocationId,

                    productVariantId =
                        scenario.ProductVariantId,

                    quantity = 2,

                    reason = "List test"
                });

        createResponse.StatusCode
            .ShouldBe(HttpStatusCode.Created);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<WarehouseTransferResponse>();

        created.ShouldNotBeNull();

        var response =
            await client.GetAsync(
                "/api/inventory/transfers/?skip=0&take=20");

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<WarehouseTransferListResponse>();

        result.ShouldNotBeNull();

        result!.Items
            .ShouldContain(
                x =>
                    x.Id ==
                    created!.Id);
    }

    [Fact]
    public async Task Get_movements_returns_transfer_movements()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                permissions:
                [
                    InventoryPermissions.Read,
                    InventoryPermissions.Manage
                ]);

        var scenario =
            await CreateTransferScenarioAsync(
                sourceQuantity: 10);

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/inventory/transfers/",
                new
                {
                    sourceWarehouseId =
                        scenario.SourceWarehouseId,

                    sourceLocationId =
                        scenario.SourceLocationId,

                    destinationWarehouseId =
                        scenario.DestinationWarehouseId,

                    destinationLocationId =
                        scenario.DestinationLocationId,

                    productVariantId =
                        scenario.ProductVariantId,

                    quantity = 3
                });

        createResponse.StatusCode
            .ShouldBe(HttpStatusCode.Created);

        var response =
            await client.GetAsync(
                $"/api/inventory/transfers/movements/" +
                $"{scenario.SourceWarehouseId}/" +
                $"{scenario.SourceLocationId}/" +
                $"{scenario.ProductVariantId}" +
                "?skip=0&take=20");

        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<WarehouseMovementListResponse>();

        result.ShouldNotBeNull();

        result!.Items
            .ShouldContain(
                x =>
                    x.Type == "TransferOut" &&
                    x.QuantityDelta == -3);
    }

    private async Task<TransferScenario>
        CreateTransferScenarioAsync(
            int sourceQuantity)
    {
        using var scope =
            factory.Services.CreateScope();

        var catalogDb =
            scope.ServiceProvider
                .GetRequiredService<CatalogDbContext>();

        var variantId =
            await catalogDb.ProductVariants
                .AsNoTracking()
                .Where(
                    x =>
                        x.IsActive &&
                        !x.IsDeleted &&
                        x.Product.IsActive &&
                        x.Product.IsPublished &&
                        !x.Product.IsDeleted)
                .Select(
                    x => x.Id)
                .FirstOrDefaultAsync();

        variantId
            .ShouldNotBe(Guid.Empty);

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        var sourceWarehouse =
            NexaEcommerce.Modules.Inventory.Domain.Entities
                .Warehouse.Create(
                    "default",
                    $"TEST-SRC-{Guid.NewGuid():N}",
                    $"Source {Guid.NewGuid():N}");

        var destinationWarehouse =
            NexaEcommerce.Modules.Inventory.Domain.Entities
                .Warehouse.Create(
                    "default",
                    $"TEST-DST-{Guid.NewGuid():N}",
                    $"Destination {Guid.NewGuid():N}");

        await inventoryDb.Warehouses.AddRangeAsync(
            sourceWarehouse,
            destinationWarehouse);

        var sourceLocation =
            NexaEcommerce.Modules.Inventory.Domain.Entities
                .WarehouseLocation.Create(
                    "default",
                    sourceWarehouse.Id,
                    $"SRC-{Guid.NewGuid():N}",
                    "Source Location");

        var destinationLocation =
            NexaEcommerce.Modules.Inventory.Domain.Entities
                .WarehouseLocation.Create(
                    "default",
                    destinationWarehouse.Id,
                    $"DST-{Guid.NewGuid():N}",
                    "Destination Location");

        await inventoryDb.WarehouseLocations.AddRangeAsync(
            sourceLocation,
            destinationLocation);

        var sourceStock =
            NexaEcommerce.Modules.Inventory.Domain.Entities
                .WarehouseStock.Create(
                    "default",
                    sourceWarehouse.Id,
                    sourceLocation.Id,
                    variantId,
                    sourceQuantity);

        await inventoryDb.WarehouseStocks.AddAsync(
            sourceStock);

        await inventoryDb.SaveChangesAsync();

        return new TransferScenario(
            sourceWarehouse.Id,
            sourceLocation.Id,
            destinationWarehouse.Id,
            destinationLocation.Id,
            variantId);
    }

    private sealed record TransferScenario(
        Guid SourceWarehouseId,
        Guid SourceLocationId,
        Guid DestinationWarehouseId,
        Guid DestinationLocationId,
        Guid ProductVariantId);

    private sealed record WarehouseTransferResponse(
        Guid Id,
        Guid SourceWarehouseId,
        Guid SourceLocationId,
        Guid DestinationWarehouseId,
        Guid DestinationLocationId,
        Guid ProductVariantId,
        int Quantity,
        string Status,
        string? Reason,
        DateTime RequestedAt,
        DateTime? CompletedAt);

    private sealed record WarehouseTransferListResponse(
        int Skip,
        int Take,
        IReadOnlyList<WarehouseTransferResponse> Items);

    private sealed record WarehouseMovementListResponse(
        Guid WarehouseId,
        Guid LocationId,
        Guid ProductVariantId,
        int Skip,
        int Take,
        IReadOnlyList<WarehouseMovementResponse> Items);

    private sealed record WarehouseMovementResponse(
        Guid Id,
        Guid WarehouseId,
        Guid LocationId,
        Guid ProductVariantId,
        string Type,
        int QuantityDelta,
        int BalanceAfter,
        string? ReferenceType,
        string? ReferenceId,
        string? Reason,
        DateTime OccurredAt);
}