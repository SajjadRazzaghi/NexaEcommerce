using NSubstitute;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaECommerce.Server.Features.Orders;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
namespace NexaECommerce.Tests.Unit.Features.Orders;

public sealed class PaymentRetryOrchestratorTests
{
    private static IOrderConcurrencyService CreateInlineOrderConcurrency()
    {
        var concurrency =
            Substitute.For<IOrderConcurrencyService>();

        concurrency.ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                callInfo =>
                {
                    var action =
                        callInfo.ArgAt<
                            Func<CancellationToken, Task>>(2);

                    var cancellationToken =
                        callInfo.ArgAt<CancellationToken>(3);

                    return action(cancellationToken);
                });

        return concurrency;
    }
    private static Order CreateOrder()
    {
        return Order.Create(
            "tenant-1",
            "user-1",
            "NX-PAY-RETRY-001",
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
    public async Task Retry_creates_new_reservation_and_payment_attempt()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        order.AddItem(
            variantId,
            "SKU-001",
            "Test Product",
            100000,
            2);

        var orderRepository =
            Substitute.For<IOrderRepository>();

        var orderUnitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var paymentAttempts =
            Substitute.For<IPaymentAttemptService>();

        orderRepository
            .GetByIdAsync(
                "tenant-1",
                order.Id,
                "user-1",
                Arg.Any<CancellationToken>())
            .Returns(order);

        inventory
            .ReserveAsync(
                "tenant-1",
                variantId,
                2,
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(
                callInfo =>
                {
                    var key =
                        callInfo.ArgAt<string>(3);

                    return Task.FromResult(
                        new StockReservationDto(
                            key,
                            variantId,
                            2,
                            "Active",
                            DateTimeOffset.UtcNow.AddMinutes(15)));
                });

        orderUnitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var paymentResult =
            new PaymentAttemptDto(
                Guid.NewGuid(),
                order.Id,
                "Pending",
                200000,
                "IRR",
                null,
                null,
                null,
                null,
                DateTimeOffset.UtcNow,
                null);

        paymentAttempts
            .CreateAsync(
                "tenant-1",
                "user-1",
                order.Id,
                "retry-key-1",
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    paymentResult));
        var paymentAttemptRepository =
    Substitute.For<IPaymentAttemptRepository>();

        var paymentReservations =
            new PaymentReservationOrchestrator(
                orderRepository,
                orderUnitOfWork,
                paymentAttemptRepository,
                inventory,
                CreateInlineOrderConcurrency(),
                NullLogger<PaymentReservationOrchestrator>.Instance);
        var sut =
            new PaymentRetryOrchestrator(
                paymentReservations,
                paymentAttempts);

        var result =
            await sut.RetryAsync(
                "tenant-1",
                "user-1",
                order.Id,
                "retry-key-1");

        result.Id
            .ShouldBe(
                paymentResult.Id);

        order.InventoryReservations
            .Count
            .ShouldBe(1);

        order.InventoryReservations
            .Single()
            .Status
            .ShouldBe(
                InventoryReservationStatus.Reserved);

        await inventory
            .Received(1)
            .ReserveAsync(
                "tenant-1",
                variantId,
                2,
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>());

        await paymentAttempts
            .Received(1)
            .CreateAsync(
                "tenant-1",
                "user-1",
                order.Id,
                "retry-key-1",
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retry_reuses_existing_active_reservation()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        order.AddItem(
            variantId,
            "SKU-002",
            "Existing Reserved Product",
            50000,
            1);

        order.AddInventoryReservation(
            "existing-reservation",
            variantId,
            1,
            DateTimeOffset.UtcNow.AddMinutes(10));

        var orderRepository =
            Substitute.For<IOrderRepository>();

        var orderUnitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var paymentAttempts =
            Substitute.For<IPaymentAttemptService>();

        orderRepository
            .GetByIdAsync(
                "tenant-1",
                order.Id,
                "user-1",
                Arg.Any<CancellationToken>())
            .Returns(order);

        orderUnitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        paymentAttempts
            .CreateAsync(
                "tenant-1",
                "user-1",
                order.Id,
                "retry-key-2",
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new PaymentAttemptDto(
                        Guid.NewGuid(),
                        order.Id,
                        "Pending",
                        50000,
                        "IRR",
                        null,
                        null,
                        null,
                        null,
                        DateTimeOffset.UtcNow,
                        null)));
        var paymentAttemptRepository =
            Substitute.For<IPaymentAttemptRepository>();

        var paymentReservations =
            new PaymentReservationOrchestrator(
                orderRepository,
                orderUnitOfWork,
                paymentAttemptRepository,
                inventory,
                CreateInlineOrderConcurrency(),
                NullLogger<PaymentReservationOrchestrator>.Instance);
        var sut =
      new PaymentRetryOrchestrator(
          paymentReservations,
          paymentAttempts);

        await sut.RetryAsync(
            "tenant-1",
            "user-1",
            order.Id,
            "retry-key-2");

        await inventory
            .DidNotReceive()
            .ReserveAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>());
    }
}