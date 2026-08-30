using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.ShoppingCart;

public sealed class CartMergeTests
{
    [Fact]
    public void MergeFrom_combines_quantities()
    {
        var userCart =
            Cart.ForUser(
                "default",
                "user-1");

        userCart.AddItem(
            Guid.Parse(
                "11111111-1111-1111-1111-111111111111"),
            2,
            100,
            "Product",
            null);

        var guestCart =
            Cart.ForGuest(
                "default",
                "guest-token");

        guestCart.AddItem(
            Guid.Parse(
                "11111111-1111-1111-1111-111111111111"),
            2,
            100,
            "Product",
            null);

        var stock =
            new Dictionary<Guid, int>
            {
                [
                    Guid.Parse(
                        "11111111-1111-1111-1111-111111111111")
                ] = 10
            };

        userCart.MergeFrom(
            guestCart,
            stock);

        userCart.Items.Count
            .ShouldBe(1);

        userCart.Items
            .Single()
            .Quantity
            .ShouldBe(4);
    }

    [Fact]
    public void MergeFrom_caps_quantity_at_available_stock()
    {
        var variantId =
            Guid.NewGuid();

        var userCart =
            Cart.ForUser(
                "default",
                "user-1");

        userCart.AddItem(
            variantId,
            4,
            100,
            "Product",
            null);

        var guestCart =
            Cart.ForGuest(
                "default",
                "guest-token");

        guestCart.AddItem(
            variantId,
            4,
            100,
            "Product",
            null);

        var stock =
            new Dictionary<Guid, int>
            {
                [variantId] = 5
            };

        userCart.MergeFrom(
            guestCart,
            stock);

        userCart.Items
            .Single()
            .Quantity
            .ShouldBe(5);
    }

    [Fact]
    public void MergeFrom_does_not_add_item_when_stock_is_zero()
    {
        var variantId =
            Guid.NewGuid();

        var userCart =
            Cart.ForUser(
                "default",
                "user-1");

        var guestCart =
            Cart.ForGuest(
                "default",
                "guest-token");

        guestCart.AddItem(
            variantId,
            2,
            100,
            "Product",
            null);

        var stock =
            new Dictionary<Guid, int>
            {
                [variantId] = 0
            };

        userCart.MergeFrom(
            guestCart,
            stock);

        userCart.Items
            .ShouldBeEmpty();
    }
}
