
using NexaEcommerce.Modules.Orders.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class ShipmentTests
{
    private static Shipment CreateShipment()
    {
        return Shipment.Create(
            Guid.NewGuid(),
            "tenant-1",
            "Standard",
            "TestCarrier",
            "TRACK-001");
    }

    [Fact]
    public void New_shipment_starts_pending()
    {
        var shipment =
            CreateShipment();

        shipment.Status
            .ShouldBe(
                ShipmentStatus.Pending);

        shipment.ShippedAt
            .ShouldBeNull();

        shipment.DeliveredAt
            .ShouldBeNull();
    }

    [Fact]
    public void Shipment_requires_tracking_number_before_shipping()
    {
        var shipment =
            Shipment.Create(
                Guid.NewGuid(),
                "tenant-1",
                "Express",
                "TestCarrier");

        Should.Throw<InvalidOperationException>(
            () =>
                shipment.MarkShipped());

        shipment.Status
            .ShouldBe(
                ShipmentStatus.Pending);
    }

    [Fact]
    public void Shipment_can_be_shipped_with_tracking_number()
    {
        var shipment =
            CreateShipment();

        shipment.MarkShipped();

        shipment.Status
            .ShouldBe(
                ShipmentStatus.Shipped);

        shipment.ShippedAt
            .ShouldNotBeNull();
    }

    [Fact]
    public void Shipped_shipment_can_be_delivered()
    {
        var shipment =
            CreateShipment();

        shipment.MarkShipped();
        shipment.MarkDelivered();

        shipment.Status
            .ShouldBe(
                ShipmentStatus.Delivered);

        shipment.DeliveredAt
            .ShouldNotBeNull();
    }

    [Fact]
    public void Shipment_cannot_be_delivered_before_shipping()
    {
        var shipment =
            CreateShipment();

        Should.Throw<InvalidOperationException>(
            () =>
                shipment.MarkDelivered());
    }

    [Fact]
    public void Tracking_number_can_be_changed_before_shipping()
    {
        var shipment =
            CreateShipment();

        shipment.SetTrackingNumber(
            "TRACK-002");

        shipment.TrackingNumber
            .ShouldBe("TRACK-002");
    }

    [Fact]
    public void Tracking_number_cannot_be_changed_after_shipping()
    {
        var shipment =
            CreateShipment();

        shipment.MarkShipped();

        Should.Throw<InvalidOperationException>(
            () =>
                shipment.SetTrackingNumber(
                    "TRACK-002"));
    }

    [Fact]
    public void Shipped_shipment_cannot_be_cancelled()
    {
        var shipment =
            CreateShipment();

        shipment.MarkShipped();

        Should.Throw<InvalidOperationException>(
            () =>
                shipment.Cancel());

        shipment.Status
            .ShouldBe(
                ShipmentStatus.Shipped);
    }

    [Fact]
    public void Pending_shipment_can_be_cancelled()
    {
        var shipment =
            CreateShipment();

        shipment.Cancel();

        shipment.Status
            .ShouldBe(
                ShipmentStatus.Cancelled);
    }
}
