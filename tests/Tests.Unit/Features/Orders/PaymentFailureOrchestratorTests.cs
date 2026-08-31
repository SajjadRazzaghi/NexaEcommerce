using NSubstitute;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaECommerce.Server.Features.Orders;
using Shouldly;

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

    [Fact]
    public async Task Failed_payment_releases_active_inventory_reservations()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        var reservation =
            order.AddInventoryReservation(
                "reservation-payment-failure",
                variantId,
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        var paymentAttempt =
            PaymentAttempt.Create(
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

        inventory
            .ReleaseAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new StockReservationDto(
                        reservation.ReservationKey,
                        variantId,
                        2,
                        "Released",
                        DateTimeOffset.UtcNow)));

        paymentAttempts
            .MarkFailedAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                "DECLINED",
                "Gateway declined payment",
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
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
                        DateTimeOffset.UtcNow)));

        orderUnitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut =
            new PaymentFailureOrchestrator(
                paymentAttempts,
                paymentAttemptRepository,
                orderRepository,
                orderUnitOfWork,
                inventory);

        var result =
            await sut.FailAsync(
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
                reservation.ReservationKey,
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

        order.Status
            .ShouldBe(
                OrderStatus.PendingPayment);
    }

    [Fact]
    public async Task Already_failed_payment_is_idempotent()
    {
        var order =
            CreateOrder();

        var paymentAttempt =
            PaymentAttempt.Create(
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

        paymentAttemptRepository
            .GetByIdAsync(
                "tenant-1",
                "user-1",
                paymentAttempt.Id,
                Arg.Any<CancellationToken>())
            .Returns(paymentAttempt);

        var sut =
            new PaymentFailureOrchestrator(
                paymentAttempts,
                paymentAttemptRepository,
                orderRepository,
                orderUnitOfWork,
                inventory);

        var result =
            await sut.FailAsync(
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
    }
}