using System.ComponentModel;

namespace NexaECommerce.Server.Features.Customers;

public static class CustomerPermissions
{
    [Description("View customers, their contact details, addresses and order history")]
    public const string Read = "customers.read";

    [Description("Update customer information")]
    public const string Update = "customers.update";
}