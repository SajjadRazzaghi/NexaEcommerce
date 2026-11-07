using System.ComponentModel;

namespace NexaECommerce.Server.Features.Favorites;

public static class FavoritePermissions
{
    [Description("View product popularity and customer favorite analytics")]
    public const string ReadReport = "catalog.favorites.read";
}
