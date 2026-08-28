using Bzs.Blazor.Demo.Client;
using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.BrowserTests;

/// <summary>
/// Builds the addresses browser tests navigate to, by asking the catalog's own link seam where a
/// destination lives. Tests name a destination and a culture; the host's base path and the
/// culture-preserving query are the seam's business, so a route rename reaches the tests through
/// <see cref="DemoCatalogDestinations" /> instead of through every navigation.
///
/// A few addresses are test-owned rather than visitor-facing: the per-render-mode variants of the
/// productivity workbench and the navigation-drawer lifecycle harness exist so a test can pin a
/// render mode. They are named here so every address a test navigates to still lives in one place.
/// </summary>
public sealed class DemoDestinationUrls
{
    public const string English = "en-US";
    public const string Chinese = "zh-Hans";

    private readonly Uri _baseUri;

    public DemoDestinationUrls(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        _baseUri = new Uri($"{baseUrl.TrimEnd('/')}/");
    }

    /// <summary>Gets the absolute address of a destination for a visitor in the given culture.</summary>
    public string To(
        DemoCatalogDestination destination,
        string? culture = null,
        string? query = null,
        string? fragment = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var links = new DemoDestinationLinks(
            new TestNavigationManager(_baseUri.ToString(), CurrentUri(culture).ToString()));
        return Absolute(links.To(destination, query, fragment));
    }

    /// <summary>Gets the address of the catalog root, which is where every host journey starts.</summary>
    public string Root(string? culture = null) => To(DemoCatalogDestinations.Overview, culture);

    /// <summary>
    /// Gets the address of the productivity workbench pinned to one render mode. These routes are
    /// test-owned: the visitor-facing destination is <see cref="DemoCatalogDestinations.Productivity" />.
    /// </summary>
    public string ProductivityRenderMode(string renderMode, string? culture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(renderMode);
        return TestRoute($"productivity/{renderMode}", culture);
    }

    /// <summary>
    /// Gets the address of the navigation-drawer lifecycle harness, a test-owned route that hosts
    /// drawer configurations no visitor-facing page offers.
    /// </summary>
    public string NavigationDrawerLifecycle(string? culture = null) =>
        TestRoute("test/navigation-drawer", culture);

    /// <summary>Gets the address of a render-mode catalog route named by its route segment.</summary>
    public string RenderMode(string routeSegment, string? culture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeSegment);
        return TestRoute($"render-modes/{routeSegment}", culture);
    }

    /// <summary>
    /// Gets a destination's address with a query a test writes itself, for the tests that assert how
    /// the host reacts to a malformed or unsupported query.
    /// </summary>
    public string WithRawQuery(DemoCatalogDestination destination, string query)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        return Absolute($"{destination.Route}?{query.TrimStart('?')}");
    }

    private string TestRoute(string relativePath, string? culture) =>
        Absolute(DemoCulture.PreserveCulture(CurrentUri(culture), _baseUri, relativePath));

    // The seam reads the visitor's culture from the address they are on, so a test states the
    // culture it wants by standing on a URL that carries it.
    private Uri CurrentUri(string? culture) => culture is null
        ? _baseUri
        : new Uri($"{_baseUri}?culture={culture}");

    private string Absolute(string relativeOrRootedPath) =>
        new Uri(_baseUri, relativeOrRootedPath).ToString();

    private sealed class TestNavigationManager : NavigationManager
    {
        internal TestNavigationManager(string baseUri, string uri) => Initialize(baseUri, uri);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
