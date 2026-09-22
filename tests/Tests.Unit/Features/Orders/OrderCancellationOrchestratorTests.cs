using NexaEcommerce.Modules.Inventory.Domain.Entities;
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

    [Fact]
    public async Task Cancel_releases_warehouse_inventory()
    {
        var order =
            CreateOrder();

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

        repository
            .GetByIdAsync(
                "tenant-1",
                order.Id,
                "user-1",
                Arg.Any<CancellationToken>())
            .Returns(order);

        warehouseReservation
            .ReleaseAsync(
                "tenant-1",
                order.Id,
                Arg.Any<CancellationToken>())
            .Returns(
                new WarehouseReleaseResultDto(
                    order.Id,
                    2,
                    true));

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut =
            new OrderCancellationOrchestrator(
                repository,
                unitOfWork,
                warehouseReservation);

        await sut.CancelAsync(
            "tenant-1",
            "user-1",
            order.Id);

        order.Status
            .ShouldBe(OrderStatus.Cancelled);

        await warehouseReservation
            .Received(1)
            .ReleaseAsync(
                "tenant-1",
                order.Id,
                Arg.Any<CancellationToken>());

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_succeeds_when_order_has_no_active_warehouse_reservation()
    {
        var order =
            CreateOrder();

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

        repository
            .GetByIdAsync(
                "tenant-1",
                order.Id,
                "user-1",
                Arg.Any<CancellationToken>())
            .Returns(order);

        warehouseReservation
            .ReleaseAsync(
                "tenant-1",
                order.Id,
                Arg.Any<CancellationToken>())
            .Returns(
                new WarehouseReleaseResultDto(
                    order.Id,
                    0,
                    false));

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut =
            new OrderCancellationOrchestrator(
                repository,
                unitOfWork,
                warehouseReservation);

        await sut.CancelAsync(
            "tenant-1",
            "user-1",
            order.Id);

        order.Status
            .ShouldBe(OrderStatus.Cancelled);

        await warehouseReservation
            .Received(1)
            .ReleaseAsync(
                "tenant-1",
                order.Id,
                Arg.Any<CancellationToken>());

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
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

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

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
                warehouseReservation);

        await Should.ThrowAsync<KeyNotFoundException>(
            () =>
                sut.CancelAsync(
                    "tenant-1",
                    "missing-user",
                    orderId));

        await warehouseReservation
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
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

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

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
                warehouseReservation);

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

        await warehouseReservation
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_rejects_delivered_order()
    {
        var order =
            CreateOrder();

        order.MarkPaid();
        order.StartProcessing();
        order.MarkShipped();
        order.MarkDelivered();

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

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
                warehouseReservation);

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
            .ShouldBe(OrderStatus.Delivered);

        await warehouseReservation
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_does_not_save_when_warehouse_release_fails()
    {
        var order =
            CreateOrder();

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

        repository
            .GetByIdAsync(
                "tenant-1",
                order.Id,
                "user-1",
                Arg.Any<CancellationToken>())
            .Returns(order);

        warehouseReservation
            .ReleaseAsync(
                "tenant-1",
                order.Id,
                Arg.Any<CancellationToken>())
            .Returns<Task<WarehouseReleaseResultDto>>(
                _ =>
                    Task.FromException<
                        WarehouseReleaseResultDto>(
                        new InvalidOperationException(
                            "Warehouse stock was not found while releasing the reservation.")));

        var sut =
            new OrderCancellationOrchestrator(
                repository,
                unitOfWork,
                warehouseReservation);

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
                "Warehouse stock was not found while releasing the reservation.");

        order.Status
            .ShouldNotBe(OrderStatus.Cancelled);

        await unitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }
}
