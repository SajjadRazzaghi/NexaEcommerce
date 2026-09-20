using NSubstitute;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Inventory;

public sealed class InventoryStockReaderTests
{
    [Fact]
    public async Task GetAvailableQuantity_returns_sum_of_physical_available_quantity()
    {
        var repository =
            Substitute.For<IWarehouseStockRepository>();

        var variantId =
            Guid.NewGuid();

        var warehouse1 =
            Guid.NewGuid();

        var warehouse2 =
            Guid.NewGuid();

        var location1 =
            Guid.NewGuid();

        var location2 =
            Guid.NewGuid();

        var first =
            WarehouseStock.Create(
                "default",
                warehouse1,
                location1,
                variantId,
                onHandQuantity: 10);

        first.Reserve(3);

        var second =
            WarehouseStock.Create(
                "default",
                warehouse2,
                location2,
                variantId,
                onHandQuantity: 5);

        repository
            .GetAllAsync(
                "default",
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                [first, second]);

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.GetAvailableQuantityAsync(
                "default",
                variantId);

        result.ShouldBe(12);
    }

    [Fact]
    public async Task GetAvailableQuantity_returns_null_when_variant_has_no_physical_stock_record()
    {
        var repository =
            Substitute.For<IWarehouseStockRepository>();

        repository
            .GetAllAsync(
                "default",
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                Array.Empty<WarehouseStock>());

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.GetAvailableQuantityAsync(
                "default",
                Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task IsInStock_returns_true_when_any_physical_available_quantity_is_positive()
    {
        var repository =
            Substitute.For<IWarehouseStockRepository>();

        var variantId =
            Guid.NewGuid();

        var stock =
            WarehouseStock.Create(
                "default",
                Guid.NewGuid(),
                Guid.NewGuid(),
                variantId,
                onHandQuantity: 5);

        repository
            .GetAllAsync(
                "default",
                true,
                Arg.Any<CancellationToken>())
            .Returns([stock]);

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
            Substitute.For<IWarehouseStockRepository>();

        var variantId =
            Guid.NewGuid();

        var stock =
            WarehouseStock.Create(
                "default",
                Guid.NewGuid(),
                Guid.NewGuid(),
                variantId,
                onHandQuantity: 0);

        repository
            .GetAllAsync(
                "default",
                true,
                Arg.Any<CancellationToken>())
            .Returns([stock]);

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.IsInStockAsync(
                "default",
                variantId);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAvailableQuantities_returns_aggregated_quantities_for_multiple_variants()
    {
        var repository =
            Substitute.For<IWarehouseStockRepository>();

        var variant1 =
            Guid.NewGuid();

        var variant2 =
            Guid.NewGuid();

        var variant1WarehouseA =
            WarehouseStock.Create(
                "default",
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant1,
                onHandQuantity: 5);

        var variant1WarehouseB =
            WarehouseStock.Create(
                "default",
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant1,
                onHandQuantity: 4);

        var variant2Stock =
            WarehouseStock.Create(
                "default",
                Guid.NewGuid(),
                Guid.NewGuid(),
                variant2,
                onHandQuantity: 9);

        repository
            .GetAllAsync(
                "default",
                true,
                Arg.Any<CancellationToken>())
            .Returns(
                [
                    variant1WarehouseA,
                    variant1WarehouseB,
                    variant2Stock
                ]);

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.GetAvailableQuantitiesAsync(
                "default",
                [variant1, variant2]);

        result.Count.ShouldBe(2);
        result[variant1].ShouldBe(9);
        result[variant2].ShouldBe(9);
    }

    [Fact]
    public async Task GetAvailableQuantities_ignores_unknown_variants()
    {
        var repository =
            Substitute.For<IWarehouseStockRepository>();

        var knownVariant =
            Guid.NewGuid();

        var unknownVariant =
            Guid.NewGuid();

        var knownStock =
            WarehouseStock.Create(
                "default",
                Guid.NewGuid(),
                Guid.NewGuid(),
                knownVariant,
                onHandQuantity: 4);

        repository
            .GetAllAsync(
                "default",
                true,
                Arg.Any<CancellationToken>())
            .Returns([knownStock]);

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.GetAvailableQuantitiesAsync(
                "default",
                [knownVariant, unknownVariant]);

        result.Count.ShouldBe(1);
        result[knownVariant].ShouldBe(4);
        result.ContainsKey(unknownVariant).ShouldBeFalse();
    }

    [Fact]
    public async Task GetAvailableQuantities_with_empty_input_returns_empty_dictionary_without_repository_access()
    {
        var repository =
            Substitute.For<IWarehouseStockRepository>();

        var reader =
            new InventoryStockReader(repository);

        var result =
            await reader.GetAvailableQuantitiesAsync(
                "default",
                Array.Empty<Guid>());

        result.ShouldBeEmpty();

        await repository
            .DidNotReceive()
            .GetAllAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
    }
}
