using NexaEcommerce.Modules.Customers.Domain.Entities;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Customers;

public sealed class CustomerAddressTests
{
    [Fact]
    public void Create_creates_address()
    {
        var address =
            CustomerAddress.Create(
                "default",
                "user-1",
                "Home",
                "Mehdi Razzaghi",
                "09120000000",
                "Iran",
                "Tehran",
                "Tehran",
                "Valiasr Street",
                "1234567890",
                true);

        address.TenantId
            .ShouldBe("default");

        address.UserId
            .ShouldBe("user-1");

        address.Title
            .ShouldBe("Home");

        address.IsDefault
            .ShouldBeTrue();
    }

    [Fact]
    public void Update_changes_address_data()
    {
        var address =
            CustomerAddress.Create(
                "default",
                "user-1",
                "Home",
                "Old Name",
                "09120000000",
                "Iran",
                "Tehran",
                "Tehran",
                "Old Address",
                null);

        address.Update(
            "Work",
            "New Name",
            "09210000000",
            "Iran",
            "Fars",
            "Shiraz",
            "New Address",
            "1111111111");

        address.Title
            .ShouldBe("Work");

        address.RecipientName
            .ShouldBe("New Name");

        address.City
            .ShouldBe("Shiraz");

        address.PostalCode
            .ShouldBe("1111111111");
    }

    [Fact]
    public void Empty_required_value_is_rejected()
    {
        Should.Throw<ArgumentException>(
            () =>
                CustomerAddress.Create(
                    "default",
                    "user-1",
                    "",
                    "Name",
                    "09120000000",
                    "Iran",
                    "Tehran",
                    "Tehran",
                    "Address",
                    null));
    }

    [Fact]
    public void SetDefault_marks_address_default()
    {
        var address =
            CustomerAddress.Create(
                "default",
                "user-1",
                "Home",
                "Name",
                "09120000000",
                "Iran",
                "Tehran",
                "Tehran",
                "Address",
                null);

        address.SetDefault();

        address.IsDefault
            .ShouldBeTrue();

        address.ClearDefault();

        address.IsDefault
            .ShouldBeFalse();
    }
}

