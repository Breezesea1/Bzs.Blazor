using System.Globalization;
using Bzs.Blazor.Demo.Client;

namespace Bzs.Blazor.Tests;

public sealed class DemoCatalogChromeTests
{
    [Fact]
    public void FullHostChromeHasStableDistinctDestinations()
    {
        var sections = DemoCatalogChrome.GetSections(includesServerRenderModes: true);

        Assert.Equal(3, sections.Count);
        Assert.Equal(
            ["overview", "theme-foundation", "foundation", "forms", "productivity", "feedback", "tabs", "overlays", "layout", "navigation-drawer"],
            sections[0].Destinations.Select(entry => entry.Destination.Id));
        Assert.Equal(["releases"], sections[1].Destinations.Select(entry => entry.Destination.Id));
        Assert.Equal(
            ["static-ssr", "interactive-server", "interactive-webassembly", "interactive-auto"],
            sections[2].Destinations.Select(entry => entry.Destination.Id));

        var destinations = sections
            .SelectMany(section => section.Destinations)
            .Select(entry => entry.Destination)
            .ToArray();
        Assert.DoesNotContain(destinations, destination => string.IsNullOrWhiteSpace(destination.Route));
        Assert.Equal(destinations.Length, destinations.Select(destination => destination.Id).Distinct().Count());
        Assert.Equal(destinations.Length, destinations.Select(destination => destination.Route).Distinct().Count());
    }

    [Fact]
    public void StandaloneHostChromeKeepsSharedDestinationsAndOnlyItsRuntime()
    {
        var sections = DemoCatalogChrome.GetSections(includesServerRenderModes: false);

        Assert.Equal(10, sections[0].Destinations.Count);
        Assert.Equal(["releases"], sections[1].Destinations.Select(entry => entry.Destination.Id));
        Assert.Equal(
            ["interactive-webassembly"],
            sections[2].Destinations.Select(entry => entry.Destination.Id));
    }

    [Fact]
    public void RuntimeSectionFollowsHostCapabilities()
    {
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var full = DemoCatalogChrome.GetRuntimeSection(includesServerRenderModes: true);
            Assert.Equal("Render modes", full.Name);
            Assert.Equal(
                [
                    ("static-ssr", "Meaningful passive markup."),
                    ("interactive-server", "Server circuit interaction."),
                    ("interactive-webassembly", "Browser-hosted interaction."),
                    ("interactive-auto", "Automatic server-to-browser selection."),
                ],
                full.Destinations.Select(entry => (entry.Destination.Id, entry.Description)));

            var standalone = DemoCatalogChrome.GetRuntimeSection(includesServerRenderModes: false);
            Assert.Equal("Runtime", standalone.Name);
            Assert.Equal(
                [("interactive-webassembly", "Browser-hosted interaction without a server runtime.")],
                standalone.Destinations.Select(entry => (entry.Destination.Id, entry.Description)));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void ChromeNamesFollowTheActiveCultureWhileIdentityDoesNot()
    {
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var english = DemoCatalogChrome.Describe(DemoCatalogDestinations.Forms);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
            var chinese = DemoCatalogChrome.Describe(DemoCatalogDestinations.Forms);

            Assert.NotEqual(english.Name, chinese.Name);
            Assert.Equal("forms", english.Destination.Id);
            Assert.Equal("forms", chinese.Destination.Id);
            Assert.Equal(english.Destination.Route, chinese.Destination.Route);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void EveryDestinationHasChromeCopy()
    {
        DemoCatalogDestination[] destinations =
        [
            .. DemoCatalogDestinations.Catalog,
            .. DemoCatalogDestinations.Project,
            .. DemoCatalogDestinations.Runtimes,
        ];

        Assert.All(
            destinations,
            destination =>
            {
                var fullEntry = DemoCatalogChrome.Describe(destination, includesServerRenderModes: true);
                Assert.False(string.IsNullOrWhiteSpace(fullEntry.Name));

                if (destination.Id is not "overview" and not "releases")
                {
                    Assert.False(string.IsNullOrWhiteSpace(fullEntry.Description));
                }
            });

        var standaloneRuntime = DemoCatalogChrome.Describe(
            DemoCatalogDestinations.InteractiveWebAssembly,
            includesServerRenderModes: false);
        var fullRuntime = DemoCatalogChrome.Describe(
            DemoCatalogDestinations.InteractiveWebAssembly,
            includesServerRenderModes: true);
        Assert.NotEqual(fullRuntime.Description, standaloneRuntime.Description);
    }
}
