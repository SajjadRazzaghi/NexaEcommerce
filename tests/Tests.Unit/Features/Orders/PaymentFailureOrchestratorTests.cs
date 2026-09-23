using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaECommerce.Server.Features.Orders;
using NSubstitute;
using Shouldly;
using System.Net.NetworkInformation;

namespace NexaECommerce.Tests.Unit.Features.Orders;

public sealed class PaymentFailureOrchestratorTests
{
    private static Order CreateOrder()
    {
        return Order.Create(
            "tenant-1",
            "user-1",
            "NX-PAY-FAIL-001",
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

    private static PaymentFailureOrchestrator CreateSut(
        IPaymentAttemptService paymentAttempts,
        IPaymentAttemptRepository paymentAttemptRepository,
        IOrderRepository orderRepository,
        IOrderUnitOfWork orderUnitOfWork,
        IInventoryService inventory,
        IWarehouseReservationOrchestrator warehouseReservation)
    {
        return new PaymentFailureOrchestrator(
            paymentAttempts,
            paymentAttemptRepository,
            orderRepository,
            orderUnitOfWork,
            inventory,
            warehouseReservation,
            NullLogger<PaymentFailureOrchestrator>.Instance);
    }

    [Fact]
    public async Task Failed_payment_releases_active_inventory_reservations()
    {
        var order = CreateOrder();

        var variantId = Guid.NewGuid();

        var reservation = order.AddInventoryReservation(
            "reservation-payment-failure",
            variantId,
            2,
            DateTimeOffset.UtcNow.AddMinutes(10));

        var paymentAttempt = PaymentAttempt.Create(
            order.Id,
            "tenant-1",
            "user-1",
            Guid.NewGuid().ToString("N"),
            200000,
            "IRR");

        var paymentAttempts =
            Substitute.For<IPaymentAttemptService>();

        var paymentAttemptRepository =
            Substitute.For<IPaymentAttemptRepository>();

        var orderRepository =
            Substitute.For<IOrderRepository>();

        var orderUnitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

        paymentAttemptRepository
            .GetByIdAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                Arg.Any<CancellationToken>())
            .Returns(paymentAttempt);

        orderRepository
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

        paymentAttempts
            .MarkFailedAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                "DECLINED",
                "Gateway declined payment",
                Arg.Any<CancellationToken>())
            .Returns(
                new PaymentAttemptDto(
                    paymentAttempt.Id,
                    order.Id,
                    "Failed",
                    200000,
                    "IRR",
                    null,
                    null,
                    "DECLINED",
                    "Gateway declined payment",
                    paymentAttempt.CreatedAt,
                    DateTimeOffset.UtcNow));

        orderUnitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut = CreateSut(
            paymentAttempts,
            paymentAttemptRepository,
            orderRepository,
            orderUnitOfWork,
            inventory,
            warehouseReservation);

        var result = await sut.FailAsync(
            "tenant-1",
            "user-1",
            paymentAttempt.Id,
            "DECLINED",
            "Gateway declined payment");

        result.Status
            .ShouldBe("Failed");

        result.AlreadyCompleted
            .ShouldBeFalse();

        result.ReleasedReservations
            .ShouldBe(1);

        reservation.Status
            .ShouldBe(
                InventoryReservationStatus.Released);

        await inventory
            .Received(1)
            .ReleaseAsync(
                "tenant-1",
                "reservation-payment-failure",
                Arg.Any<CancellationToken>());

        await warehouseReservation
            .Received(1)
            .ReleaseAsync(
                "tenant-1",
                order.Id,
                Arg.Any<CancellationToken>());

        await paymentAttempts
            .Received(1)
            .MarkFailedAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                "DECLINED",
                "Gateway declined payment",
                Arg.Any<CancellationToken>());

