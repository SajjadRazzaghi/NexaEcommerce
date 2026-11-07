using NexaEcommerce.Modules.Catalog.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Features.Catalog;

public sealed class ProductFavoriteTests
{
    [Fact]
    public void Create_for_registered_user_sets_user_identity()
    {
        var productId = Guid.NewGuid();

        var favorite = new ProductFavorite(
            " tenant-1 ",
            productId,
            " user-1 ",
            null);

        favorite.TenantId.ShouldBe("tenant-1");
        favorite.ProductId.ShouldBe(productId);
        favorite.UserId.ShouldBe("user-1");
        favorite.GuestKey.ShouldBeNull();
        favorite.IsActive.ShouldBeTrue();
        favorite.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_for_guest_sets_guest_identity()
    {
        var productId = Guid.NewGuid();

        var favorite = new ProductFavorite(
            "tenant-1",
            productId,
            null,
            " guest-key ");

        favorite.TenantId.ShouldBe("tenant-1");
        favorite.ProductId.ShouldBe(productId);
        favorite.UserId.ShouldBeNull();
        favorite.GuestKey.ShouldBe("guest-key");
        favorite.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_missing_tenant()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = new ProductFavorite(
                " ",
                Guid.NewGuid(),
                "user-1",
                null);
        });
    }

    [Fact]
    public void Create_rejects_empty_product_id()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = new ProductFavorite(
                "tenant-1",
                Guid.Empty,
                "user-1",
                null);
        });
    }

    [Fact]
    public void Create_requires_exactly_one_customer_identity()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = new ProductFavorite(
                "tenant-1",
                Guid.NewGuid(),
                null,
                null);
        });

        Should.Throw<ArgumentException>(() =>
        {
            _ = new ProductFavorite(
                "tenant-1",
                Guid.NewGuid(),
                "user-1",
                "guest-key");
        });
    }

    [Fact]
    public void AssignToUser_transfers_guest_favorite_to_account()
    {
        var favorite = new ProductFavorite(
            "tenant-1",
            Guid.NewGuid(),
            null,
            "guest-key");

        favorite.AssignToUser(" user-2 ");

        favorite.UserId.ShouldBe("user-2");
        favorite.GuestKey.ShouldBeNull();
        favorite.IsActive.ShouldBeTrue();
        favorite.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void AssignToUser_rejects_inactive_favorite()
    {
        var favorite = new ProductFavorite(
            "tenant-1",
            Guid.NewGuid(),
            null,
            "guest-key");

        favorite.Remove();

        Should.Throw<InvalidOperationException>(() =>
            favorite.AssignToUser("user-1"));
    }

    [Fact]
    public void Remove_is_idempotent_and_records_removal_time()
    {
        var favorite = new ProductFavorite(
            "tenant-1",
            Guid.NewGuid(),
            "user-1",
            null);

        favorite.Remove();

        favorite.IsActive.ShouldBeFalse();
        favorite.RemovedAt.ShouldNotBeNull();
        var firstRemovedAt = favorite.RemovedAt;

        favorite.Remove();

        favorite.IsActive.ShouldBeFalse();
        favorite.RemovedAt.ShouldBe(firstRemovedAt);
    }
}
