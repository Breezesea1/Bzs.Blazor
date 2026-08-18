using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class NavigationDrawerShowcaseTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task CatalogDrawerSupportsPointerKeyboardAndThemedScrolling()
    {
        BeginBrowserGateTest("resize");
        await Page.SetViewportSizeAsync(1280, 800);
        var response = await Page.GotoAsync($"{server.BaseUrl}/productivity/auto?culture=en-US");

        Assert.True(response?.Ok ?? false);
        var drawer = Page.Locator("#demo-navigation-drawer");
        var handle = drawer.GetByRole(
            AriaRole.Separator,
            new() { Name = "Resize navigation drawer", Exact = true });
        await Expect(handle).ToBeVisibleAsync();
        await Expect(handle).ToHaveAttributeAsync("aria-valuetext", "256 pixels");

        var appBar = Page.Locator("#demo-app-bar");
        var mainContent = Page.Locator("#main-content");
        var initialHandleBox = await handle.BoundingBoxAsync();
        var initialAppBarBox = await appBar.BoundingBoxAsync();
        var initialMainBox = await mainContent.BoundingBoxAsync();
        Assert.NotNull(initialHandleBox);
        Assert.NotNull(initialAppBarBox);
        Assert.NotNull(initialMainBox);

        await Page.Mouse.MoveAsync(
            initialHandleBox.X + (initialHandleBox.Width / 2),
            initialHandleBox.Y + (initialHandleBox.Height / 2));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(
            initialHandleBox.X + (initialHandleBox.Width / 2) + 64,
            initialHandleBox.Y + (initialHandleBox.Height / 2),
            new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await Expect(handle).ToHaveAttributeAsync("aria-valuenow", "320");
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "320px");
        await Expect(mainContent).ToHaveCSSAsync("margin-inline-start", "320px");

        var draggedHandleBox = await handle.BoundingBoxAsync();
        var draggedAppBarBox = await appBar.BoundingBoxAsync();
        var draggedMainBox = await mainContent.BoundingBoxAsync();
        Assert.NotNull(draggedHandleBox);
        Assert.NotNull(draggedAppBarBox);
        Assert.NotNull(draggedMainBox);
        Assert.InRange(draggedHandleBox.X - initialHandleBox.X, 63, 65);
        Assert.InRange(draggedAppBarBox.X - initialAppBarBox.X, 63, 65);
        Assert.InRange(draggedMainBox.X - initialMainBox.X, 63, 65);

        await handle.FocusAsync();
        await handle.PressAsync("ArrowRight");
        await Expect(handle).ToHaveAttributeAsync("aria-valuenow", "336");
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "336px");
        var keyboardHandleBox = await handle.BoundingBoxAsync();
        var keyboardAppBarBox = await appBar.BoundingBoxAsync();
        Assert.NotNull(keyboardHandleBox);
        Assert.NotNull(keyboardAppBarBox);
        Assert.InRange(keyboardHandleBox.X - draggedHandleBox.X, 15, 17);
        Assert.InRange(keyboardAppBarBox.X - draggedAppBarBox.X, 15, 17);

        await Page.Locator("#demo-app-shell").EvaluateAsync(
            "element => element.style.inlineSize = '180px'");
        await Expect(handle).ToHaveAttributeAsync("aria-valuemin", "180");
        await Expect(handle).ToHaveAttributeAsync("aria-valuemax", "180");
        await Expect(handle).ToHaveAttributeAsync("aria-valuenow", "180");
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "180px");
        await Expect(mainContent).ToHaveCSSAsync("margin-inline-start", "180px");

        await Page.Locator("#demo-app-shell").EvaluateAsync(
            "element => element.style.removeProperty('inline-size')");
        await Expect(handle).ToHaveAttributeAsync("aria-valuenow", "336");
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "336px");
        await Expect(mainContent).ToHaveCSSAsync("margin-inline-start", "336px");

        var scrollRegion = drawer.Locator("[data-bzs-navigation-drawer-scroll-region='true']");
        var scrollbar = await scrollRegion.EvaluateAsync<string[]>(
            "element => { const style = getComputedStyle(element); return [style.scrollbarWidth, style.scrollbarColor]; }");
        Assert.Equal("thin", scrollbar[0]);
        Assert.NotEqual("auto", scrollbar[1]);
        Assert.True(await scrollRegion.EvaluateAsync<bool>(
            "element => element.scrollHeight > element.clientHeight"));
        Assert.False(await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth"));
        AssertNoUnexpectedBrowserErrors("resizable catalog navigation drawer");
    }

    [Fact]
    public async Task PublicDrawerSupportsOpeningInitialFocusAndEscapeDismissal()
    {
        BeginBrowserGateTest();
        var response = await Page.GotoAsync($"{server.BaseUrl}/navigation-drawer?culture=en-US");

        Assert.True(response?.Ok ?? false);
        var showcase = Page.GetByTestId("navigation-drawer-showcase");
        await Expect(showcase).ToHaveAttributeAsync("data-bzs-interactive", "true");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Navigation drawer lifecycle", Exact = true }))
            .ToBeVisibleAsync();

        var drawer = Page.GetByTestId("navigation-drawer-showcase-drawer");
        var status = Page.GetByTestId("navigation-drawer-status");
        await Page.GetByTestId("navigation-drawer-open").ClickAsync();
        await Expect(drawer).Not.ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(status).ToContainTextAsync("Open");
        await Expect(Page.GetByTestId("navigation-drawer-primary-action")).ToBeFocusedAsync();

        await Page.GetByTestId("navigation-drawer-close").ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(status).ToContainTextAsync("Closed");

        await Page.GetByTestId("navigation-drawer-open").ClickAsync();
        await Expect(Page.GetByTestId("navigation-drawer-primary-action")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(drawer).ToHaveAttributeAsync("aria-hidden", "true");

        var acceptDismissals = Page.GetByRole(
            AriaRole.Switch,
            new() { Name = "Accept dismissal requests", Exact = true });
        await acceptDismissals.FocusAsync();
        await acceptDismissals.PressAsync("Space");
        await Expect(acceptDismissals).ToHaveAttributeAsync("aria-checked", "false");
        await Page.GetByTestId("navigation-drawer-open").ClickAsync();
        await Expect(Page.GetByTestId("navigation-drawer-primary-action")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(drawer).Not.ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.GetByTestId("navigation-drawer-status"))
            .ToContainTextAsync("Rejected; drawer remains open");
        await Page.GetByTestId("navigation-drawer-close").ClickAsync();
        AssertNoUnexpectedBrowserErrors("public navigation drawer lifecycle");
    }
}
