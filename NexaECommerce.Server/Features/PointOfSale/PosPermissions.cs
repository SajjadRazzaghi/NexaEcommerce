using System.ComponentModel;

namespace NexaECommerce.Server.Features.PointOfSale;

public static class PosPermissions
{
    [Description("Read in-store sales and invoices")]
    public const string Read = "pos.read";

    [Description("Create and process in-store sales")]
    public const string Sell = "pos.sell";

    [Description("Cancel pending in-store sales")]
    public const string Cancel = "pos.cancel";
}