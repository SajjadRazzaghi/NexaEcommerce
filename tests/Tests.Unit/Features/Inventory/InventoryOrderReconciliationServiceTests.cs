using Microsoft.Extensions.Logging;
using NexaEcommerce.Modules.Inventory.Application.DTOs;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using NexaECommerce.Server.Features.Inventory;
using NSubstitute;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using NexaECommerce.Server.Features.Orders;
namespace NexaECommerce.Tests.Unit.Features.Inventory;

public sealed class InventoryOrderReconciliationServiceTests
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
    private static PaymentReservationOrchestrator
     CreatePaymentReservations(
         IOrderRepository repository,
         IOrderUnitOfWork unitOfWork,
         IInventoryService inventory)
    {
        var paymentAttemptRepository =
            Substitute.For<IPaymentAttemptRepository>();

        /*
         * These reconciliation tests are not intended to test
         * stale pending-payment order cancellation.
         *
         * Return an empty batch so the reservation orchestrator
         * can complete its final stale-order check without
         * introducing unrelated behavior into these tests.
         */
        repository
            .GetPendingPaymentOrdersOlderThanAsync(
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(
                (IReadOnlyList<Order>)
                    Array.Empty<Order>());

        return new PaymentReservationOrchestrator(
            repository,
            unitOfWork,
            paymentAttemptRepository,
            inventory,
            CreateInlineOrderConcurrency(),
            NullLogger<PaymentReservationOrchestrator>.Instance);
    }
    private static Order CreateOrder()
    {
        return Order.Create(
            "tenant-1",
            "user-1",
            "NX-RECON-001",
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
    public async Task Committed_inventory_marks_order_reservation_committed()
    {
        var order =
            CreateOrder();

        var reservation =
            order.AddInventoryReservation(
                "reservation-committed",
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var logger =
            Substitute.For<
                ILogger<InventoryOrderReconciliationService>>();

        repository
            .GetOrdersForInventoryReconciliationAsync(
                "tenant-1",
                100,
                Arg.Any<CancellationToken>())
            .Returns(
                new[] { order });

        inventory
            .GetReservationAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>())
            .Returns(
                new StockReservationDto(
                    reservation.ReservationKey,
                    reservation.ProductVariantId,
                    reservation.Quantity,
                    "Committed",
                    reservation.ExpiresAt));

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut =
     new InventoryOrderReconciliationService(
         repository,
         unitOfWork,
         inventory,
         CreateInlineOrderConcurrency(),
         CreatePaymentReservations(
             repository,
             unitOfWork,
             inventory),
         logger);

        var result =
            await sut.ReconcileAsync(
                "tenant-1",
                100);

        reservation.Status
            .ShouldBe(
                InventoryReservationStatus.Committed);

        result.OrdersChecked
            .ShouldBe(1);

        result.ReservationsChecked
            .ShouldBe(1);

        result.ReservationsRepaired
            .ShouldBe(1);

        result.Discrepancies
            .ShouldBe(0);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Released_inventory_marks_order_reservation_released()
    {
        var order =
            CreateOrder();

        var reservation =
            order.AddInventoryReservation(
                "reservation-released",
                Guid.NewGuid(),
                1,
                DateTimeOffset.UtcNow.AddMinutes(10));

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var logger =
            Substitute.For<
                ILogger<InventoryOrderReconciliationService>>();

        repository
            .GetOrdersForInventoryReconciliationAsync(
                "tenant-1",
                100,
                Arg.Any<CancellationToken>())
            .Returns(
                new[] { order });

        inventory
            .GetReservationAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>())
            .Returns(
                new StockReservationDto(
                    reservation.ReservationKey,
                    reservation.ProductVariantId,
                    reservation.Quantity,
                    "Released",
                    reservation.ExpiresAt));

        unitOfWork
            .SaveChangesAsync(
                Arg.Any<CancellationToken>())
            .Returns(1);

        var sut =
      new InventoryOrderReconciliationService(
          repository,
          unitOfWork,
          inventory,
          CreateInlineOrderConcurrency(),
          CreatePaymentReservations(
              repository,
              unitOfWork,
              inventory),
          logger);

        var result =
            await sut.ReconcileAsync(
                "tenant-1",
                100);

        reservation.Status
            .ShouldBe(
                InventoryReservationStatus.Released);

        result.OrdersChecked
            .ShouldBe(1);

        result.ReservationsChecked
            .ShouldBe(1);

        result.ReservationsRepaired
            .ShouldBe(1);

        result.Discrepancies
            .ShouldBe(0);

        await unitOfWork
            .Received(1)
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Released_order_reservation_releases_still_active_inventory()
    {
        var order =
            CreateOrder();

        var reservation =
            order.AddInventoryReservation(
                "reservation-release-repair",
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        reservation.MarkReleased();

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var logger =
            Substitute.For<
                ILogger<InventoryOrderReconciliationService>>();

        repository
            .GetOrdersForInventoryReconciliationAsync(
                "tenant-1",
                100,
                Arg.Any<CancellationToken>())
            .Returns(
                new[] { order });

        inventory
            .GetReservationAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>())
            .Returns(
                new StockReservationDto(
                    reservation.ReservationKey,
                    reservation.ProductVariantId,
                    reservation.Quantity,
                    "Active",
                    reservation.ExpiresAt));

        inventory
            .ReleaseAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>())
            .Returns(
                new StockReservationDto(
                    reservation.ReservationKey,
                    reservation.ProductVariantId,
                    reservation.Quantity,
                    "Released",
                    reservation.ExpiresAt));

        var sut =
       new InventoryOrderReconciliationService(
           repository,
           unitOfWork,
           inventory,
           CreateInlineOrderConcurrency(),
           CreatePaymentReservations(
               repository,
               unitOfWork,
               inventory),
           logger);

        var result =
            await sut.ReconcileAsync(
                "tenant-1",
                100);

        result.OrdersChecked
            .ShouldBe(1);

        result.ReservationsChecked
            .ShouldBe(1);

        result.ReservationsRepaired
            .ShouldBe(1);

        result.Discrepancies
            .ShouldBe(0);

        await inventory
            .Received(1)
            .ReleaseAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_inventory_reservation_is_reported_as_discrepancy()
    {
        var order =
            CreateOrder();

        var reservation =
            order.AddInventoryReservation(
                "reservation-missing",
                Guid.NewGuid(),
                1,
                DateTimeOffset.UtcNow.AddMinutes(10));

        var repository =
            Substitute.For<IOrderRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        var inventory =
            Substitute.For<IInventoryService>();

        var logger =
            Substitute.For<
                ILogger<InventoryOrderReconciliationService>>();

        repository
            .GetOrdersForInventoryReconciliationAsync(
                "tenant-1",
                100,
                Arg.Any<CancellationToken>())
            .Returns(
                new[] { order });

        inventory
            .GetReservationAsync(
                "tenant-1",
                reservation.ReservationKey,
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<
                    StockReservationDto?>(null));

        var sut =
      new InventoryOrderReconciliationService(
          repository,
          unitOfWork,
          inventory,
          CreateInlineOrderConcurrency(),
          CreatePaymentReservations(
              repository,
              unitOfWork,
              inventory),
          logger);

        var result =
            await sut.ReconcileAsync(
                "tenant-1",
                100);

        result.OrdersChecked
            .ShouldBe(1);

        result.ReservationsChecked
            .ShouldBe(1);

        result.ReservationsRepaired
            .ShouldBe(0);

        result.Discrepancies
            .ShouldBe(1);

        await inventory
            .DidNotReceive()
            .ReleaseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        await inventory
            .DidNotReceive()
            .CommitAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }
}