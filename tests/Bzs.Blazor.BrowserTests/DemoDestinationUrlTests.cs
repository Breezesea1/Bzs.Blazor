using Bzs.Blazor.Demo.Client;

namespace Bzs.Blazor.BrowserTests;

/// <summary>
/// Pins the addresses browser tests navigate to, including the standalone host's base path, so a
/// mistake in the seam adapter shows up here rather than as a page of 404s across the suite.
/// </summary>
public sealed class DemoDestinationUrlTests
{
    [Fact]
    public void ADestinationCarriesTheRequestedCulture()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001");

        Assert.Equal(
            "http://127.0.0.1:5001/forms?culture=en-US",
            urls.To(DemoCatalogDestinations.Forms, DemoDestinationUrls.English));
        Assert.Equal(
            "http://127.0.0.1:5001/forms?culture=zh-Hans",
            urls.To(DemoCatalogDestinations.Forms, DemoDestinationUrls.Chinese));
    }

    [Fact]
    public void ADestinationWithoutACultureLeavesTheHostToChoose()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001");

        Assert.Equal("http://127.0.0.1:5001/forms", urls.To(DemoCatalogDestinations.Forms));
    }

    [Fact]
    public void TheStandaloneHostBasePathSurvives()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001/Bzs.Blazor");

        Assert.Equal(
            "http://127.0.0.1:5001/Bzs.Blazor/forms?culture=en-US",
            urls.To(DemoCatalogDestinations.Forms, DemoDestinationUrls.English));
        Assert.Equal(
            "http://127.0.0.1:5001/Bzs.Blazor/render-modes/webassembly?culture=en-US",
            urls.To(DemoCatalogDestinations.InteractiveWebAssembly, DemoDestinationUrls.English));
    }

    [Fact]
    public void TheRootIsTheOverviewDestination()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001");

        Assert.Equal("http://127.0.0.1:5001/?culture=en-US", urls.Root(DemoDestinationUrls.English));
        Assert.Equal("http://127.0.0.1:5001/", urls.Root());
    }

    [Fact]
    public void TheStandaloneRootKeepsItsBasePath()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001/Bzs.Blazor");

        Assert.Equal(
            "http://127.0.0.1:5001/Bzs.Blazor/?culture=en-US",
            urls.Root(DemoDestinationUrls.English));
        Assert.Equal("http://127.0.0.1:5001/Bzs.Blazor/", urls.Root());
    }

    [Fact]
    public void ADestinationCarriesItsOwnQueryAndFragment()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001");

        Assert.Equal(
            "http://127.0.0.1:5001/productivity?view=assigned&culture=en-US",
            urls.To(DemoCatalogDestinations.Productivity, DemoDestinationUrls.English, query: "view=assigned"));
        Assert.Equal(
            "http://127.0.0.1:5001/releases?culture=en-US#release-030",
            urls.To(DemoCatalogDestinations.Releases, DemoDestinationUrls.English, fragment: "release-030"));
    }

    [Fact]
    public void ATestOwnedRouteAlsoCarriesTheCultureAndBasePath()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001/Bzs.Blazor");

        Assert.Equal(
            "http://127.0.0.1:5001/Bzs.Blazor/productivity/static?culture=zh-Hans",
            urls.ProductivityRenderMode("static", DemoDestinationUrls.Chinese));
        Assert.Equal(
            "http://127.0.0.1:5001/Bzs.Blazor/test/navigation-drawer?culture=en-US",
            urls.NavigationDrawerLifecycle(DemoDestinationUrls.English));
        Assert.Equal(
            "http://127.0.0.1:5001/Bzs.Blazor/render-modes/auto?culture=en-US",
            urls.RenderMode("auto", DemoDestinationUrls.English));
        Assert.Equal(
            "http://127.0.0.1:5001/Bzs.Blazor/productivity/server",
            urls.ProductivityRenderMode("server"));
    }

    [Fact]
    public void ARawQueryReachesTheHostUnchanged()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001");

        Assert.Equal(
            "http://127.0.0.1:5001/forms?culture=invalid",
            urls.WithRawQuery(DemoCatalogDestinations.Forms, "culture=invalid"));
        Assert.Equal(
            "http://127.0.0.1:5001/forms?next=culture=zh-Hans&culture=en-US",
            urls.WithRawQuery(DemoCatalogDestinations.Forms, "next=culture=zh-Hans&culture=en-US"));
    }

    [Fact]
    public void ARawQueryKeepsTheStandaloneBasePath()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001/Bzs.Blazor");

        Assert.Equal(
            "http://127.0.0.1:5001/Bzs.Blazor/forms?culture=invalid",
            urls.WithRawQuery(DemoCatalogDestinations.Forms, "culture=invalid"));
    }

    [Fact]
    public void ADestinationOrdersItsQueryTheCultureAndItsFragment()
    {
        var urls = new DemoDestinationUrls("http://127.0.0.1:5001");

        Assert.Equal(
            "http://127.0.0.1:5001/releases?view=all&culture=en-US#release-030",
            urls.To(
                DemoCatalogDestinations.Releases,
                DemoDestinationUrls.English,
                query: "view=all",
                fragment: "release-030"));
    }
}
