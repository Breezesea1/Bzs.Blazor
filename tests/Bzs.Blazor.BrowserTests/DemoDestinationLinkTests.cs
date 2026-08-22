using Bzs.Blazor.Demo.Client;
using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.BrowserTests;

public sealed class DemoDestinationLinkTests
{
    [Fact]
    public void LinkKeepsBasePathAndCurrentCulture()
    {
        var links = CreateLinks(
            "https://demo.example/catalog/",
            "https://demo.example/catalog/forms?culture=en-US");

        Assert.Equal(
            "/catalog/productivity?culture=en-US",
            links.To(DemoCatalogDestinations.Productivity));
    }

    [Fact]
    public void LinkAddsDestinationQueryBeforeCulture()
    {
        var links = CreateLinks(
            "https://demo.example/catalog/",
            "https://demo.example/catalog/forms?culture=en-US");

        Assert.Equal(
            "/catalog/productivity?view=assigned&culture=en-US",
            links.To(DemoCatalogDestinations.Productivity, query: "view=assigned"));
    }

    [Fact]
    public void LinkKeepsFragmentAfterCulture()
    {
        var links = CreateLinks(
            "https://demo.example/catalog/",
            "https://demo.example/catalog/forms?culture=en-US");

        Assert.Equal(
            "/catalog/releases?culture=en-US#release-030",
            links.To(DemoCatalogDestinations.Releases, fragment: "release-030"));
    }

    [Fact]
    public void LinkOmitsCultureWhenTheVisitorHasNotChosenOne()
    {
        var links = CreateLinks(
            "https://demo.example/catalog/",
            "https://demo.example/catalog/forms");

        Assert.Equal("releases", links.To(DemoCatalogDestinations.Releases));
    }

    [Fact]
    public void LinkAcceptsCallerSuppliedPrefixes()
    {
        var links = CreateLinks(
            "https://demo.example/catalog/",
            "https://demo.example/catalog/forms?culture=en-US");

        Assert.Equal(
            "/catalog/productivity?view=assigned&culture=en-US#top",
            links.To(DemoCatalogDestinations.Productivity, query: "?view=assigned", fragment: "#top"));
    }

    [Fact]
    public void LinkRejectsAMissingDestination()
    {
        var links = CreateLinks(
            "https://demo.example/catalog/",
            "https://demo.example/catalog/forms");

        Assert.Throws<ArgumentNullException>(() => links.To(null!));
    }

    private static DemoDestinationLinks CreateLinks(string baseUri, string uri) =>
        new(new TestNavigationManager(baseUri, uri));

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
