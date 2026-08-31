using NexaEcommerce.Modules.Orders.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class ShippingMethodTests
{
    [Fact]
    public void Create_creates_active_shipping_method()
    {
        var method =
            ShippingMethod.Create(
                "tenant-1",
                "standard",
                "Standard Shipping",
                "Local Carrier",
                50000,
                1);

        method.Code
            .ShouldBe("standard");

        method.Price
            .ShouldBe(50000);

        method.SortOrder
            .ShouldBe(1);

        method.IsActive
            .ShouldBeTrue();
    }

    [Fact]
    public void Negative_price_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                ShippingMethod.Create(
                    "tenant-1",
                    "standard",
                    "Standard",
                    "Carrier",
                    -1));
    }

    [Fact]
    public void Deactivate_changes_active_state()
    {
        var method =
            ShippingMethod.Create(
                "tenant-1",
                "express",
                "Express",
                "Carrier",
                100000);

        method.Deactivate();

        method.IsActive
            .ShouldBeFalse();
    }

    [Fact]
    public void Activate_restores_active_state()
    {
        var method =
            ShippingMethod.Create(
                "tenant-1",
                "express",
                "Express",
                "Carrier",
                100000);

        method.Deactivate();
        method.Activate();

        method.IsActive
            .ShouldBeTrue();
    }

    [Fact]
    public void Update_changes_price_and_display_data()
    {
        var method =
            ShippingMethod.Create(
                "tenant-1",
                "standard",
                "Standard",
                "Carrier A",
                50000,
                1);

        method.Update(
            "Updated Standard",
            "Carrier B",
            75000,
            2);

        method.Name
            .ShouldBe(
                "Updated Standard");

        method.Carrier
            .ShouldBe(
                "Carrier B");

        method.Price
            .ShouldBe(75000);

        method.SortOrder
            .ShouldBe(2);
    }
}
