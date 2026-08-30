using System.ComponentModel;

namespace NexaECommerce.Server.Features.Inventory;

public static class InventoryPermissions
{
    [Description("View inventory")]
    public const string Read = "inventory.read";

    [Description("Manage inventory")]
    public const string Manage = "inventory.manage";
}
