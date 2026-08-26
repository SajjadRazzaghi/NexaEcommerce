namespace NexaECommerce.Server.Platform.Authorization;

/// <summary>
/// Permissions are carried as principal claims of type <see cref="ClaimType"/>.
/// A role stores its permissions as role claims using the same type.
/// </summary>
public static class PermissionClaims
{
    public const string ClaimType = "permission";

    /// <summary>
    /// Superadmin wildcard.
    /// Grants every permission, including permissions introduced by future features.
    /// </summary>
    public const string All = "*";

    /// <summary>
    /// Determines whether a granted permission satisfies a required permission.
    ///
    /// Supported forms:
    /// - exact: catalog.products.read
    /// - global wildcard: *
    /// - group wildcard: catalog.*
    /// - nested wildcard: catalog.products.*
    /// </summary>
    public static bool Grants(string granted, string required)
    {
        if (string.Equals(granted, All, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(granted, required, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!granted.EndsWith(".*", StringComparison.Ordinal))
            return false;

        var prefix = granted[..^1];

        return required.StartsWith(
            prefix,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true when any granted permission satisfies the required permission.
    /// </summary>
    public static bool Satisfies(
        IEnumerable<string> granted,
        string required)
    {
        return granted.Any(g => Grants(g, required));
    }
}