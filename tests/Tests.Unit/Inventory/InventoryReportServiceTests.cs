using NSubstitute;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Inventory;

public sealed class InventoryReportServiceTests
{
    private const string TenantId = "default";


private readonly IInventoryRepository _inventoryRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly InventoryReportService _service;

    public InventoryReportServiceTests()
    {
        _inventoryRepository =
            Substitute.For<IInventoryRepository>();

        _warehouseStockRepository =
            Substitute.For<IWarehouseStockRepository>();

        _service =
            new InventoryReportService(
                _inventoryRepository,
                _warehouseStockRepository);
    }

    [Fact]
    public async Task GetSummary_maps_summary_fields_in_correct_order()
    {
        var warehouse1 =
            Guid.NewGuid();

        var warehouse2 =
            Guid.NewGuid();

        var location1 =
            Guid.NewGuid();

        var location2 =
            Guid.NewGuid();

        var variant1 =
            Guid.NewGuid();

        var variant2 =
            Guid.NewGuid();

        var stock1 =
            StockItem.Create(
                TenantId,
                variant1,
                100);

        stock1.Reserve(20);

        var stock2 =
            StockItem.Create(
                TenantId,
                variant2,
                50);

        var warehouseStock1 =
            WarehouseStock.Create(
                TenantId,
                warehouse1,
                location1,
                variant1,
                onHandQuantity: 80,
                reservedQuantity: 10,
                incomingQuantity: 25,
                damagedQuantity: 3,
                reorderPoint: 20);

        var warehouseStock2 =
            WarehouseStock.Create(
                TenantId,
                warehouse2,
                location2,
                variant2,
                onHandQuantity: 50,
                reservedQuantity: 0,
                incomingQuantity: 10,
                damagedQuantity: 2,
                reorderPoint: 5);

        _inventoryRepository
            .GetStocksAsync(
                TenantId,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                stock1,
                stock2
                });

