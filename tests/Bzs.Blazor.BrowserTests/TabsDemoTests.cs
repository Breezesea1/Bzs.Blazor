using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class TabsDemoTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task AutomaticTabsExposeRolesRelationshipsAndSkipDisabledTabs()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1440, 900);
        await GoToTabsAsync();

        var tabs = Page.GetByTestId("automatic-tabs");
        var tabList = tabs.GetByRole(AriaRole.Tablist);
        var overview = tabs.GetByRole(AriaRole.Tab, new() { Name = "Overview" });
        var disabledSchedule = tabs.GetByRole(AriaRole.Tab, new() { Name = "Schedule unavailable" });
        var activity = tabs.GetByRole(AriaRole.Tab, new() { Name = "Activity" });

        await Expect(tabList).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await Expect(tabList).ToHaveAttributeAsync("aria-label", "Automatic project tabs");
        await Expect(disabledSchedule).ToBeDisabledAsync();

        var panelId = await overview.GetAttributeAsync("aria-controls");
        Assert.False(string.IsNullOrWhiteSpace(panelId));
        var overviewPanel = tabs.Locator($"#{panelId}");
        await Expect(overviewPanel).ToHaveAttributeAsync("role", "tabpanel");
        var overviewId = await overview.GetAttributeAsync("id");
        Assert.False(string.IsNullOrWhiteSpace(overviewId));
        await Expect(overviewPanel).ToHaveAttributeAsync("aria-labelledby", overviewId);

        await overview.FocusAsync();
        await overview.PressAsync("ArrowRight");
        await Expect(activity).ToBeFocusedAsync();
        await Expect(activity).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.GetByTestId("automatic-selection")).ToHaveTextAsync("Selected: activity");

        await activity.PressAsync("Home");
        await Expect(overview).ToBeFocusedAsync();
        await Expect(overview).ToHaveAttributeAsync("aria-selected", "true");

        await overview.PressAsync("End");
        await Expect(activity).ToBeFocusedAsync();
        await Expect(activity).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task ManualTabsRequireEnterOrSpaceAndTabLeavesTheTabList()
    {
        BeginBrowserGateTest();
        await GoToTabsAsync();

        var automaticTabs = Page.GetByTestId("automatic-tabs");
        var automaticOverview = automaticTabs.GetByRole(AriaRole.Tab, new() { Name = "Overview" });
        await automaticOverview.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(automaticTabs.GetByRole(AriaRole.Tabpanel)).ToBeFocusedAsync();

        var manualTabs = Page.GetByTestId("manual-tabs");
        var profile = manualTabs.GetByRole(AriaRole.Tab, new() { Name = "Profile" });
        var preferences = manualTabs.GetByRole(AriaRole.Tab, new() { Name = "Preferences" });
        var security = manualTabs.GetByRole(AriaRole.Tab, new() { Name = "Security" });

        await profile.FocusAsync();
        await profile.PressAsync("ArrowDown");
        await Expect(preferences).ToBeFocusedAsync();
        await Expect(profile).ToHaveAttributeAsync("aria-selected", "true");
        await preferences.PressAsync("Enter");
        await Expect(preferences).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.GetByTestId("manual-selection")).ToHaveTextAsync("Selected: preferences");

        await preferences.PressAsync("ArrowDown");
        await Expect(security).ToBeFocusedAsync();
        await security.PressAsync("Space");
        await Expect(security).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.GetByTestId("manual-selection")).ToHaveTextAsync("Selected: security");
    }

    [Fact]
    public async Task RtlArrowRightFollowsThePhysicalRightDirectionAndChineseContentIsVisible()
    {
        BeginBrowserGateTest();
        await GoToTabsAsync();

        var chineseTabs = Page.GetByTestId("chinese-tabs");
        await Expect(chineseTabs).ToHaveAttributeAsync("lang", "zh-Hans");
        await Expect(chineseTabs.GetByRole(AriaRole.Tab, new() { Name = "概览" })).ToBeVisibleAsync();
        await Expect(chineseTabs.GetByRole(AriaRole.Tabpanel)).ToContainTextAsync("项目概览包含当前进度、负责人和下一次评审日期。");

        var rtlTabs = Page.GetByTestId("rtl-tabs");
        var summary = rtlTabs.GetByRole(AriaRole.Tab, new() { Name = "ملخص" });
        var decisions = rtlTabs.GetByRole(AriaRole.Tab, new() { Name = "قرارات" });
        Assert.Equal("rtl", await rtlTabs.EvaluateAsync<string>("element => getComputedStyle(element).direction"));
        await Expect(rtlTabs).Not.ToHaveAttributeAsync("dir", "rtl");

        await summary.FocusAsync();
        await summary.PressAsync("ArrowRight");
        await Expect(decisions).ToBeFocusedAsync();
        await Expect(decisions).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.GetByTestId("rtl-selection")).ToHaveTextAsync("المحدد: decisions");
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task TabsFitWithoutHorizontalPageOverflowAtDesktopAndMobileWidths(int width, int height)
    {
        BeginBrowserGateTest($"{width}x{height}");
        await Page.SetViewportSizeAsync(width, height);
        await GoToTabsAsync();

        var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.False(hasHorizontalOverflow);
        await Expect(Page.GetByTestId("manual-tabs").GetByRole(AriaRole.Tablist))
            .ToHaveAttributeAsync("aria-orientation", "vertical");
    }

    [Fact]
    public async Task StaticPrerenderContainsTabsPanelsAndSelectedContent()
    {
        BeginBrowserGateTest();
        using var client = new HttpClient();
        var html = await client.GetStringAsync($"{server.BaseUrl}/tabs");

        Assert.Contains("role=\"tablist\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"tab\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"tabpanel\"", html, StringComparison.Ordinal);
        Assert.Contains("Overview is active", html, StringComparison.Ordinal);
    }

    private async Task GoToTabsAsync()
    {
        await Page.GotoAsync($"{server.BaseUrl}/tabs");
        await Expect(Page.GetByTestId("tabs-runtime-status"))
            .ToHaveTextAsync("Interactive runtime ready");
    }
}
