using System.Collections.Concurrent;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(StandaloneWebAssemblyCollection.Name)]
public sealed class StandaloneWebAssemblyNavigationTests(StandaloneWebAssemblyFixture server)
    : BrowserGatePageTest
{
    [Fact]
    public async Task BareVisitUsesChineseAndExplicitEnglishUpdatesDocumentLanguage()
    {
        BeginBrowserGateTest();

        await Page.GotoAsync(server.BaseUrl);
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "zh-Hans");

        await Page.GotoAsync($"{server.BaseUrl}?culture=en-US");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "en-US");
        var language = Page.GetByRole(
            AriaRole.Radiogroup,
            new() { Name = "Catalog language", Exact = true });
        await Page.GotoAsync($"{server.BaseUrl}/forms?culture=en-US");
        await Expect(language.GetByRole(AriaRole.Radio, new() { Name = "English", Exact = true }))
            .ToBeCheckedAsync();
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("?culture=en-US", false)]
    public async Task CatalogChromeUsesTheRequestedCulture(string query, bool isChinese)
    {
        BeginBrowserGateTest(isChinese ? "zh-Hans" : "en-US");

        await Page.GotoAsync($"{server.BaseUrl}{query}");

        await AssertDemoChromeAsync(
            isChinese,
            includesServerRenderModes: false,
            isChinese ? "静态 WebAssembly 主机" : "Static WebAssembly host");

        await Page.SetViewportSizeAsync(390, 844);
        await Expect(Page.GetByRole(
            AriaRole.Button,
            new() { Name = isChinese ? "打开导航" : "Open navigation", Exact = true }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task HostShellUsesBzsLayoutAndControlledNavigation()
    {
        BeginBrowserGateTest();
        await Page.AddInitScriptAsync(
            "localStorage.removeItem('bzs-demo-sidebar-collapsed')");
        await Page.SetViewportSizeAsync(1280, 900);
        await Page.GotoAsync($"{server.BaseUrl}?culture=en-US");
        var shell = Page.Locator("#demo-app-shell");
        var drawer = Page.Locator("#demo-navigation-drawer");
        var appBar = Page.Locator("#demo-app-bar");
        var mainContent = Page.Locator("#main-content");
        await Expect(shell).ToHaveAttributeAsync("data-bzs-app-shell", "true");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(appBar).ToHaveAttributeAsync("data-bzs-app-bar", "surface");
        await Expect(mainContent).ToHaveAttributeAsync("data-bzs-main-content", "landmark");
        await Expect(Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Bzs.Blazor catalog", Exact = true }))
            .ToBeVisibleAsync();

        await Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Close navigation", Exact = true })
            .ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        var openNavigation = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Open navigation", Exact = true });
        await Expect(openNavigation).ToBeFocusedAsync();
        await openNavigation.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");

        await Page.SetViewportSizeAsync(390, 844);
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        await openNavigation.ClickAsync();
        await Expect(appBar).ToHaveAttributeAsync("inert", "");
        await Expect(mainContent).ToHaveAttributeAsync("inert", "");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        await Expect(openNavigation).ToBeFocusedAsync();
    }

    [Fact]
    public async Task BrandBlockShowsLogoAndFaviconResolvesToServedAsset()
    {
        BeginBrowserGateTest();
        var response = await Page.GotoAsync(server.BaseUrl);
        Assert.NotNull(response);
        Assert.True(response.Ok);

        await AssertBrandBlockShowsLogoAndFaviconResolvesToServedAssetAsync();
    }

    [Fact]
    public async Task DatePickerLanguageSwitchLoadsMatchingLocalizationResources()
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

        await Page.GotoAsync($"{server.BaseUrl}/forms?culture=en-US");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        var language = Page.GetByRole(
            AriaRole.Radiogroup,
            new() { Name = "Catalog language", Exact = true });
        var english = language.GetByRole(AriaRole.Radio, new() { Name = "English", Exact = true });
        var chinese = language.GetByRole(AriaRole.Radio, new() { Name = "中文", Exact = true });
        await Expect(english).ToBeCheckedAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        var panel = Page.GetByRole(AriaRole.Dialog, new() { Name = "Choose a date" });
        await Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Month" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        await language.GetByText("中文", new() { Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/forms?culture=zh-Hans");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        language = Page.GetByRole(
            AriaRole.Radiogroup,
            new() { Name = "目录语言", Exact = true });
        chinese = language.GetByRole(AriaRole.Radio, new() { Name = "中文", Exact = true });
        await Expect(chinese).ToBeCheckedAsync();

        input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        panel = Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" });
        await Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "月份" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        var catalogNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Bzs.Blazor 目录", Exact = true });
        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "反馈", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/feedback?culture=zh-Hans");
        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "表单", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/forms?culture=zh-Hans");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "概览", Exact = true })
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

        Assert.Empty(consoleErrors);
        Assert.Empty(pageErrors);
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

        var response = await Page.GotoAsync($"{server.BaseUrl}/render-modes/webassembly?culture=en-US");

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
        AssertNoUnexpectedBrowserErrors("standalone WebAssembly render-mode workflow");
    }

    [Fact]
    public async Task ProductivityWorkflowRunsInStandaloneWebAssembly()
    {
        BeginBrowserGateTest();
        var response = await Page.GotoAsync($"{server.BaseUrl}/productivity");

        Assert.True(response?.Ok ?? false);
        await Expect(Page.GetByTestId("productivity-workbench"))
            .ToHaveAttributeAsync("data-bzs-interactive", "true");
        var grid = Page.GetByRole(AriaRole.Table, new() { Name = "Review queue" });
        await Expect(grid.Locator("tbody tr")).ToHaveCountAsync(5);

        var workbenchNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Workbench navigation", Exact = true });
        var reviewQueueDisclosure = workbenchNavigation.Locator("details");
        await Expect(reviewQueueDisclosure).ToHaveAttributeAsync("data-bzs-open", "true");
        await reviewQueueDisclosure.Locator("summary").ClickAsync();
        await Expect(reviewQueueDisclosure).ToHaveAttributeAsync("data-bzs-open", "false");
        await reviewQueueDisclosure.Locator("summary").ClickAsync();
        await Expect(reviewQueueDisclosure).ToHaveAttributeAsync("data-bzs-open", "true");

        var removeReviewFilter = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Remove review filter", Exact = true });
        await removeReviewFilter.ClickAsync();
        await Expect(removeReviewFilter).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Needs review", new() { Exact = true })).ToHaveCountAsync(0);

        var tooltipTrigger = Page.GetByTestId("productivity-tooltip-trigger");
        await tooltipTrigger.FocusAsync();
        await Expect(Page.GetByRole(AriaRole.Tooltip))
            .ToContainTextAsync("keyboard and pointer");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Open review details" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Review details" }))
            .ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");

        var owner = Page.GetByTestId("productivity-owner");
        await owner.FillAsync("Mei");
        await Page.GetByRole(AriaRole.Option, new() { Name = "Mei Lin" }).ClickAsync();
        await Expect(owner).ToHaveValueAsync("Mei Lin");

        workbenchNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Workbench navigation", Exact = true });
        await workbenchNavigation.GetByRole(
            AriaRole.Link,
            new() { Name = "Productivity", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/productivity");

        workbenchNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Workbench navigation", Exact = true });
        await workbenchNavigation.GetByRole(
            AriaRole.Link,
            new() { Name = "Assigned", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/productivity?view=assigned");

        workbenchNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Workbench navigation", Exact = true });
        await workbenchNavigation.GetByRole(
            AriaRole.Link,
            new() { Name = "Waiting", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/productivity?view=waiting");

        workbenchNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Workbench navigation", Exact = true });
        await workbenchNavigation.GetByRole(
            AriaRole.Link,
            new() { Name = "Overview", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/");

        await Page.GotoAsync($"{server.BaseUrl}/productivity");
        var breadcrumbs = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Productivity breadcrumb", Exact = true });
        await breadcrumbs.GetByRole(
            AriaRole.Link,
            new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/");
        AssertNoUnexpectedBrowserErrors("standalone WebAssembly productivity workflow");
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
