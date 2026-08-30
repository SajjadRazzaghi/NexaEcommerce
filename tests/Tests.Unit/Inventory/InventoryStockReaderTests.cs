using NSubstitute;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Inventory;

public sealed class InventoryStockReaderTests
{
    [Fact]
    public async Task GetAvailableQuantity_returns_inventory_available_quantity()
    {
        var repository =
            Substitute.For<IInventoryRepository>();

        var variantId =
            Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                10);

        stock.Reserve(3);

        repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.GetAvailableQuantityAsync(
                "default",
                variantId);

        result.ShouldBe(7);
    }

    [Fact]
    public async Task GetAvailableQuantity_returns_null_when_stock_does_not_exist()
    {
        var repository =
            Substitute.For<IInventoryRepository>();

        var variantId =
            Guid.NewGuid();

        repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.GetAvailableQuantityAsync(
                "default",
                variantId);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task IsInStock_returns_true_when_available_quantity_is_positive()
    {
        var repository =
            Substitute.For<IInventoryRepository>();

        var variantId =
            Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                5);

        repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.IsInStockAsync(
                "default",
                variantId);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsInStock_returns_false_when_available_quantity_is_zero()
    {
        var repository =
            Substitute.For<IInventoryRepository>();

        var variantId =
            Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                0);

        repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.IsInStockAsync(
                "default",
                variantId);

        result.ShouldBeFalse();
    }
}