using System.Globalization;
using Bzs.Blazor.Demo.Client;

namespace Bzs.Blazor.BrowserTests;

/// <summary>
/// Reads visitor-facing Demo Catalog copy for one culture so browser assertions compare against the
/// same source the hosts render from rather than a second copy of every string.
/// </summary>
internal static class DemoChrome
{
    internal static DemoChromeCopy Read(bool isChinese)
    {
        // DemoText follows the ambient UI culture, which each host applies from ?culture=. Resolve
        // every string up front so no assertion depends on the culture still being in scope.
        using var culture = new DemoCultureScope(isChinese ? "zh-Hans" : "en-US");
        return new DemoChromeCopy
        {
            SkipLink = DemoText.Chrome.SkipLink,
            NavigationAccessibleName = DemoText.Chrome.NavigationAccessibleName,
            BrandTagline = DemoText.Chrome.BrandTagline,
            CloseNavigation = DemoText.Chrome.CloseNavigation,
            OpenNavigation = DemoText.Chrome.OpenNavigation,
            DemoUser = DemoText.Chrome.DemoUser,
            DemoUserAvatarInitial = DemoText.Chrome.DemoUserAvatarInitial,
            SignOutAccessibleName = DemoText.Chrome.SignOutAccessibleName,
            ResizeNavigationDrawer = DemoText.Chrome.ResizeNavigationDrawer,
            ComponentWorkbench = DemoText.Chrome.ComponentWorkbench,
            AspireDemoHost = DemoText.Chrome.AspireDemoHost,
            StaticWebAssemblyHost = DemoText.Chrome.StaticWebAssemblyHost,
            LanguageSwitcherAccessibleName = DemoText.Chrome.LanguageSwitcherAccessibleName,
            ThemeSwitcherAccessibleName = DemoText.Chrome.ThemeSwitcherAccessibleName,
            ThemeLight = DemoText.Chrome.ThemeLight,
            ThemeDark = DemoText.Chrome.ThemeDark,
            ThemeSystem = DemoText.Chrome.ThemeSystem,
            FoundationComponents = DemoText.Chrome.FoundationComponents,
        };
    }

    /// <summary>Gets the navigation link names a host offers, in the order chrome presents them.</summary>
    internal static IReadOnlyList<string> ReadNavigationLinkNames(
        bool isChinese,
        bool includesServerRenderModes)
    {
        using var culture = new DemoCultureScope(isChinese ? "zh-Hans" : "en-US");
        return DemoCatalogChrome.GetSections(includesServerRenderModes)
            .SelectMany(section => section.Destinations)
            .Select(entry => entry.Name)
            .ToArray();
    }

    /// <summary>Gets the section names a host offers, in the order chrome presents them.</summary>
    internal static IReadOnlyList<string> ReadSectionNames(
        bool isChinese,
        bool includesServerRenderModes)
    {
        using var culture = new DemoCultureScope(isChinese ? "zh-Hans" : "en-US");
        return DemoCatalogChrome.GetSections(includesServerRenderModes)
            .Select(section => section.Name)
            .ToArray();
    }

    private sealed class DemoCultureScope : IDisposable
    {
        private readonly CultureInfo _culture;
        private readonly CultureInfo _uiCulture;

        internal DemoCultureScope(string cultureName)
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;
            var culture = DemoCulture.Resolve(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }
}

internal sealed class DemoChromeCopy
{
    public required string SkipLink { get; init; }

    public required string NavigationAccessibleName { get; init; }

    public required string BrandTagline { get; init; }

    public required string CloseNavigation { get; init; }

    public required string OpenNavigation { get; init; }

    public required string DemoUser { get; init; }

    public required string DemoUserAvatarInitial { get; init; }

    public required string SignOutAccessibleName { get; init; }

    public required string ResizeNavigationDrawer { get; init; }

    public required string ComponentWorkbench { get; init; }

    public required string AspireDemoHost { get; init; }

    public required string StaticWebAssemblyHost { get; init; }

    public required string LanguageSwitcherAccessibleName { get; init; }

    public required string ThemeSwitcherAccessibleName { get; init; }

    public required string ThemeLight { get; init; }

    public required string ThemeDark { get; init; }

    public required string ThemeSystem { get; init; }

    public required string FoundationComponents { get; init; }
}
