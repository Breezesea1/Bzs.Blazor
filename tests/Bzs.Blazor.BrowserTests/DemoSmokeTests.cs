using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System.Text.RegularExpressions;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class DemoSmokeTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task CatalogExposesTheRenderModeRoutes()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync(server.BaseUrl);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Bzs.Blazor" }))
            .ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Static SSR" }).First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Static SSR" }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Interaction unavailable" }))
            .ToBeDisabledAsync();
    }

    [Theory]
    [InlineData("01 Theme foundation", "theme-foundation", "Light, Dark, and System")]
    [InlineData("02 Foundation", "foundation", "Icon, Surface, and Button")]
    [InlineData("03 Forms", "forms", "Profile editor")]
    [InlineData("04 Feedback", "feedback", "Status and notifications")]
    [InlineData("05 Tabs", "tabs", "Tabs, language, and direction")]
    [InlineData("06 Overlays", "overlays", "Dialog, Drawer, and Host")]
    [InlineData("07 Layout", "layout", "Container, Grid, and Stack")]
    public async Task CatalogComponentGroupLinksNavigateToTheirSamples(
        string linkName,
        string route,
        string pageHeading)
    {
        BeginBrowserGateTest();
        await Page.GotoAsync(server.BaseUrl);

        var componentGroups = Page.GetByRole(AriaRole.Region, new() { Name = "Component groups" });
        await componentGroups.GetByRole(AriaRole.Link, new() { Name = linkName, Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex($"/{Regex.Escape(route)}$"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = pageHeading }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task ThemeFoundationHonorsCspSystemModeAndReducedMotion()
    {
        BeginBrowserGateTest();
        await Page.EmulateMediaAsync(new()
        {
            ColorScheme = ColorScheme.Dark,
            ReducedMotion = ReducedMotion.Reduce,
        });

        var response = await Page.GotoAsync($"{server.BaseUrl}/theme-foundation");
        Assert.NotNull(response);
        var styleDirective = response.Headers["content-security-policy"]
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Single(directive => directive.StartsWith("style-src", StringComparison.Ordinal));
        Assert.Equal("style-src 'self'", styleDirective);

        var provider = Page.Locator(".bzs-theme-provider");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Light, Dark, and System" }))
            .ToBeVisibleAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "light");
        Assert.Equal(0, await provider.Locator("style").CountAsync());
        await Expect(Page.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Interactive runtime ready");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Dark" }).ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Light" }).ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "light");

        await Page.GetByRole(AriaRole.Button, new() { Name = "System" }).ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        var primary = await provider.EvaluateAsync<string>(
            "element => getComputedStyle(element).getPropertyValue('--bzs-primary').trim()");
        var motion = await provider.EvaluateAsync<string>(
            "element => getComputedStyle(element).getPropertyValue('--bzs-motion-normal').trim()");
        Assert.Equal("#0f766e", primary);
        Assert.Equal("0ms", motion);
    }

    [Fact]
    public async Task FoundationComponentsActivateAfterAutoHydration()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/foundation");
        await Expect(Page.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Interactive runtime ready");

        var action = Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 0" });
        await action.PressAsync("Enter");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 1" }))
            .ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 1" }).PressAsync("Space");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 2" }))
            .ToBeVisibleAsync();
        var compactHeight = await Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 2" })
            .EvaluateAsync<double>("element => element.getBoundingClientRect().height");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Comfortable density" }).ClickAsync();
        var provider = Page.Locator(".bzs-theme-provider");
        await Expect(provider).ToHaveAttributeAsync("data-bzs-density", "comfortable");
        var comfortableHeight = await Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 2" })
            .EvaluateAsync<double>("element => element.getBoundingClientRect().height");
        Assert.True(comfortableHeight > compactHeight);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Saving" }))
            .ToBeDisabledAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Close example" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task LayoutPrimitivesRespondAcrossViewports()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync($"{server.BaseUrl}/layout");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Container, Grid, and Stack" }))
            .ToBeVisibleAsync();

        var grid = Page.Locator("#layout-responsive-grid");
        var production = Page.Locator("#layout-production");
        var review = Page.Locator("#layout-review");
        var archive = Page.Locator("#layout-archive");
        await Expect(production).ToBeVisibleAsync();
        await Expect(review).ToBeVisibleAsync();
        await Expect(archive).ToBeVisibleAsync();
        var productionMobile = await GetVisibleBoxAsync(production);
        var reviewMobile = await GetVisibleBoxAsync(review);
        var archiveMobile = await GetVisibleBoxAsync(archive);

        Assert.True(reviewMobile.Y > productionMobile.Y);
        Assert.True(archiveMobile.Y > reviewMobile.Y);
        Assert.InRange(Math.Abs(productionMobile.Width - reviewMobile.Width), 0, 1);

        await Page.SetViewportSizeAsync(1280, 900);
        var productionDesktop = await GetVisibleBoxAsync(production);
        var reviewDesktop = await GetVisibleBoxAsync(review);
        var archiveDesktop = await GetVisibleBoxAsync(archive);

        Assert.InRange(Math.Abs(productionDesktop.Y - reviewDesktop.Y), 0, 1);
        Assert.InRange(Math.Abs(reviewDesktop.Y - archiveDesktop.Y), 0, 1);
        Assert.InRange(Math.Abs(productionDesktop.Width - reviewDesktop.Width), 0, 1);
        Assert.True(productionDesktop.Width < productionMobile.Width);
        Assert.Equal("12px", await grid.EvaluateAsync<string>("element => getComputedStyle(element).gap"));

        var queueStack = Page.Locator("#layout-queue-stack");
        var queue = await GetVisibleBoxAsync(queueStack.GetByText("Queue", new() { Exact = true }));
        var itemCount = await GetVisibleBoxAsync(queueStack.GetByText("12 items", new() { Exact = true }));
        Assert.True(itemCount.X - (queue.X + queue.Width) > 100);

        var flexDivider = Page.Locator("#layout-flex-divider");
        var naturalDivider = Page.Locator("#layout-natural-divider");
        Assert.Equal(
            "stretch",
            await flexDivider.EvaluateAsync<string>("element => getComputedStyle(element).alignSelf"));
        Assert.NotEqual(
            "stretch",
            await naturalDivider.EvaluateAsync<string>("element => getComputedStyle(element).alignSelf"));
        var flexDividerBox = await GetVisibleBoxAsync(flexDivider);
        var naturalDividerBox = await GetVisibleBoxAsync(naturalDivider);
        Assert.True(flexDividerBox.Height > naturalDividerBox.Height + 8);

        var absoluteBoundary = await GetVisibleBoxAsync(Page.Locator("#layout-absolute-boundary"));
        var absoluteDivider = await GetVisibleBoxAsync(Page.Locator("#layout-absolute-divider"));
        var blockStartInset = absoluteDivider.Y - absoluteBoundary.Y;
        var blockEndInset = absoluteBoundary.Y + absoluteBoundary.Height
            - absoluteDivider.Y - absoluteDivider.Height;
        Assert.True(blockStartInset > 0);
        Assert.True(blockEndInset > 0);
        Assert.InRange(Math.Abs(blockStartInset - blockEndInset), 0, 1);
    }

    private async Task<(double X, double Y, double Width, double Height)> GetVisibleBoxAsync(
        ILocator locator)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await Expect(locator).ToBeVisibleAsync();
            var box = await locator.BoundingBoxAsync();
            if (box is not null)
            {
                return (box.X, box.Y, box.Width, box.Height);
            }

            await Task.Delay(25);
        }

        throw new InvalidOperationException("The visible element did not expose a bounding box.");
    }
}
