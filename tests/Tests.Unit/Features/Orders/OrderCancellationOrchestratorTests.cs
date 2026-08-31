using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaECommerce.Server.Features.Orders;
using NSubstitute;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Features.Orders;

public sealed class OrderCancellationOrchestratorTests
{
    private static Order CreateOrder()
    {
        return Order.Create(
            "tenant-1",
            "user-1",
            "NX-TEST-001",
            Guid.NewGuid().ToString("N"),
            "IRR",
            0,
            0,
            0,
            "Test User",
            "09120000000",
            "Test Address",
            "Tehran",
            "1234567890");
    }

    private static StockReservationDto CreateReservationDto(
        string reservationKey,
        Guid productVariantId,
        int quantity)
    {
        return new StockReservationDto(
            reservationKey,
            productVariantId,
            quantity,
            "Released",
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Cancel_releases_reserved_inventory()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        var reservation =
            order.AddInventoryReservation(
                "checkout:tenant-1:user-1:key-1:" +
                variantId.ToString("N"),
                variantId,
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        repository
            .GetByIdAsync(
                "tenant-1",
                order.Id,
                "user-1",
                Arg.Any<CancellationToken>())
            .Returns(order);

        inventory
            .ReleaseAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    CreateReservationDto(
                        reservation.ReservationKey,
                        variantId,
                        2)));

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut =
            new OrderCancellationOrchestrator(
                repository,
                unitOfWork,
                inventory);

        await sut.CancelAsync(
            "tenant-1",
            "user-1",
            order.Id);

        order.Status
            .ShouldBe(OrderStatus.Cancelled);

        reservation.Status
            .ShouldBe(
                InventoryReservationStatus.Released);

        await inventory
            .Received(1)
            .ReleaseAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>());

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_does_not_release_committed_inventory()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        var reservation =
            order.AddInventoryReservation(
                "checkout:tenant-1:user-1:key-2:" +
                variantId.ToString("N"),
                variantId,
                1,
                DateTimeOffset.UtcNow.AddMinutes(10));

        reservation.MarkCommitted();

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        repository
            .GetByIdAsync(
                "tenant-1",
                order.Id,
                "user-1",
                Arg.Any<CancellationToken>())
            .Returns(order);

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut =
            new OrderCancellationOrchestrator(
                repository,
                unitOfWork,
                inventory);

        await sut.CancelAsync(
            "tenant-1",
            "user-1",
            order.Id);

        order.Status
            .ShouldBe(OrderStatus.Cancelled);

        reservation.Status
            .ShouldBe(
                InventoryReservationStatus.Committed);

        await inventory
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

 
[Fact]
public async Task Cancel_throws_when_order_does_not_exist()
    {
        var orderId =
            Guid.NewGuid();

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        repository
            .GetByIdAsync(
                "tenant-1",
                orderId,
                "missing-user",
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<Order?>(null));

        var sut =
            new OrderCancellationOrchestrator(
                repository,
                unitOfWork,
                inventory);

        await Should.ThrowAsync<KeyNotFoundException>(
            () =>
                sut.CancelAsync(
                    "tenant-1",
                    "missing-user",
                    orderId));
    }
    [Fact]
    public async Task Cancel_rejects_shipped_order()
    {
        var order =
            CreateOrder();

        order.MarkPaid();
        order.StartProcessing();
        order.MarkShipped();

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        repository
            .GetByIdAsync(
                "tenant-1",
                order.Id,
                "user-1",
                Arg.Any<CancellationToken>())
            .Returns(order);

        var sut =
            new OrderCancellationOrchestrator(
                repository,
                unitOfWork,
                inventory);

        var exception =
            await Should.ThrowAsync<
                InvalidOperationException>(
                () =>
                    sut.CancelAsync(
                        "tenant-1",
                        "user-1",
                        order.Id));

        exception.Message
            .ShouldBe(
                "Shipped or delivered orders cannot be cancelled.");

        order.Status
            .ShouldBe(OrderStatus.Shipped);

        await inventory
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }
}