        _warehouseStockRepository
            .GetAllAsync(
                TenantId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                warehouseStock1,
                warehouseStock2
                });

        var result =
            await _service.GetSummaryAsync(
                TenantId);

        result.WarehouseCount
            .ShouldBe(2);

        result.LocationCount
            .ShouldBe(2);

        result.StockRecordCount
            .ShouldBe(2);

        result.LowStockCount
            .ShouldBe(0);

        result.TotalOnHandQuantity
            .ShouldBe(130);

        result.TotalReservedQuantity
            .ShouldBe(20);

        result.TotalIncomingQuantity
            .ShouldBe(35);

        result.TotalDamagedQuantity
            .ShouldBe(5);
    }

    [Fact]
    public async Task GetLowStock_filters_by_warehouse()
    {
        var warehouse1 =
            Guid.NewGuid();

        var warehouse2 =
            Guid.NewGuid();

        var location1 =
            Guid.NewGuid();

        var location2 =
            Guid.NewGuid();

        var variant1 =
            Guid.NewGuid();

        var variant2 =
            Guid.NewGuid();

        var lowStock1 =
            WarehouseStock.Create(
                TenantId,
                warehouse1,
                location1,
                variant1,
                onHandQuantity: 3,
                reorderPoint: 5);

        var lowStock2 =
            WarehouseStock.Create(
                TenantId,
                warehouse2,
                location2,
                variant2,
                onHandQuantity: 2,
                reorderPoint: 5);

        _warehouseStockRepository
            .GetAllAsync(
                TenantId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                lowStock1,
                lowStock2
                });

        var result =
            await _service.GetLowStockAsync(
                TenantId,
                warehouse1,
                0,
                20);

        result.Count
            .ShouldBe(1);

        result[0].WarehouseId
            .ShouldBe(warehouse1);

        result[0].ProductVariantId
            .ShouldBe(variant1);
    }

    [Fact]
    public async Task GetLowStock_applies_skip_and_take_after_sorting()
    {
        var warehouse =
            Guid.NewGuid();

        var location =
            Guid.NewGuid();

        var variant1 =
            Guid.NewGuid();

        var variant2 =
            Guid.NewGuid();

        var variant3 =
            Guid.NewGuid();

        var stock1 =
            WarehouseStock.Create(
                TenantId,
                warehouse,
                location,
                variant1,
                onHandQuantity: 3,
                reorderPoint: 10);

        var stock2 =
            WarehouseStock.Create(
                TenantId,
                warehouse,
                location,
                variant2,
                onHandQuantity: 1,
                reorderPoint: 10);

        var stock3 =
            WarehouseStock.Create(
                TenantId,
                warehouse,
                location,
                variant3,
                onHandQuantity: 5,
                reorderPoint: 10);

        _warehouseStockRepository
            .GetAllAsync(
                TenantId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                stock1,
                stock2,
                stock3
                });

        var result =
            await _service.GetLowStockAsync(
                TenantId,
                null,
                skip: 1,
                take: 1);

        result.Count
            .ShouldBe(1);

        result[0].ProductVariantId
            .ShouldBe(variant1);
    }

    [Fact]
    public async Task GetLowStock_includes_zero_on_hand_stock()
    {
        var warehouse =
            Guid.NewGuid();

        var location =
            Guid.NewGuid();

        var variant =
            Guid.NewGuid();

        var stock =
            WarehouseStock.Create(
                TenantId,
                warehouse,
                location,
                variant,
                onHandQuantity: 0,
                reorderPoint: 5);

        _warehouseStockRepository
            .GetAllAsync(
                TenantId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                stock
                });

        var result =
            await _service.GetLowStockAsync(
                TenantId,
                null,
                0,
                20);

        result.Count
            .ShouldBe(1);

        result[0].IsLowStock
            .ShouldBeTrue();
    }

    [Fact]
    public async Task GetLowStock_rejects_invalid_paging()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () =>
                _service.GetLowStockAsync(
                    TenantId,
                    null,
                    -1,
                    20));

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () =>
                _service.GetLowStockAsync(
                    TenantId,
                    null,
                    0,
                    0));

        await _warehouseStockRepository
            .DidNotReceive()
            .GetAllAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDiscrepancies_calculates_global_and_physical_difference()
    {
        var variant =
            Guid.NewGuid();

        var stock1 =
            StockItem.Create(
                TenantId,
                variant,
                100);

        var warehouse1 =
            WarehouseStock.Create(
                TenantId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant,
                onHandQuantity: 80);

        _inventoryRepository
            .GetStocksAsync(
                TenantId,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                stock1
                });

        _warehouseStockRepository
            .GetAllAsync(
                TenantId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                warehouse1
                });

        var result =
            await _service.GetDiscrepanciesAsync(
                TenantId,
                0,
                20);

        result.Count
            .ShouldBe(1);

        result[0].ProductVariantId
            .ShouldBe(variant);

        result[0].GlobalTotalQuantity
            .ShouldBe(100);

        result[0].PhysicalOnHandQuantity
            .ShouldBe(80);

        result[0].Difference
            .ShouldBe(-20);

        result[0].HasDiscrepancy
            .ShouldBeTrue();
    }

    [Fact]
    public async Task GetDiscrepancies_aggregates_multiple_logical_stock_records()
    {
        var variant =
            Guid.NewGuid();

        var stock1 =
            StockItem.Create(
                TenantId,
                variant,
                40);

        var stock2 =
            StockItem.Create(
                TenantId,
                variant,
                60);

        var warehouse =
            WarehouseStock.Create(
                TenantId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant,
                onHandQuantity: 90);

        _inventoryRepository
            .GetStocksAsync(
                TenantId,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                stock1,
                stock2
                });

        _warehouseStockRepository
            .GetAllAsync(
                TenantId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                warehouse
                });

        var result =
            await _service.GetDiscrepanciesAsync(
                TenantId,
                0,
                20);

        result.Count
            .ShouldBe(1);

        result[0].GlobalTotalQuantity
            .ShouldBe(100);

        result[0].PhysicalOnHandQuantity
            .ShouldBe(90);

        result[0].Difference
            .ShouldBe(-10);
    }

    [Fact]
    public async Task GetDiscrepancies_applies_pagination_after_sorting()
    {
        var variant1 =
            Guid.NewGuid();

        var variant2 =
            Guid.NewGuid();

        var variant3 =
            Guid.NewGuid();

        var stock1 =
            StockItem.Create(
                TenantId,
                variant1,
                100);

        var stock2 =
            StockItem.Create(
                TenantId,
                variant2,
                100);

        var stock3 =
            StockItem.Create(
                TenantId,
                variant3,
                100);

        var warehouse1 =
            WarehouseStock.Create(
                TenantId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant1,
                onHandQuantity: 90);

        var warehouse2 =
            WarehouseStock.Create(
                TenantId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant2,
                onHandQuantity: 40);

        var warehouse3 =
            WarehouseStock.Create(
                TenantId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant3,
                onHandQuantity: 70);

        _inventoryRepository
            .GetStocksAsync(
                TenantId,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                stock1,
                stock2,
                stock3
                });

        _warehouseStockRepository
            .GetAllAsync(
                TenantId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                warehouse1,
                warehouse2,
                warehouse3
                });

        var result =
            await _service.GetDiscrepanciesAsync(
                TenantId,
                skip: 1,
                take: 1);

        result.Count
            .ShouldBe(1);

        result[0].ProductVariantId
            .ShouldBe(variant3);

        result[0].Difference
            .ShouldBe(-30);
    }

    [Fact]
    public async Task GetDiscrepancies_does_not_return_matching_records()
    {
        var variant =
            Guid.NewGuid();

        var stock =
            StockItem.Create(
                TenantId,
                variant,
                100);

        var warehouse =
            WarehouseStock.Create(
                TenantId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant,
                onHandQuantity: 100);

        _inventoryRepository
            .GetStocksAsync(
                TenantId,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                stock
                });

        _warehouseStockRepository
            .GetAllAsync(
                TenantId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                warehouse
                });

        var result =
            await _service.GetDiscrepanciesAsync(
                TenantId,
                0,
                20);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDiscrepancies_rejects_invalid_paging()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () =>
                _service.GetDiscrepanciesAsync(
                    TenantId,
                    -1,
                    20));

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () =>
                _service.GetDiscrepanciesAsync(
                    TenantId,
                    0,
                    0));

        await _inventoryRepository
            .DidNotReceive()
            .GetStocksAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }


}
