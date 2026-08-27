using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.ShoppingCart;

public sealed class CartTests
{
    private static readonly Guid VariantId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Can_create_user_cart()
    {
        var cart = Cart.ForUser(
            "default",
            "user-1");

        cart.TenantId.ShouldBe("default");
        cart.UserId.ShouldBe("user-1");
        cart.GuestToken.ShouldBeNull();
        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Can_create_guest_cart()
    {
        var cart = Cart.ForGuest(
            "default",
            "guest-token");

        cart.TenantId.ShouldBe("default");
        cart.UserId.ShouldBeNull();
        cart.GuestToken.ShouldBe("guest-token");
        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Adding_new_item_creates_cart_item()
    {
        var cart = Cart.ForUser(
            "default",
            "user-1");

        var item = cart.AddItem(
            VariantId,
            2,
            100m,
            "Product",
            "/image.jpg");

        cart.Items.Count.ShouldBe(1);

        item.ProductVariantId.ShouldBe(VariantId);
        item.Quantity.ShouldBe(2);
        item.UnitPrice.ShouldBe(100m);
        item.ProductName.ShouldBe("Product");
        item.ImageUrl.ShouldBe("/image.jpg");
        item.LineTotal.ShouldBe(200m);
    }

    [Fact]
    public void Adding_same_variant_increases_quantity()
    {
        var cart = Cart.ForUser(
            "default",
            "user-1");

        cart.AddItem(
            VariantId,
            2,
            100m,
            "Product",
            null);

        cart.AddItem(
            VariantId,
            3,
            120m,
            "Product Updated",
            "/new.jpg");

        cart.Items.Count.ShouldBe(1);

        var item = cart.Items.Single();

        item.Quantity.ShouldBe(5);
        item.UnitPrice.ShouldBe(120m);
        item.LineTotal.ShouldBe(600m);
        item.ProductName.ShouldBe("Product Updated");
        item.ImageUrl.ShouldBe("/new.jpg");
    }

    [Fact]
    public void Setting_quantity_changes_existing_item()
    {
        var cart = Cart.ForUser(
            "default",
            "user-1");

        cart.AddItem(
            VariantId,
            2,
            100m,
            "Product",
            null);

        cart.SetQuantity(
            VariantId,
            7,
            95m,
            "Product",
            null);

        var item = cart.Items.Single();

        item.Quantity.ShouldBe(7);
        item.UnitPrice.ShouldBe(95m);
        item.LineTotal.ShouldBe(665m);
    }

    [Fact]
    public void Setting_quantity_to_zero_removes_item()
    {
        var cart = Cart.ForUser(
            "default",
            "user-1");

        cart.AddItem(
            VariantId,
            2,
            100m,
            "Product",
            null);

        cart.SetQuantity(
            VariantId,
            0,
            100m,
            "Product",
            null);

        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Remove_item_removes_only_requested_variant()
    {
        var secondVariant =
            Guid.Parse(
                "22222222-2222-2222-2222-222222222222");

        var cart = Cart.ForUser(
            "default",
            "user-1");

        cart.AddItem(
            VariantId,
            1,
            100m,
            "Product 1",
            null);

        cart.AddItem(
            secondVariant,
            2,
            200m,
            "Product 2",
            null);

        cart.RemoveItem(
            VariantId);

        cart.Items.Count.ShouldBe(1);

        cart.Items.Single()
            .ProductVariantId
            .ShouldBe(secondVariant);
    }

    [Fact]
    public void Clear_removes_all_items()
    {
        var secondVariant =
            Guid.Parse(
                "22222222-2222-2222-2222-222222222222");

        var cart = Cart.ForUser(
            "default",
            "user-1");

        cart.AddItem(
            VariantId,
            1,
            100m,
            "Product 1",
            null);

        cart.AddItem(
            secondVariant,
            2,
            200m,
            "Product 2",
            null);

        cart.Clear();

        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Adding_zero_quantity_throws()
    {
        var cart = Cart.ForUser(
            "default",
            "user-1");

        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                cart.AddItem(
                    VariantId,
                    0,
                    100m,
                    "Product",
                    null));
    }

    [Fact]
    public void Adding_negative_quantity_throws()
    {
        var cart = Cart.ForUser(
            "default",
            "user-1");

        Should.Throw<ArgumentOutOfRangeException>(
            () =>
                cart.AddItem(
                    VariantId,
                    -1,
                    100m,
                    "Product",
                    null));
    }

    [Fact]
    public void Empty_user_id_and_guest_token_are_rejected()
    {
        Should.Throw<ArgumentException>(
            () =>
                Cart.ForUser(
                    "default",
                    ""));

        Should.Throw<ArgumentException>(
            () =>
                Cart.ForGuest(
                    "default",
                    ""));
    }
}