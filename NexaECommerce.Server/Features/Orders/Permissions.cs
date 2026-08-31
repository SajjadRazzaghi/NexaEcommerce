
using System.ComponentModel;

namespace NexaECommerce.Server.Features.Orders;

public static class OrderPermissions
{
    [Description("View own orders")]
    public const string Read =
        "orders.read";

    [Description("View all tenant orders")]
    public const string Manage =
        "orders.manage";

    [Description("Update order status")]
    public const string UpdateStatus =
        "orders.update-status";

    [Description("View shipping methods")]
    public const string ShippingRead =
        "shipping.read";

    [Description("Create shipping methods")]
    public const string ShippingCreate =
        "shipping.create";

    [Description("Update shipping methods")]
    public const string ShippingUpdate =
        "shipping.update";

    [Description("Delete shipping methods")]
    public const string ShippingDelete =
        "shipping.delete";
}
