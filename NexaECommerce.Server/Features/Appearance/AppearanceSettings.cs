using NexaECommerce.Server.Platform.Settings;

namespace NexaECommerce.Server.Features.Appearance;

public static class AppearanceSettings
{
    public const string Category = "Appearance";

    public const string Theme =
        "Appearance.Theme";

    public const string BrandColor =
        "Appearance.BrandColor";

    public const string CustomTheme =
        "Appearance.CustomTheme";

    public const string ContactPhone =
        "Appearance.ContactPhone";

    public const string ContactEmail =
        "Appearance.ContactEmail";

    public const string ContactAddress =
        "Appearance.ContactAddress";

    public const string WebsiteUrl =
        "Appearance.WebsiteUrl";

    public const string SeoTitle =
        "Appearance.SeoTitle";

    public const string SeoDescription =
        "Appearance.SeoDescription";

    public const string FaviconUrl =
        "Appearance.FaviconUrl";
}

public sealed class AppearanceSettingsContributor
    : ISettingsContributor
{
    public void Register()
    {
        SettingDefinitions.Register(
            AppearanceSettings.Theme,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.BrandColor,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.CustomTheme,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.ContactPhone,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.ContactEmail,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.ContactAddress,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.WebsiteUrl,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.SeoTitle,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.SeoDescription,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);

        SettingDefinitions.Register(
            AppearanceSettings.FaviconUrl,
            typeof(string),
            [SettingScope.Tenant],
            string.Empty,
            AppearanceSettings.Category);
    }
}
