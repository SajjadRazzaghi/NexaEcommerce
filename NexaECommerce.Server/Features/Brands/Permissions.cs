using System.ComponentModel;

namespace NexaECommerce.Server.Features.Brands;

public static class BrandPermissions
{
    [Description("View brands")]
    public const string Read = "brands.read";

    [Description("Create brands")]
    public const string Create = "brands.create";

    [Description("Edit brands")]
    public const string Update = "brands.update";

    [Description("Delete brands")]
    public const string Delete = "brands.delete";

    [Description("Restore deleted brands")]
    public const string Restore = "brands.restore";

    [Description("Activate or deactivate brands")]
    public const string ManageStatus = "brands.status";

    [Description("Publish or unpublish brands")]
    public const string Publish = "brands.publish";

    [Description("Feature or unfeature brands")]
    public const string Feature = "brands.feature";
}