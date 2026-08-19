using System.Globalization;
using Bzs.Blazor.Demo.Client;
using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.BrowserTests;

public sealed class DemoCatalogDestinationTests
{
    [Fact]
    public void FullDemoNavigationHasStableDistinctDestinations()
    {
        var sections = DemoCatalogDestinations.GetNavigationSections(
            DemoCatalogHostCapabilities.SharedCatalog
            | DemoCatalogHostCapabilities.FullRenderModes);

        Assert.Equal(3, sections.Count);
        Assert.Equal(
            ["overview", "theme-foundation", "foundation", "forms", "productivity", "feedback", "tabs", "overlays", "layout", "navigation-drawer"],
            sections[0].Destinations.Select(destination => destination.Id));
        Assert.Equal(["releases"], sections[1].Destinations.Select(destination => destination.Id));
        Assert.Equal(
            ["static-ssr", "interactive-server", "interactive-webassembly", "interactive-auto"],
            sections[2].Destinations.Select(destination => destination.Id));

        var destinations = sections.SelectMany(section => section.Destinations).ToArray();
        Assert.DoesNotContain(
            destinations,
            destination => string.IsNullOrWhiteSpace(destination.Route));
        Assert.Equal(destinations.Length, destinations.Select(destination => destination.Id).Distinct().Count());
        Assert.Equal(destinations.Length, destinations.Select(destination => destination.Route).Distinct().Count());
    }

    [Fact]
    public void StandaloneNavigationKeepsSharedDestinationsAndOnlyItsRuntime()
    {
        var sections = DemoCatalogDestinations.GetNavigationSections(
            DemoCatalogHostCapabilities.SharedCatalog
            | DemoCatalogHostCapabilities.StandaloneRuntime);

        Assert.Equal(10, sections[0].Destinations.Count);
        Assert.Equal(["releases"], sections[1].Destinations.Select(destination => destination.Id));
        Assert.Equal(
            ["interactive-webassembly"],
            sections[2].Destinations.Select(destination => destination.Id));
    }

    [Fact]
    public void RuntimePresentationFollowsHostCapabilities()
    {
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var full = DemoCatalogDestinations.GetRuntimePresentation(
                DemoCatalogHostCapabilities.SharedCatalog
                | DemoCatalogHostCapabilities.FullRenderModes);
            Assert.Equal("Render modes", full.SectionName);
            Assert.Equal(
                [
                    ("static-ssr", "Meaningful passive markup."),
                    ("interactive-server", "Server circuit interaction."),
                    ("interactive-webassembly", "Browser-hosted interaction."),
                    ("interactive-auto", "Automatic server-to-browser selection."),
                ],
                full.Destinations.Select(item => (item.Destination.Id, item.Description)));

            var standalone = DemoCatalogDestinations.GetRuntimePresentation(
                DemoCatalogHostCapabilities.SharedCatalog
                | DemoCatalogHostCapabilities.StandaloneRuntime);
            Assert.Equal("Runtime", standalone.SectionName);
            Assert.Equal(
                [("interactive-webassembly", "Browser-hosted interaction without a server runtime.")],
                standalone.Destinations.Select(item => (item.Destination.Id, item.Description)));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void HrefPreservesBasePathCultureQueryAndFragment()
    {
        var navigation = new TestNavigationManager(
            "https://demo.example/catalog/",
            "https://demo.example/catalog/forms?culture=en-US");

        Assert.Equal(
            "/catalog/productivity?view=assigned&culture=en-US",
            DemoCatalogDestinations.GetHref(
                navigation,
                DemoCatalogDestinations.Productivity,
                query: "view=assigned"));
        Assert.Equal(
            "/catalog/releases?culture=en-US#release-030",
            DemoCatalogDestinations.GetHref(
                navigation,
                DemoCatalogDestinations.Releases,
                fragment: "release-030"));
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        internal TestNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
