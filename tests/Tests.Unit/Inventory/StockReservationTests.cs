using NexaEcommerce.Modules.Inventory.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Inventory;

public sealed class StockReservationTests
{
    [Fact]
    public void Expired_active_reservation_is_detected()
    {
        var reservation =
            StockReservation.Create(
                "default",
                "reservation-1",
                Guid.NewGuid(),
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(-1));

        reservation.IsActive
            .ShouldBeTrue();

        reservation.IsExpired
            .ShouldBeTrue();
    }

    [Fact]
    public void MarkExpired_changes_status()
    {
        var reservation =
            StockReservation.Create(
                "default",
                "reservation-2",
                Guid.NewGuid(),
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        reservation.MarkExpired();

        reservation.Status
            .ShouldBe(StockReservationStatus.Expired);

        reservation.CompletedAt
            .ShouldNotBeNull();
    }

    [Fact]
    public void MarkExpired_on_non_active_reservation_does_nothing()
    {
        var reservation =
            StockReservation.Create(
                "default",
                "reservation-3",
                Guid.NewGuid(),
                Guid.NewGuid(),
                2,
                DateTimeOffset.UtcNow.AddMinutes(10));

        reservation.MarkReleased();

        reservation.MarkExpired();

        reservation.Status
            .ShouldBe(StockReservationStatus.Released);
    }
}