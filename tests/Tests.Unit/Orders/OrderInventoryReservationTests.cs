using NexaEcommerce.Modules.Orders.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class OrderInventoryReservationTests
{
    private static Order CreateOrder()
    {
        return Order.Create(
            "tenant-1",
            "user-1",
            "NX-RES-001",
            Guid.NewGuid().ToString("N"),
            "IRR",
            0,
            0,
            0,
            null,
            "Test User",
            "09120000000",
            "Test Address",
            "Tehran",
            "1234567890");
    }

    [Fact]
    public void Expired_inventory_reservation_can_be_marked_expired_on_order()
    {
        var order =
            Order.Create(
                "tenant-1",
                "user-1",
                "NX-EXP-001",
                Guid.NewGuid().ToString("N"),
                "IRR",
                0,
                0,
                0,
                null,
                "Test User",
                "09120000000",
                "Test Address",
                "Tehran",
                "1234567890");

        var reservation =
            order.AddInventoryReservation(
                "reservation-expired",
                Guid.NewGuid(),
                1,
                DateTimeOffset.UtcNow.AddMinutes(10));

        var changed =
            order.MarkInventoryReservationExpired(
                "reservation-expired");

        changed.ShouldBeTrue();

        reservation.Status
            .ShouldBe(
                InventoryReservationStatus.Expired);
    }

    [Fact]
    public void Adding_reservation_tracks_it_on_order()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        var expiresAt =
            DateTimeOffset.UtcNow
                .AddMinutes(15);

        var reservation =
            order.AddInventoryReservation(
                "reservation-1",
                variantId,
                3,
                expiresAt);

        order.InventoryReservations
            .Count
            .ShouldBe(1);

        reservation
            .ReservationKey
            .ShouldBe("reservation-1");

        reservation
            .ProductVariantId
            .ShouldBe(variantId);

        reservation
            .Quantity
            .ShouldBe(3);

        reservation
            .Status
            .ShouldBe(
                InventoryReservationStatus.Reserved);
    }

    [Fact]
    public void Adding_same_reservation_key_is_idempotent()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        var expiresAt =
            DateTimeOffset.UtcNow
                .AddMinutes(15);

        var first =
            order.AddInventoryReservation(
                "reservation-1",
                variantId,
                2,
                expiresAt);

        var second =
            order.AddInventoryReservation(
                "reservation-1",
                variantId,
                2,
                expiresAt);

        order.InventoryReservations
            .Count
            .ShouldBe(1);

        second
            .ShouldBeSameAs(first);
    }

    [Fact]
    public void Same_key_with_different_quantity_is_rejected()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        var expiresAt =
            DateTimeOffset.UtcNow
                .AddMinutes(15);

        order.AddInventoryReservation(
            "reservation-1",
            variantId,
            2,
            expiresAt);

        Should.Throw<InvalidOperationException>(
            () =>
                order.AddInventoryReservation(
                    "reservation-1",
                    variantId,
                    3,
                    expiresAt));
    }

    [Fact]
    public void Marking_inventory_reservations_committed_changes_reserved_state()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        order.AddInventoryReservation(
            "reservation-1",
            variantId,
            2,
            DateTimeOffset.UtcNow.AddMinutes(15));

        order.MarkInventoryReservationsCommitted();

        order.InventoryReservations
            .Single()
            .Status
            .ShouldBe(
                InventoryReservationStatus.Committed);
    }

    [Fact]
    public void Marking_inventory_reservations_released_changes_reserved_state()
    {
        var order =
            CreateOrder();

        var variantId =
            Guid.NewGuid();

        order.AddInventoryReservation(
            "reservation-1",
            variantId,
            2,
            DateTimeOffset.UtcNow.AddMinutes(15));

        order.MarkInventoryReservationsReleased();

        order.InventoryReservations
            .Single()
            .Status
            .ShouldBe(
                InventoryReservationStatus.Released);
    }
}