        await orderUnitOfWork
            .Received(1)
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());

        order.Status
            .ShouldBe(OrderStatus.PendingPayment);
    }

    [Fact]
    public async Task Already_failed_payment_is_idempotent()
    {
        var order = CreateOrder();

        var paymentAttempt = PaymentAttempt.Create(
            order.Id,
            "tenant-1",
            "user-1",
            Guid.NewGuid().ToString("N"),
            100000,
            "IRR");

        paymentAttempt.MarkFailed(
            "DECLINED",
            "Declined");

        var paymentAttempts =
            Substitute.For<IPaymentAttemptService>();

        var paymentAttemptRepository =
            Substitute.For<IPaymentAttemptRepository>();

        var orderRepository =
            Substitute.For<IOrderRepository>();

        var orderUnitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

        paymentAttemptRepository
            .GetByIdAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                Arg.Any<CancellationToken>())
            .Returns(paymentAttempt);

        var sut = CreateSut(
            paymentAttempts,
            paymentAttemptRepository,
            orderRepository,
            orderUnitOfWork,
            inventory,
            warehouseReservation);

        var result = await sut.FailAsync(
            "tenant-1",
            "user-1",
            paymentAttempt.Id,
            "DECLINED",
            "Declined");

        result.Status
            .ShouldBe("Failed");

        result.AlreadyCompleted
            .ShouldBeTrue();

        result.ReleasedReservations
            .ShouldBe(0);

        await inventory
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        await warehouseReservation
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await paymentAttempts
            .DidNotReceive()
            .MarkFailedAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());

        await orderUnitOfWork
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_payment_does_not_release_committed_reservations()
    {
        var order = CreateOrder();

        var variantId = Guid.NewGuid();

        var reservation = order.AddInventoryReservation(
            "reservation-committed",
            variantId,
            2,
            DateTimeOffset.UtcNow.AddMinutes(10));

        reservation.MarkCommitted();

        var paymentAttempt = PaymentAttempt.Create(
            order.Id,
            "tenant-1",
            "user-1",
            Guid.NewGuid().ToString("N"),
            200000,
            "IRR");

        var paymentAttempts =
            Substitute.For<IPaymentAttemptService>();

        var paymentAttemptRepository =
            Substitute.For<IPaymentAttemptRepository>();

        var orderRepository =
            Substitute.For<IOrderRepository>();

        var orderUnitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

        paymentAttemptRepository
            .GetByIdAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                Arg.Any<CancellationToken>())
            .Returns(paymentAttempt);

        orderRepository
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

        paymentAttempts
            .MarkFailedAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                "DECLINED",
                "Declined",
                Arg.Any<CancellationToken>())
            .Returns(
                new PaymentAttemptDto(
                    paymentAttempt.Id,
                    order.Id,
                    "Failed",
                    200000,
                    "IRR",
                    null,
                    null,
                    "DECLINED",
                    "Declined",
                    paymentAttempt.CreatedAt,
                    DateTimeOffset.UtcNow));

        orderUnitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut = CreateSut(
            paymentAttempts,
            paymentAttemptRepository,
            orderRepository,
            orderUnitOfWork,
            inventory,
            warehouseReservation);

        var result = await sut.FailAsync(
            "tenant-1",
            "user-1",
            paymentAttempt.Id,
            "DECLINED",
            "Declined");

        result.Status
            .ShouldBe("Failed");

        result.AlreadyCompleted
            .ShouldBeFalse();

        result.ReleasedReservations
            .ShouldBe(0);

        reservation.Status
            .ShouldBe(
                InventoryReservationStatus.Committed);

        await inventory
            .DidNotReceive()
            .ReleaseAsync(
                "tenant-1",
                "reservation-committed",
                Arg.Any<CancellationToken>());

        await warehouseReservation
            .Received(1)
            .ReleaseAsync(
                "tenant-1",
                order.Id,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_payment_throws_when_payment_attempt_does_not_exist()
    {
        var paymentAttemptId = Guid.NewGuid();

        var paymentAttempts =
            Substitute.For<IPaymentAttemptService>();

        var paymentAttemptRepository =
            Substitute.For<IPaymentAttemptRepository>();

        var orderRepository =
            Substitute.For<IOrderRepository>();

        var orderUnitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

        paymentAttemptRepository
            .GetByIdAsync(
                "tenant-1",
                "user-1",
                paymentAttemptId,
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<PaymentAttempt?>(null));

        var sut = CreateSut(
            paymentAttempts,
            paymentAttemptRepository,
            orderRepository,
            orderUnitOfWork,
            inventory,
            warehouseReservation);

        await Should.ThrowAsync<KeyNotFoundException>(
            () =>
                sut.FailAsync(
                    "tenant-1",
                    "user-1",
                    paymentAttemptId,
                    "DECLINED",
                    "Declined"));

        await inventory
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        await warehouseReservation
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await paymentAttempts
            .DidNotReceive()
            .MarkFailedAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_payment_rejects_successful_payment_attempt()
    {
        var order = CreateOrder();

        var paymentAttempt = PaymentAttempt.Create(
            order.Id,
            "tenant-1",
            "user-1",
            Guid.NewGuid().ToString("N"),
            100000,
            "IRR");

        paymentAttempt.MarkSucceeded(
            "gateway-reference",
            "gateway-reference");

        var paymentAttempts =
            Substitute.For<IPaymentAttemptService>();

        var paymentAttemptRepository =
            Substitute.For<IPaymentAttemptRepository>();

        var orderRepository =
            Substitute.For<IOrderRepository>();

        var orderUnitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var warehouseReservation =
            Substitute.For<IWarehouseReservationOrchestrator>();

        paymentAttemptRepository
            .GetByIdAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                Arg.Any<CancellationToken>())
            .Returns(paymentAttempt);

        var sut = CreateSut(
            paymentAttempts,
            paymentAttemptRepository,
            orderRepository,
            orderUnitOfWork,
            inventory,
            warehouseReservation);

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    sut.FailAsync(
                        "tenant-1",
                        "user-1",
                        paymentAttempt.Id,
                        "DECLINED",
                        "Declined"));

        exception.Message
            .ShouldBe(
                "A successful payment attempt cannot be marked as failed.");

        await inventory
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        await warehouseReservation
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await paymentAttempts
            .DidNotReceive()
            .MarkFailedAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
    }
}