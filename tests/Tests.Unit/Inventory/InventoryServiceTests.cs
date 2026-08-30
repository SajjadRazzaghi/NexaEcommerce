using NSubstitute;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Domain.Interfaces;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Inventory;

public sealed class InventoryServiceTests
{
    private readonly IInventoryRepository _repository;
    private readonly IInventoryUnitOfWork _unitOfWork;
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        _repository =
            Substitute.For<IInventoryRepository>();

        _unitOfWork =
            Substitute.For<IInventoryUnitOfWork>();

        _service =
      new InventoryService(
          _repository,
          _unitOfWork);
    }

    [Fact]
    public async Task GetStock_returns_null_when_stock_does_not_exist()
    {
        var variantId = Guid.NewGuid();

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);

        var result =
            await _service.GetStockAsync(
                "default",
                variantId);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetStock_maps_existing_stock()
    {
        var variantId = Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                10);

        stock.Reserve(3);

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var result =
            await _service.GetStockAsync(
                "default",
                variantId);

        result.ShouldNotBeNull();
        result!.ProductVariantId
            .ShouldBe(variantId);

        result.AvailableQuantity
            .ShouldBe(7);

        result.ReservedQuantity
            .ShouldBe(3);

        result.TotalQuantity
            .ShouldBe(10);
    }

    [Fact]
    public async Task SetStock_creates_stock_when_it_does_not_exist()
    {
        var variantId = Guid.NewGuid();

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);

        var result =
            await _service.SetStockAsync(
                "default",
                variantId,
                25);

        result.ProductVariantId
            .ShouldBe(variantId);

        result.AvailableQuantity
            .ShouldBe(25);

        result.ReservedQuantity
            .ShouldBe(0);

        await _repository
            .Received(1)
            .AddStockAsync(
                Arg.Is<StockItem>(
                    x =>
                        x.ProductVariantId == variantId &&
                        x.AvailableQuantity == 25),
                Arg.Any<CancellationToken>());

        await _unitOfWork
            .Received(1)
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetStock_updates_total_stock_without_touching_reserved_stock()
    {
        var variantId = Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                10);

        stock.Reserve(4);

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var result =
            await _service.SetStockAsync(
                "default",
                variantId,
                12);

        result.AvailableQuantity
            .ShouldBe(8);

        result.ReservedQuantity
            .ShouldBe(4);

        result.TotalQuantity
            .ShouldBe(12);
    }

    [Fact]
    public async Task SetStock_rejects_quantity_lower_than_reserved_stock()
    {
        var variantId = Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                10);

        stock.Reserve(6);

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    _service.SetStockAsync(
                        "default",
                        variantId,
                        5));

        exception.Message
            .ShouldContain(
                "cannot be lower than reserved quantity");
    }

    [Fact]
    public async Task SetStock_rejects_negative_quantity()
    {
        var exception =
            await Should.ThrowAsync<ArgumentOutOfRangeException>(
                () =>
                    _service.SetStockAsync(
                        "default",
                        Guid.NewGuid(),
                        -1));

        _repository
            .DidNotReceive()
            .GetStockAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjustStock_creates_stock_when_positive_quantity_is_requested()
    {
        var variantId = Guid.NewGuid();

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);

        var result =
            await _service.AdjustStockAsync(
                "default",
                variantId,
                7);

        result.AvailableQuantity
            .ShouldBe(7);

        result.ReservedQuantity
            .ShouldBe(0);

        await _repository
            .Received(1)
            .AddStockAsync(
                Arg.Any<StockItem>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjustStock_cannot_reduce_non_existing_stock()
    {
        var variantId = Guid.NewGuid();

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    _service.AdjustStockAsync(
                        "default",
                        variantId,
                        -1));

        exception.Message
            .ShouldContain(
                "Cannot reduce stock that does not exist");
    }

    [Fact]
    public async Task Reserve_creates_active_reservation_and_updates_stock()
    {
        var variantId = Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                10);

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var result =
            await _service.ReserveAsync(
                "default",
                variantId,
                3,
                "reservation-1",
                TimeSpan.FromMinutes(10));

        result.ReservationKey
            .ShouldBe("reservation-1");

        result.ProductVariantId
            .ShouldBe(variantId);

        result.Quantity
            .ShouldBe(3);

        result.Status
            .ShouldBe("Active");

        stock.AvailableQuantity
            .ShouldBe(7);

        stock.ReservedQuantity
            .ShouldBe(3);

        await _repository
            .Received(1)
            .AddReservationAsync(
                Arg.Is<StockReservation>(
                    x =>
                        x.ReservationKey == "reservation-1" &&
                        x.ProductVariantId == variantId &&
                        x.Quantity == 3),
                Arg.Any<CancellationToken>());

        await _unitOfWork
            .Received(1)
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reserve_returns_existing_reservation_for_same_key_and_payload()
    {
        var variantId = Guid.NewGuid();

        var existing =
            StockReservation.Create(
                "default",
                "reservation-1",
                variantId,
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result =
            await _service.ReserveAsync(
                "default",
                variantId,
                2,
                "reservation-1",
                TimeSpan.FromMinutes(20));

        result.ReservationKey
            .ShouldBe("reservation-1");

        result.Quantity
            .ShouldBe(2);

        await _repository
            .DidNotReceive()
            .AddReservationAsync(
                Arg.Any<StockReservation>(),
                Arg.Any<CancellationToken>());

        await _unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reserve_rejects_reuse_of_key_with_different_quantity()
    {
        var variantId = Guid.NewGuid();

        var existing =
            StockReservation.Create(
                "default",
                "reservation-1",
                variantId,
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    _service.ReserveAsync(
                        "default",
                        variantId,
                        3,
                        "reservation-1",
                        TimeSpan.FromMinutes(10)));

        exception.Message
            .ShouldContain(
                "already used");
    }

    [Fact]
    public async Task Reserve_fails_when_stock_record_does_not_exist()
    {
        var variantId = Guid.NewGuid();

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);

        _repository
            .GetStockAsync(
                "default",
                variantId,
                Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    _service.ReserveAsync(
                        "default",
                        variantId,
                        2,
                        "reservation-1",
                        TimeSpan.FromMinutes(10)));

        exception.Message
            .ShouldContain(
                "Stock record was not found");
    }

    [Fact]
    public async Task Release_returns_released_reservation_and_restores_stock()
    {
        var variantId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                6);

        stock.Reserve(4);

        var reservation =
            StockReservation.Create(
                "default",
                "reservation-1",
                variantId,
                stock.Id,
                4,
                DateTimeOffset.UtcNow.AddMinutes(10));

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns(reservation);

        _repository
            .GetStockByIdAsync(
                "default",
                reservation.StockItemId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var result =
            await _service.ReleaseAsync(
                "default",
                "reservation-1");

        result.Status
            .ShouldBe("Released");

        stock.AvailableQuantity
      .ShouldBe(6);

        stock.ReservedQuantity
            .ShouldBe(0);
    }

    [Fact]
    public async Task Release_is_idempotent_for_already_released_reservation()
    {
        var variantId = Guid.NewGuid();

        var reservation =
            StockReservation.Create(
                "default",
                "reservation-1",
                variantId,
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        reservation.MarkReleased();

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns(reservation);

        var result =
            await _service.ReleaseAsync(
                "default",
                "reservation-1");

        result.Status
            .ShouldBe("Released");

        await _repository
            .DidNotReceive()
            .GetStockByIdAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Release_fails_when_reservation_does_not_exist()
    {
        _repository
            .GetReservationAsync(
                "default",
                "missing",
                Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);

        await Should.ThrowAsync<KeyNotFoundException>(
            () =>
                _service.ReleaseAsync(
                    "default",
                    "missing"));
    }

    [Fact]
    public async Task Commit_commits_active_reservation_and_removes_reserved_stock()
    {
        var variantId = Guid.NewGuid();

        var stock =
            StockItem.Create(
                "default",
                variantId,
                6);

        stock.Reserve(4);

        var reservation =
            StockReservation.Create(
                "default",
                "reservation-1",
                variantId,
                stock.Id,
                4,
                DateTimeOffset.UtcNow.AddMinutes(10));

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns(reservation);

        _repository
            .GetStockByIdAsync(
                "default",
                reservation.StockItemId,
                Arg.Any<CancellationToken>())
            .Returns(stock);

        var result =
            await _service.CommitAsync(
                "default",
                "reservation-1");

        result.Status
            .ShouldBe("Committed");

        stock.AvailableQuantity
    .ShouldBe(2);

        stock.ReservedQuantity
            .ShouldBe(0);
    }

    [Fact]
    public async Task Commit_rejects_expired_reservation()
    {
        var variantId = Guid.NewGuid();

        var reservation =
            StockReservation.Create(
                "default",
                "reservation-1",
                variantId,
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(-1));

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns(reservation);

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    _service.CommitAsync(
                        "default",
                        "reservation-1"));

        exception.Message
            .ShouldContain(
                "Expired reservation cannot be committed");
    }

    [Fact]
    public async Task Commit_is_idempotent_for_already_committed_reservation()
    {
        var variantId = Guid.NewGuid();

        var reservation =
            StockReservation.Create(
                "default",
                "reservation-1",
                variantId,
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        reservation.MarkCommitted();

        _repository
            .GetReservationAsync(
                "default",
                "reservation-1",
                Arg.Any<CancellationToken>())
            .Returns(reservation);

        var result =
            await _service.CommitAsync(
                "default",
                "reservation-1");

        result.Status
            .ShouldBe("Committed");

        await _repository
            .DidNotReceive()
            .GetStockByIdAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }
}

