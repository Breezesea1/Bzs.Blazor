using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class BrowserMatrixTests(DemoServerFixture server)
{
    [Fact]
    public async Task RequestedBrowserTargetRunsInteractiveAutoCatalogWorkflow()
    {
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(30_000);
        var target = Environment.GetEnvironmentVariable("BZS_BROWSER_MATRIX_TARGET")?.Trim().ToLowerInvariant()
            ?? "chromium";
        var artifactDirectory = GetArtifactDirectory(target);
        if (Directory.Exists(artifactDirectory))
        {
            Directory.Delete(artifactDirectory, recursive: true);
        }

        Directory.CreateDirectory(artifactDirectory);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await LaunchBrowserAsync(playwright, target);
        await using var context = await CreateContextAsync(browser, playwright, target);
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });

        var page = await context.NewPageAsync();
        IPage? productivityPage = null;
        IPage? localizationPage = null;
        var consoleMessages = new List<string>();
        var consoleErrors = new List<string>();
        var requests = new List<string>();
        var failedRequests = new List<string>();
        var unexpectedFailedRequests = new List<string>();
        var responses = new List<string>();
        var badResponses = new List<string>();

        void ObservePage(IPage observedPage)
        {
            observedPage.Console += (_, message) =>
            {
                consoleMessages.Add($"{message.Type}: {message.Text}");
                if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
                {
                    consoleErrors.Add(message.Text);
                }
            };
            observedPage.Request += (_, request) => requests.Add($"{request.Method} {request.Url}");
            observedPage.RequestFailed += (_, request) =>
            {
                var message = $"{request.Method} {request.Url}: {request.Failure}";
                failedRequests.Add(message);
                if (!string.Equals(request.Failure, "net::ERR_ABORTED", StringComparison.OrdinalIgnoreCase))
                {
                    unexpectedFailedRequests.Add(message);
                }
            };
            observedPage.Response += (_, response) =>
            {
                responses.Add($"{response.Status} {response.Request.Method} {response.Url}");
                if (response.Status >= 400)
                {
                    badResponses.Add($"{response.Status} {response.Url}");
                }
            };
        }

        ObservePage(page);

        try
        {
            await RunInteractiveAutoWorkflowAsync(page, server.BaseUrl, target);
            productivityPage = await context.NewPageAsync();
            ObservePage(productivityPage);
            await RunProductivityWorkflowAsync(productivityPage, server.BaseUrl, target);
            localizationPage = await context.NewPageAsync();
            ObservePage(localizationPage);
            await RunLocalizationAndRtlWorkflowAsync(localizationPage, server.BaseUrl, target);
            Assert.True(
                badResponses.Count == 0,
                $"{target} returned HTTP errors:{Environment.NewLine}{string.Join(Environment.NewLine, badResponses)}");
            Assert.True(
                unexpectedFailedRequests.Count == 0,
                $"{target} had failed requests:{Environment.NewLine}{string.Join(Environment.NewLine, unexpectedFailedRequests)}");
            Assert.True(
                consoleErrors.Count == 0,
                $"{target} wrote console errors:{Environment.NewLine}{string.Join(Environment.NewLine, consoleErrors)}");
        }
        finally
        {
            await TryCaptureArtifactAsync(async () =>
            {
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(artifactDirectory, "workflow.png"),
                    FullPage = true,
                });
            });
            if (localizationPage is not null)
            {
                await TryCaptureArtifactAsync(async () =>
                {
                    await localizationPage.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = Path.Combine(artifactDirectory, "rtl.png"),
                        FullPage = true,
                    });
                });
            }
            if (productivityPage is not null)
            {
                await TryCaptureArtifactAsync(async () =>
                {
                    await productivityPage.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = Path.Combine(artifactDirectory, "productivity.png"),
                        FullPage = true,
                    });
                });
            }

            await TryCaptureArtifactAsync(() =>
                File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "console.log"), consoleMessages));
            await TryCaptureArtifactAsync(() =>
                File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "requests.log"), requests));
            await TryCaptureArtifactAsync(() =>
                File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "request-failures.log"), failedRequests));
            await TryCaptureArtifactAsync(() =>
                File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "responses.log"), responses));
            await TryCaptureArtifactAsync(async () =>
            {
                await context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = Path.Combine(artifactDirectory, "trace.zip"),
                });
            });
        }
    }

    private static async Task RunInteractiveAutoWorkflowAsync(IPage page, string baseUrl, string target)
    {
        const string testId = "render-mode-auto";
        var response = await page.GotoAsync($"{baseUrl}/render-modes/auto");
        Assert.True(response?.Ok ?? false, $"{target} could not load the Interactive Auto catalog.");

        await Expect(page.GetByTestId($"{testId}-runtime-readiness"))
            .ToHaveTextAsync("Interactive runtime ready");
        var counter = page.GetByTestId($"{testId}-counter");
        await counter.ClickAsync();
        await Expect(counter).ToHaveTextAsync("Interaction count: 1");

        var theme = page.GetByTestId($"{testId}-theme");
        var darkTheme = theme.GetByRole(AriaRole.Button, new() { Name = "Dark" });
        await darkTheme.ClickAsync();
        await Expect(theme).ToHaveAttributeAsync("data-bzs-theme", "dark");

        var workItem = page.GetByTestId($"{testId}-work-item");
        await workItem.FillAsync("Lighting review");
        await workItem.PressAsync("Tab");
        await page.GetByTestId($"{testId}-save").ClickAsync();
        await Expect(page.GetByTestId($"{testId}-form-status")).ToHaveTextAsync("Saved Lighting review.");

        var tabs = page.GetByTestId($"{testId}-tabs");
        var details = tabs.GetByRole(AriaRole.Tab, new() { Name = "Details" });
        await details.ClickAsync();
        await Expect(details).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByTestId($"{testId}-tabs-status")).ToHaveTextAsync("Active tab: details");

        var dialogTrigger = page.GetByTestId($"{testId}-open-controlled-dialog");
        await dialogTrigger.FocusAsync();
        await dialogTrigger.PressAsync("Enter");
        var controlledDialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled catalog dialog" });
        var completeDialog = controlledDialog.GetByTestId($"{testId}-complete-controlled-dialog");
        await Expect(controlledDialog).ToBeVisibleAsync();
        await Expect(completeDialog).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(controlledDialog).ToHaveCountAsync(0);
        await Expect(dialogTrigger).ToBeFocusedAsync();

        await page.GetByTestId($"{testId}-open-drawer").ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Catalog drawer" });
        await Expect(drawer).ToBeVisibleAsync();
        await drawer.GetByTestId($"{testId}-close-drawer").ClickAsync();
        await Expect(drawer).ToHaveCountAsync(0);
        await Expect(page.GetByTestId($"{testId}-drawer-status")).ToHaveTextAsync("Drawer is closed.");

        await page.GetByTestId($"{testId}-open-service-dialog").ClickAsync();
        var serviceDialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Catalog service dialog" });
        await Expect(serviceDialog).ToBeVisibleAsync();
        await serviceDialog.GetByTestId("service-dialog-complete").ClickAsync();
        await Expect(serviceDialog).ToHaveCountAsync(0);
        await Expect(page.GetByTestId($"{testId}-service-dialog-status")).ToHaveTextAsync("Completed: true");

        await page.GetByTestId($"{testId}-show-toast").ClickAsync();
        await Expect(page.GetByTestId($"{testId}-overlay-host")).ToContainTextAsync("Catalog toast");

        var hasHorizontalOverflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasHorizontalOverflow, $"{target} Interactive Auto catalog has horizontal page overflow.");
    }

    private static async Task RunLocalizationAndRtlWorkflowAsync(IPage page, string baseUrl, string target)
    {
        var response = await page.GotoAsync($"{baseUrl}/tabs");
        Assert.True(response?.Ok ?? false, $"{target} could not load the Interactive Auto tabs workbench.");
        await Expect(page.GetByTestId("tabs-runtime-status")).ToHaveTextAsync("Interactive runtime ready");

        var rtlTabs = page.GetByTestId("rtl-tabs");
        var chineseTabs = page.GetByTestId("chinese-tabs");
        await Expect(chineseTabs).ToHaveAttributeAsync("lang", "zh-Hans");
        await Expect(chineseTabs.GetByRole(AriaRole.Tab, new() { Name = "概览" })).ToBeVisibleAsync();
        await Expect(chineseTabs.GetByRole(AriaRole.Tabpanel))
            .ToContainTextAsync("项目概览包含当前进度、负责人和下一次评审日期。");

        var summary = rtlTabs.GetByRole(AriaRole.Tab, new() { Name = "ملخص" });
        var decisions = rtlTabs.GetByRole(AriaRole.Tab, new() { Name = "قرارات" });
        await Expect(summary).ToBeVisibleAsync();
        await summary.FocusAsync();
        await summary.PressAsync("ArrowRight");
        await Expect(decisions).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByTestId("rtl-selection")).ToHaveTextAsync("المحدد: decisions");
    }

    private static async Task RunProductivityWorkflowAsync(IPage page, string baseUrl, string target)
    {
        var response = await page.GotoAsync($"{baseUrl}/productivity");
        Assert.True(response?.Ok ?? false, $"{target} could not load the Productivity catalog.");
        await Expect(page.GetByTestId("productivity-workbench"))
            .ToHaveAttributeAsync("data-bzs-interactive", "true");

        var grid = page.GetByRole(AriaRole.Table, new() { Name = "Review queue" });
        await Expect(grid.Locator("tbody tr")).ToHaveCountAsync(5);

        var tooltipTrigger = page.GetByTestId("productivity-tooltip-trigger");
        if (target is "mobile-chrome" or "mobile-safari")
        {
            await tooltipTrigger.TapAsync();
            await Expect(page.GetByRole(AriaRole.Tooltip)).ToBeVisibleAsync();
            await tooltipTrigger.TapAsync();
            await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveCountAsync(0);
        }
        else
        {
            await tooltipTrigger.FocusAsync();
            await Expect(page.GetByRole(AriaRole.Tooltip)).ToBeVisibleAsync();
            await page.Keyboard.PressAsync("Escape");
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Open review details" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Review details" }))
            .ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");

        var menuTrigger = page.GetByRole(AriaRole.Button, new() { Name = "Open review actions" });
        await menuTrigger.FocusAsync();
        await menuTrigger.PressAsync("Enter");
        var markReady = page.GetByRole(AriaRole.Menuitem, new() { Name = "Mark ready" });
        await Expect(markReady).ToBeFocusedAsync();
        await markReady.PressAsync("Enter");
        await Expect(page.GetByText("Review marked ready.", new() { Exact = true })).ToBeVisibleAsync();

        var owner = page.GetByTestId("productivity-owner");
        await owner.FillAsync("Alicia");
        await Expect(page.GetByRole(AriaRole.Option, new() { Name = "Alicia Santos" }))
            .ToBeVisibleAsync();
        await owner.PressAsync("ArrowDown");
        await owner.PressAsync("Enter");
        await Expect(owner).ToHaveValueAsync("Alicia Santos");

        var upload = page.GetByTestId("productivity-upload");
        await upload.FocusAsync();
        await Expect(upload).ToBeFocusedAsync();
        await upload.SetInputFilesAsync(new FilePayload
        {
            Name = "matrix-review.pdf",
            MimeType = "application/pdf",
            Buffer = "Browser matrix upload"u8.ToArray(),
        });
        await Expect(page.GetByText("matrix-review.pdf", new() { Exact = true })).ToBeVisibleAsync();

        var reviewSort = grid.GetByRole(AriaRole.Button, new() { Name = "Review", Exact = true });
        await reviewSort.FocusAsync();
        await reviewSort.PressAsync("Enter");
        await Expect(grid.Locator("th[aria-sort]"))
            .ToHaveAttributeAsync("aria-sort", "ascending");

        var hasHorizontalOverflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasHorizontalOverflow, $"{target} Productivity catalog has horizontal page overflow.");
    }

    private static Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright, string target) =>
        target switch
        {
            "chromium" => playwright.Chromium.LaunchAsync(),
            "mobile-chrome" => playwright.Chromium.LaunchAsync(),
            "chrome" => playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Channel = "chrome" }),
            "msedge" => playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Channel = "msedge" }),
            "firefox" => playwright.Firefox.LaunchAsync(),
            "webkit" => playwright.Webkit.LaunchAsync(),
            "mobile-safari" => playwright.Webkit.LaunchAsync(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(target),
                target,
                "Use chromium, mobile-chrome, chrome, msedge, firefox, webkit, or mobile-safari."),
        };

    private static Task<IBrowserContext> CreateContextAsync(
        IBrowser browser,
        IPlaywright playwright,
        string target) =>
        target switch
        {
            "mobile-chrome" => browser.NewContextAsync(playwright.Devices["Pixel 5"]),
            "mobile-safari" => browser.NewContextAsync(playwright.Devices["iPhone 13"]),
            _ => browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            }),
        };

    private static string GetArtifactDirectory(string target)
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("BZS_BROWSER_ARTIFACTS");
        var root = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "artifacts", "browser-matrix")
            : configuredDirectory;

        return Path.Combine(root, target);
    }

    private static async Task TryCaptureArtifactAsync(Func<Task> capture)
    {
        try
        {
            await capture();
        }
        catch
        {
            // Preserve the workflow result when artifact capture is unavailable.
        }
    }
}
