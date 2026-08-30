
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Inventory;

public sealed class StockItemTests
{
    [Fact]
    public void Create_initializes_stock_correctly()
    {
        var productVariantId = Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                productVariantId,
                10);

        stock.TenantId
            .ShouldBe("default");

        stock.ProductVariantId
            .ShouldBe(productVariantId);

        stock.AvailableQuantity
            .ShouldBe(10);

        stock.ReservedQuantity
            .ShouldBe(0);

        stock.TotalQuantity
            .ShouldBe(10);

        stock.Version
            .ShouldBe(1);
    }

    [Fact]
    public void Add_increases_available_quantity()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        stock.Add(5);

        stock.AvailableQuantity
            .ShouldBe(15);

        stock.ReservedQuantity
            .ShouldBe(0);

        stock.TotalQuantity
            .ShouldBe(15);

        stock.Version
            .ShouldBe(2);
    }

    [Fact]
    public void Add_rejects_non_positive_quantity()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Add(0));

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Add(-1));
    }

    [Fact]
    public void Remove_decreases_available_quantity()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        stock.Remove(4);

        stock.AvailableQuantity
            .ShouldBe(6);

        stock.ReservedQuantity
            .ShouldBe(0);

        stock.TotalQuantity
            .ShouldBe(6);

        stock.Version
            .ShouldBe(2);
    }

    [Fact]
    public void Remove_more_than_available_stock_fails()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                5);

        var exception =
            Should.Throw<InvalidOperationException>(
                () => stock.Remove(6));

        exception.Message
            .ShouldContain(
                "Insufficient available stock");
    }

    [Fact]
    public void Reserve_moves_stock_from_available_to_reserved()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        stock.Reserve(3);

        stock.AvailableQuantity
            .ShouldBe(7);

        stock.ReservedQuantity
            .ShouldBe(3);

        stock.TotalQuantity
            .ShouldBe(10);

        stock.Version
            .ShouldBe(2);
    }

    [Fact]
    public void Reserve_more_than_available_stock_fails()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                3);

        var exception =
            Should.Throw<InvalidOperationException>(
                () => stock.Reserve(4));

        exception.Message
            .ShouldContain(
                "Insufficient available stock");

        stock.AvailableQuantity
            .ShouldBe(3);

        stock.ReservedQuantity
            .ShouldBe(0);
    }

    [Fact]
    public void Release_returns_reserved_stock_to_available()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        stock.Reserve(4);
        stock.Release(4);

        stock.AvailableQuantity
            .ShouldBe(10);

        stock.ReservedQuantity
            .ShouldBe(0);

        stock.TotalQuantity
            .ShouldBe(10);

        stock.Version
            .ShouldBe(3);
    }

    [Fact]
    public void Release_more_than_reserved_stock_fails()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        stock.Reserve(2);

        var exception =
            Should.Throw<InvalidOperationException>(
                () => stock.Release(3));

        exception.Message
            .ShouldContain(
                "Cannot release more than reserved stock");

        stock.AvailableQuantity
            .ShouldBe(8);

        stock.ReservedQuantity
            .ShouldBe(2);
    }

    [Fact]
    public void Commit_removes_stock_from_reserved_quantity()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        stock.Reserve(4);
        stock.Commit(4);

        stock.AvailableQuantity
            .ShouldBe(6);

        stock.ReservedQuantity
            .ShouldBe(0);

        stock.TotalQuantity
            .ShouldBe(6);

        stock.Version
            .ShouldBe(3);
    }

    [Fact]
    public void Commit_more_than_reserved_stock_fails()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        stock.Reserve(2);

        var exception =
            Should.Throw<InvalidOperationException>(
                () => stock.Commit(3));

        exception.Message
            .ShouldContain(
                "Cannot commit more than reserved stock");

        stock.AvailableQuantity
            .ShouldBe(8);

        stock.ReservedQuantity
            .ShouldBe(2);
    }

    [Fact]
    public void All_stock_operations_reject_non_positive_quantity()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                10);

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Remove(0));

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Remove(-1));

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Reserve(0));

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Reserve(-1));

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Release(0));

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Release(-1));

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Commit(0));

        Should.Throw<ArgumentOutOfRangeException>(
            () => stock.Commit(-1));
    }

    [Fact]
    public void Total_quantity_remains_constant_when_stock_is_reserved_and_released()
    {
        var stock =
            StockItem.Create(
                "default",
                Guid.NewGuid(),
                20);

        stock.Reserve(7);

        stock.TotalQuantity
            .ShouldBe(20);

        stock.Release(7);

        stock.TotalQuantity
            .ShouldBe(20);

        stock.AvailableQuantity
            .ShouldBe(20);

        stock.ReservedQuantity
            .ShouldBe(0);
    }
}