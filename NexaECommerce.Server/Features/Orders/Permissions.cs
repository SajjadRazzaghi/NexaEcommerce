using System.ComponentModel;

namespace NexaECommerce.Server.Features.Orders;

public static class OrderPermissions
{
    [Description("View own orders")]
    public const string Read = "orders.read";

    [Description("View all tenant orders")]
    public const string Manage = "orders.manage";

    [Description("Update order status")]
    public const string UpdateStatus = "orders.update-status";
}