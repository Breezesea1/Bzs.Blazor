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
}
