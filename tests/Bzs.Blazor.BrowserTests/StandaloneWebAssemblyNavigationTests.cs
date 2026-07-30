using System.Collections.Concurrent;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(StandaloneWebAssemblyCollection.Name)]
public sealed class StandaloneWebAssemblyNavigationTests(StandaloneWebAssemblyFixture server)
    : BrowserGatePageTest
{
    [Fact]
    public async Task DatePickerLanguageSwitchLoadsMatchingLocalizationResources()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms?culture=en-US");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "English", Exact = true }))
            .ToHaveAttributeAsync("aria-current", "page");

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        var panel = Page.GetByRole(AriaRole.Dialog, new() { Name = "Choose a date" });
        await Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Month" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        await Page.GetByRole(AriaRole.Link, new() { Name = "中文", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/forms?culture=zh-Hans");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "中文", Exact = true }))
            .ToHaveAttributeAsync("aria-current", "page");

        input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        panel = Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" });
        await Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "月份" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        var catalogNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Bzs.Blazor catalog", Exact = true });
        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "Feedback", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/feedback?culture=zh-Hans");
        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "Forms", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/forms?culture=zh-Hans");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "Overview", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/?culture=zh-Hans");
        var componentGroups = Page.GetByRole(AriaRole.Region, new() { Name = "Component groups" });
        await componentGroups.GetByRole(AriaRole.Link, new() { Name = "03 Forms", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/forms?culture=zh-Hans");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task OverlayHostSurvivesStandaloneWebAssemblyNavigation()
    {
        BeginBrowserGateTest();
        var consoleErrors = new ConcurrentQueue<string>();
        var pageErrors = new ConcurrentQueue<string>();
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                consoleErrors.Enqueue(message.Text);
            }
        };
        Page.PageError += (_, error) => pageErrors.Enqueue(error);

        var response = await Page.GotoAsync($"{server.BaseUrl}/render-modes/webassembly");

        Assert.NotNull(response);
        Assert.True(response.Ok);
        await AssertRuntimeReadyAsync(
            "render-mode-webassembly-runtime-readiness",
            consoleErrors,
            pageErrors);
        await AssertBlazorErrorUiHiddenAsync(consoleErrors, pageErrors);

        await ClickUniqueNavigationLinkAsync("Overlays");
        await AssertRuntimeReadyAsync(
            "overlays-runtime-status",
            consoleErrors,
            pageErrors);
        await AssertBlazorErrorUiHiddenAsync(consoleErrors, pageErrors);

        await Page.GetByTestId("open-service-dialog").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Service dialog" });
        await Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByTestId("service-dialog-complete").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("service-dialog-result")).ToHaveTextAsync("Completed: true");

        await Page.GetByTestId("show-host-toast").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Overlay host toast" }))
            .ToBeVisibleAsync();

        await ClickUniqueNavigationLinkAsync("Interactive WebAssembly");
        await AssertRuntimeReadyAsync(
            "render-mode-webassembly-runtime-readiness",
            consoleErrors,
            pageErrors);
        await AssertBlazorErrorUiHiddenAsync(consoleErrors, pageErrors);
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Overlay host toast" }))
            .ToBeVisibleAsync();

        Assert.Empty(consoleErrors);
        Assert.Empty(pageErrors);
    }

    private async Task ClickUniqueNavigationLinkAsync(string accessibleName)
    {
        var navigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Bzs.Blazor catalog", Exact = true });
        var link = navigation.GetByRole(
            AriaRole.Link,
            new() { Name = accessibleName, Exact = true });

        await Expect(link).ToHaveCountAsync(1);
        await link.ClickAsync();
    }

    private async Task AssertRuntimeReadyAsync(
        string statusTestId,
        ConcurrentQueue<string> consoleErrors,
        ConcurrentQueue<string> pageErrors)
    {
        await Page.WaitForFunctionAsync(
            """
            testId => {
                const status = document.querySelector(`[data-testid="${testId}"]`);
                const errorUi = document.querySelector('#blazor-error-ui');
                return status?.textContent?.trim() === 'Interactive runtime ready'
                    || (errorUi && getComputedStyle(errorUi).display !== 'none');
            }
            """,
            statusTestId,
            new() { Timeout = 30_000 });

        var status = Page.GetByTestId(statusTestId);
        var ready = string.Equals(
            (await status.TextContentAsync())?.Trim(),
            "Interactive runtime ready",
            StringComparison.Ordinal);
        Assert.True(ready, BuildBrowserErrorMessage(consoleErrors, pageErrors));
    }

    private async Task AssertBlazorErrorUiHiddenAsync(
        ConcurrentQueue<string> consoleErrors,
        ConcurrentQueue<string> pageErrors)
    {
        Assert.True(
            await Page.Locator("#blazor-error-ui").IsHiddenAsync(),
            BuildBrowserErrorMessage(consoleErrors, pageErrors));
    }

    private static string BuildBrowserErrorMessage(
        ConcurrentQueue<string> consoleErrors,
        ConcurrentQueue<string> pageErrors)
    {
        var errors = consoleErrors
            .Select(error => $"Console error: {error}")
            .Concat(pageErrors.Select(error => $"Page error: {error}"))
            .ToArray();
        return errors.Length == 0
            ? "The page failed before its interactive runtime became ready."
            : string.Join(Environment.NewLine, errors);
    }
}
