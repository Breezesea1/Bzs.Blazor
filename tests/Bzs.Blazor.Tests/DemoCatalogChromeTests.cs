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
            var english = DemoCatalogChrome.Describe(
                DemoCatalogDestinations.Forms,
                includesServerRenderModes: true);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
            var chinese = DemoCatalogChrome.Describe(
                DemoCatalogDestinations.Forms,
                includesServerRenderModes: true);

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
    public void ComponentGroupsFollowHostCapabilities()
    {
        Assert.Equal(
            DemoCatalogChrome.GetComponentGroups(includesServerRenderModes: true)
                .Select(entry => entry.Destination.Id),
            DemoCatalogChrome.GetComponentGroups(includesServerRenderModes: false)
                .Select(entry => entry.Destination.Id));

        Assert.All(
            DemoCatalogDestinations.ComponentGroups,
            destination => Assert.True(
                destination.IsAvailable(DemoCatalogHostCapabilities.StandaloneRuntime
                    | DemoCatalogHostCapabilities.SharedCatalog)));
    }

    [Theory]
    [InlineData(
        "zh-Hans",
        new[] { "目录", "项目", "渲染模式" },
        new[]
        {
            "概览", "主题基础", "基础组件", "表单", "生产力", "反馈", "选项卡", "浮层", "布局", "导航抽屉",
            "版本发布",
            "静态 SSR", "交互式服务器", "交互式 WebAssembly", "交互式自动",
        })]
    [InlineData(
        "en-US",
        new[] { "Catalog", "Project", "Render modes" },
        new[]
        {
            "Overview", "Theme foundation", "Foundation components", "Forms", "Productivity", "Feedback",
            "Tabs", "Overlays", "Layout", "Navigation drawer",
            "Releases",
            "Static SSR", "Interactive Server", "Interactive WebAssembly", "Interactive Auto",
        })]
    public void ChromeCopyMatchesTheVisitorFacingWording(
        string cultureName,
        string[] expectedSections,
        string[] expectedNames)
    {
        // Browser assertions read their expectations from this module, so wholesale copy drift is
        // only observable here.
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var sections = DemoCatalogChrome.GetSections(includesServerRenderModes: true);

            Assert.Equal(expectedSections, sections.Select(section => section.Name));
            Assert.Equal(
                expectedNames,
                sections.SelectMany(section => section.Destinations).Select(entry => entry.Name));
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
