using NexaEcommerce.Modules.Orders.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class OrderLifecycleTests
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
    public void New_order_starts_pending_payment()
    {
        var order = CreateOrder();

        order.Status.ShouldBe(
            OrderStatus.PendingPayment);
    }

    [Fact]
    public void Paid_order_can_start_processing()
    {
        var order = CreateOrder();

        order.MarkPaid();
        order.StartProcessing();

        order.Status.ShouldBe(
            OrderStatus.Processing);
    }

    [Fact]
    public void Processing_order_can_be_shipped()
    {
        var order = CreateOrder();

        order.MarkPaid();
        order.StartProcessing();
        order.MarkShipped();

        order.Status.ShouldBe(
            OrderStatus.Shipped);
    }

    [Fact]
    public void Shipped_order_can_be_delivered()
    {
        var order = CreateOrder();

        order.MarkPaid();
        order.StartProcessing();
        order.MarkShipped();
        order.MarkDelivered();

        order.Status.ShouldBe(
            OrderStatus.Delivered);
    }

    [Fact]
    public void Pending_payment_order_can_be_cancelled()
    {
        var order = CreateOrder();

        order.Cancel();

        order.Status.ShouldBe(
            OrderStatus.Cancelled);
    }

    [Fact]
    public void Paid_order_can_be_cancelled()
    {
        var order = CreateOrder();

        order.MarkPaid();
        order.Cancel();

        order.Status.ShouldBe(
            OrderStatus.Cancelled);
    }

    [Fact]
    public void Shipped_order_cannot_be_cancelled()
    {
        var order = CreateOrder();

        order.MarkPaid();
        order.StartProcessing();
        order.MarkShipped();

        Should.Throw<InvalidOperationException>(
            () => order.Cancel());

        order.Status.ShouldBe(
            OrderStatus.Shipped);
    }

    [Fact]
    public void Delivered_order_cannot_be_cancelled()
    {
        var order = CreateOrder();

        order.MarkPaid();
        order.StartProcessing();
        order.MarkShipped();
        order.MarkDelivered();

        Should.Throw<InvalidOperationException>(
            () => order.Cancel());

        order.Status.ShouldBe(
            OrderStatus.Delivered);
    }

    [Fact]
    public void Cannot_start_processing_before_payment()
    {
        var order = CreateOrder();

        Should.Throw<InvalidOperationException>(
            () => order.StartProcessing());
    }

    [Fact]
    public void Cannot_ship_before_processing()
    {
        var order = CreateOrder();

        order.MarkPaid();

        Should.Throw<InvalidOperationException>(
            () => order.MarkShipped());
    }

    [Fact]
    public void Cannot_deliver_before_shipping()
    {
        var order = CreateOrder();

        order.MarkPaid();
        order.StartProcessing();

        Should.Throw<InvalidOperationException>(
            () => order.MarkDelivered());
    }
}