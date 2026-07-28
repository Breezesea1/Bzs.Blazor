using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class RenderModeCatalogTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task StaticSsrReturnsLibraryMarkupWithoutAnInteractiveBrowserStep()
    {
        BeginBrowserGateTest();
        using var client = new HttpClient();
        var response = await client.GetAsync($"{server.BaseUrl}/render-modes/static");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Static server rendering", html, StringComparison.Ordinal);
        Assert.Contains("data-bzs-surface=\"raised\"", html, StringComparison.Ordinal);
        Assert.Contains("data-bzs-message-severity=\"information\"", html, StringComparison.Ordinal);
        Assert.Contains("data-bzs-progress=\"determinate\"", html, StringComparison.Ordinal);
        Assert.Contains("data-bzs-tabs=\"horizontal\"", html, StringComparison.Ordinal);
        Assert.Contains("data-bzs-overlay-host=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"render-mode-static-work-item\"", html, StringComparison.Ordinal);
        Assert.Contains("<select", html, StringComparison.Ordinal);
        Assert.Contains("value=\"review\"", html, StringComparison.Ordinal);
        Assert.Contains("Controlled dialog is closed.", html, StringComparison.Ordinal);
        Assert.Contains("Interaction unavailable", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("server", "Interactive Server", "Server circuit")]
    [InlineData("webassembly", "Interactive WebAssembly", "Browser WebAssembly")]
    [InlineData("auto", "Interactive Auto", "Automatic server or browser runtime")]
    public async Task InteractiveRenderModesActivateTheCatalogAndOverlayHost(
        string route,
        string runtimeName,
        string runtimeStatus)
    {
        BeginBrowserGateTest(route);
        var consoleErrors = new List<string>();
        var failedRequests = new List<string>();
        var missingAssets = new List<string>();
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                consoleErrors.Add(message.Text);
            }
        };
        Page.RequestFailed += (_, request) =>
            failedRequests.Add($"{request.Method} {request.Url}: {request.Failure}");
        Page.Response += (_, response) =>
        {
            if (response.Status >= 400
                && response.Request.ResourceType is "script" or "stylesheet" or "wasm" or "font")
            {
                missingAssets.Add($"{response.Status} {response.Url}");
            }
        };

        var testId = $"render-mode-{route}";
        var response = await Page.GotoAsync($"{server.BaseUrl}/render-modes/{route}");

        Assert.NotNull(response);
        Assert.True(response.Ok);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = runtimeName })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId($"{testId}-runtime-status")).ToHaveTextAsync(runtimeStatus);
        await Expect(Page.GetByTestId($"{testId}-runtime-readiness"))
            .ToHaveTextAsync("Interactive runtime ready");

        await Page.GetByTestId($"{testId}-counter").ClickAsync();
        await Expect(Page.GetByTestId($"{testId}-counter")).ToHaveTextAsync("Interaction count: 1");

        var theme = Page.GetByTestId($"{testId}-theme");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Dark" }).ClickAsync();
        await Expect(theme).ToHaveAttributeAsync("data-bzs-theme", "dark");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Comfortable" }).ClickAsync();
        await Expect(theme).ToHaveAttributeAsync("data-bzs-density", "comfortable");

        var workItem = Page.GetByTestId($"{testId}-work-item");
        await workItem.FillAsync("Lighting review");
        await workItem.PressAsync("Tab");
        await Page.GetByTestId($"{testId}-save").ClickAsync();
        await Expect(Page.GetByTestId($"{testId}-form-status")).ToHaveTextAsync("Saved Lighting review.");

        var tabs = Page.GetByTestId($"{testId}-tabs");
        var details = tabs.GetByRole(AriaRole.Tab, new() { Name = "Details" });
        await details.ClickAsync();
        await Expect(details).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.GetByTestId($"{testId}-tabs-status")).ToHaveTextAsync("Active tab: details");

        await Page.GetByTestId($"{testId}-open-controlled-dialog").ClickAsync();
        var controlledDialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled catalog dialog" });
        await Expect(controlledDialog).ToBeVisibleAsync();
        await controlledDialog.GetByTestId($"{testId}-complete-controlled-dialog").ClickAsync();
        await Expect(controlledDialog).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId($"{testId}-controlled-dialog-status"))
            .ToHaveTextAsync("Controlled dialog completed.");

        await Page.GetByTestId($"{testId}-open-drawer").ClickAsync();
        var drawer = Page.GetByRole(AriaRole.Dialog, new() { Name = "Catalog drawer" });
        await Expect(drawer).ToBeVisibleAsync();
        await drawer.GetByTestId($"{testId}-close-drawer").ClickAsync();
        await Expect(drawer).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId($"{testId}-drawer-status")).ToHaveTextAsync("Drawer is closed.");

        await Page.GetByTestId($"{testId}-open-service-dialog").ClickAsync();
        var serviceDialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Catalog service dialog" });
        await Expect(serviceDialog).ToBeVisibleAsync();
        await serviceDialog.GetByTestId("service-dialog-complete").ClickAsync();
        await Expect(serviceDialog).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId($"{testId}-service-dialog-status")).ToHaveTextAsync("Completed: true");

        await Page.GetByTestId($"{testId}-show-toast").ClickAsync();
        await Expect(Page.GetByTestId($"{testId}-overlay-host"))
            .ToContainTextAsync("Catalog toast");

        Assert.Empty(consoleErrors);
        Assert.Empty(failedRequests);
        Assert.Empty(missingAssets);
    }
}
