using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Inventory;

public sealed class WarehouseStockReaderTests
{
    [Fact]
    public async Task Returns_sum_of_available_stock_across_warehouses()
    {
        var options =
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        await using var db =
            new InventoryDbContext(options);

        var variantId = Guid.NewGuid();

        var warehouse1 =
            Warehouse.Create(
                "default",
                "WH-01",
                "Warehouse 1");

        var warehouse2 =
            Warehouse.Create(
                "default",
                "WH-02",
                "Warehouse 2");

        var location1 =
            WarehouseLocation.Create(
                "default",
                warehouse1.Id,
                "LOC-01",
                "Location 1");

        var location2 =
            WarehouseLocation.Create(
                "default",
                warehouse2.Id,
                "LOC-02",
                "Location 2");

        var stock1 =
            WarehouseStock.Create(
                "default",
                warehouse1.Id,
                location1.Id,
                variantId,
                10);

        var stock2 =
            WarehouseStock.Create(
                "default",
                warehouse2.Id,
                location2.Id,
                variantId,
                15);

        stock1.Reserve(3);
        stock2.Reserve(2);

        await db.Warehouses.AddRangeAsync(
            warehouse1,
            warehouse2);

        await db.WarehouseLocations.AddRangeAsync(
            location1,
            location2);

        await db.WarehouseStocks.AddRangeAsync(
            stock1,
            stock2);

        await db.SaveChangesAsync();

        var reader =
            new WarehouseStockReader(db);

        var result =
            await reader.GetAvailableQuantityAsync(
                "default",
                variantId);

        result.ShouldBe(20);
    }

    [Fact]
    public async Task Ignores_stock_from_other_tenant()
    {
        var options =
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        await using var db =
            new InventoryDbContext(options);

        var variantId = Guid.NewGuid();

        var warehouse =
            Warehouse.Create(
                "other",
                "WH-01",
                "Other Warehouse");

        var location =
            WarehouseLocation.Create(
                "other",
                warehouse.Id,
                "LOC-01",
                "Other Location");

        var stock =
            WarehouseStock.Create(
                "other",
                warehouse.Id,
                location.Id,
                variantId,
                100);

        await db.Warehouses.AddAsync(warehouse);
        await db.WarehouseLocations.AddAsync(location);
        await db.WarehouseStocks.AddAsync(stock);

        await db.SaveChangesAsync();

        var reader =
            new WarehouseStockReader(db);

        var result =
            await reader.GetAvailableQuantityAsync(
                "default",
                variantId);

        result.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_zero_when_variant_has_no_stock()
    {
        var options =
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        await using var db =
            new InventoryDbContext(options);

        var reader =
            new WarehouseStockReader(db);

        var result =
            await reader.GetAvailableQuantityAsync(
                "default",
                Guid.NewGuid());

        result.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_available_quantities_for_multiple_variants()
    {
        var options =
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        await using var db =
            new InventoryDbContext(options);

        var variant1 = Guid.NewGuid();
        var variant2 = Guid.NewGuid();

        var warehouse =
            Warehouse.Create(
                "default",
                "WH-01",
                "Warehouse 1");

        var location =
            WarehouseLocation.Create(
                "default",
                warehouse.Id,
                "LOC-01",
                "Location 1");

        var stock1 =
            WarehouseStock.Create(
                "default",
                warehouse.Id,
                location.Id,
                variant1,
                20);

        var stock2 =
            WarehouseStock.Create(
                "default",
                warehouse.Id,
                location.Id,
                variant2,
                30);

        stock1.Reserve(5);
        stock2.Reserve(10);

        await db.Warehouses.AddAsync(warehouse);
        await db.WarehouseLocations.AddAsync(location);
        await db.WarehouseStocks.AddRangeAsync(
            stock1,
            stock2);

        await db.SaveChangesAsync();

        var reader =
            new WarehouseStockReader(db);

        var result =
            await reader.GetAvailableQuantitiesAsync(
                "default",
                new[]
                {
                    variant1,
                    variant2
                });

        result.ShouldContainKey(variant1);
        result.ShouldContainKey(variant2);

        result[variant1].ShouldBe(15);
        result[variant2].ShouldBe(20);
    }

    [Fact]
    public async Task GetAvailableQuantitiesAsync_ignores_other_tenant()
    {
        var options =
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        await using var db =
            new InventoryDbContext(options);

        var variantId = Guid.NewGuid();

        var warehouse =
            Warehouse.Create(
                "other",
                "WH-01",
                "Other Warehouse");

        var location =
            WarehouseLocation.Create(
                "other",
                warehouse.Id,
                "LOC-01",
                "Other Location");

        var stock =
            WarehouseStock.Create(
                "other",
                warehouse.Id,
                location.Id,
                variantId,
                100);

        await db.Warehouses.AddAsync(warehouse);
        await db.WarehouseLocations.AddAsync(location);
        await db.WarehouseStocks.AddAsync(stock);

        await db.SaveChangesAsync();

        var reader =
            new WarehouseStockReader(db);

        var result =
            await reader.GetAvailableQuantitiesAsync(
                "default",
                new[]
                {
                    variantId
                });

        result.ShouldBeEmpty();
    }
}