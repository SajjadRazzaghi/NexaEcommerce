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
            new InventoryStockReader(
                repository);

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
            new InventoryStockReader(
                repository);

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
            new InventoryStockReader(
                repository);

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
            new InventoryStockReader(
                repository);

        var result =
            await reader.IsInStockAsync(
                "default",
                variantId);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAvailableQuantities_returns_quantities_for_multiple_variants()
    {
        var repository =
            Substitute.For<IInventoryRepository>();

        var variant1 =
            Guid.NewGuid();

        var variant2 =
            Guid.NewGuid();

        repository
            .GetStockAsync(
                "default",
                variant1,
                Arg.Any<CancellationToken>())
            .Returns(
                StockItem.Create(
                    "default",
                    variant1,
                    5));

        repository
            .GetStockAsync(
                "default",
                variant2,
                Arg.Any<CancellationToken>())
            .Returns(
                StockItem.Create(
                    "default",
                    variant2,
                    9));

        var reader =
            new InventoryStockReader(
                repository);

        var result =
            await reader.GetAvailableQuantitiesAsync(
                "default",
                [
                    variant1,
                    variant2
                ]);

        result.Count
            .ShouldBe(2);

        result[variant1]
            .ShouldBe(5);

        result[variant2]
            .ShouldBe(9);
    }

    [Fact]
    public async Task GetAvailableQuantities_ignores_unknown_variants()
    {
        var repository =
            Substitute.For<IInventoryRepository>();

        var knownVariant =
            Guid.NewGuid();

        var unknownVariant =
            Guid.NewGuid();

        repository
            .GetStockAsync(
                "default",
                knownVariant,
                Arg.Any<CancellationToken>())
            .Returns(
                StockItem.Create(
                    "default",
                    knownVariant,
                    4));

        repository
            .GetStockAsync(
                "default",
                unknownVariant,
                Arg.Any<CancellationToken>())
            .Returns(
                (StockItem?)null);

        var reader =
            new InventoryStockReader(
                repository);

        var result =
            await reader.GetAvailableQuantitiesAsync(
                "default",
                [
                    knownVariant,
                    unknownVariant
                ]);

        result.Count
            .ShouldBe(1);

        result[knownVariant]
            .ShouldBe(4);

        result.ContainsKey(
            unknownVariant)
            .ShouldBeFalse();
    }

    [Fact]
    public async Task GetAvailableQuantities_with_empty_input_returns_empty_dictionary()
    {
        var repository =
            Substitute.For<IInventoryRepository>();

        var reader =
            new InventoryStockReader(
                repository);

        var result =
            await reader.GetAvailableQuantitiesAsync(
                "default",
                Array.Empty<Guid>());

        result.ShouldBeEmpty();

        await repository
            .DidNotReceive()
            .GetStockAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }
}

