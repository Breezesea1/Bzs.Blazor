using System.Globalization;
using Bzs.Blazor.Demo.Client;

namespace Bzs.Blazor.BrowserTests;

/// <summary>
/// Reads the newest release the Demo Catalog announces, so a release bump edits the catalog rather
/// than a version literal repeated across the suite.
/// </summary>
internal static class DemoReleases
{
    /// <summary>Gets the newest release's tag id, as the announcement stores and links it.</summary>
    internal static string LatestId => DemoReleaseCatalog.Latest.Id;

    /// <summary>Gets the newest release's version, as the install snippet pins it.</summary>
    internal static string LatestVersion => DemoReleaseCatalog.Latest.Version;

    /// <summary>Gets the newest release's title in one culture, as the announcement dialog shows it.</summary>
    internal static string LatestTitle(bool isChinese) =>
        DemoReleaseCatalog.Latest.Title.Resolve(isChinese);

    /// <summary>Gets the titles of the newest releases in order, as the history page lists them.</summary>
    internal static IReadOnlyList<string> TitlesInOrder(bool isChinese, int count) =>
        DemoReleaseCatalog.All
            .Take(count)
            .Select(release => release.Title.Resolve(isChinese))
            .ToArray();

    /// <summary>Gets the versions the history page lists, newest first.</summary>
    internal static IReadOnlyList<string> AllVersions =>
        DemoReleaseCatalog.All.Select(release => release.Version).ToArray();

    /// <summary>Gets the announcement dialog's accessible name in one culture.</summary>
    internal static string LatestDialogTitle(bool isChinese)
    {
        using var culture = new DemoCultureScope(isChinese);
        return DemoText.Chrome.ReleaseDialogTitle(LatestVersion);
    }

    private sealed class DemoCultureScope : IDisposable
    {
        private readonly CultureInfo _culture;
        private readonly CultureInfo _uiCulture;

        internal DemoCultureScope(bool isChinese)
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;
            var culture = DemoCulture.Resolve(isChinese ? "zh-Hans" : "en-US");
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
