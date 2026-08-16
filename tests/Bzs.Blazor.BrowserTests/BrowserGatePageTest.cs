using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

public abstract class BrowserGatePageTest : PageTest
{
    private readonly ConcurrentQueue<string> _consoleMessages = new();
    private readonly ConcurrentQueue<string> _consoleErrors = new();
    private readonly List<IBrowserContext> _observedContexts = [];
    private readonly List<IPage> _observedPages = [];
    private readonly ConcurrentQueue<string> _pageErrors = new();
    private readonly ConcurrentQueue<string> _requestFailures = new();
    private readonly ConcurrentQueue<(string Method, string Url, string? Failure)> _failedRequests = new();
    private readonly ConcurrentQueue<string> _requests = new();
    private readonly ConcurrentQueue<string> _responses = new();
    private IBrowserContext? _artifactContext;
    private IPage? _artifactPage;
    private string _artifactTestName = "unattributed";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(30_000);
        await ObserveContextAsync(Context);
        _artifactContext = Context;
        _artifactPage = Page;
    }

    public override async Task DisposeAsync()
    {
        string? artifactDirectory = null;

        try
        {
            if (!TestOk)
            {
                artifactDirectory = GetArtifactDirectory();
                if (Directory.Exists(artifactDirectory))
                {
                    Directory.Delete(artifactDirectory, recursive: true);
                }

                Directory.CreateDirectory(artifactDirectory);
                await WriteFailureLogsAsync(artifactDirectory);
                await CaptureFailureScreenshotAsync(artifactDirectory);
            }
        }
        catch
        {
            // Keep the original test failure as the reported failure when artifact capture also fails.
        }
        finally
        {
            await StopTracesAsync(artifactDirectory);
            await base.DisposeAsync();
        }
    }

    protected void BeginBrowserGateTest(
        string? caseSuffix = null,
        [CallerMemberName] string testName = "")
    {
        var name = string.IsNullOrWhiteSpace(caseSuffix)
            ? testName
            : $"{testName}-{caseSuffix}";
        _artifactTestName = SanitizePathSegment($"{GetType().Name}.{name}");
    }

    protected async Task<IPage> NewObservedPageAsync(BrowserNewContextOptions options)
    {
        var context = await NewContext(options);
        await ObserveContextAsync(context);
        var page = await context.NewPageAsync();

        ObservePage(page);
        _artifactContext = context;
        _artifactPage = page;
        return page;
    }

    protected async Task AssertBrandBlockShowsLogoAndFaviconResolvesToServedAssetAsync()
    {
        var navigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Bzs.Blazor 目录", Exact = true });
        await Expect(navigation).ToBeVisibleAsync();

        var brandLink = navigation.GetByRole(
            AriaRole.Link,
            new() { Name = "Bzs.Blazor", Exact = false });
        await Expect(brandLink).ToBeVisibleAsync();
        var logo = brandLink.Locator("img");
        var logoHref = await logo.GetAttributeAsync("src");
        Assert.False(string.IsNullOrWhiteSpace(logoHref), "The brand logo source is missing.");
        Assert.False(
            logoHref.StartsWith("data:", StringComparison.Ordinal),
            "The brand logo still uses an inline data URL.");
        await logo.EvaluateAsync(
            """
            image => image.complete && image.naturalWidth > 0
                ? Promise.resolve()
                : new Promise((resolve, reject) => {
                    image.addEventListener('load', resolve, { once: true });
                    image.addEventListener('error', () => reject(new Error('The brand logo failed to load.')), { once: true });
                })
            """);

        var icon = Page.Locator("head link[rel='icon']");
        var iconHref = await icon.GetAttributeAsync("href");
        Assert.False(string.IsNullOrWhiteSpace(iconHref), "The favicon link is missing.");
        Assert.False(
            iconHref.StartsWith("data:", StringComparison.Ordinal),
            "The favicon link still uses the empty data: placeholder.");
        var resolvedIconHref = await icon.EvaluateAsync<string>("element => element.href");
        var iconResponse = await Page.Context.APIRequest.GetAsync(resolvedIconHref);
        Assert.True(iconResponse.Ok, $"The favicon '{resolvedIconHref}' was not served successfully.");
    }

    protected async Task AssertDemoChromeAsync(bool isChinese, bool includesServerRenderModes, string hostStatus)
    {
        var chrome = GetDemoChromeText(isChinese);

        await Expect(Page.Locator(".demo-skip-link")).ToHaveTextAsync(chrome.SkipLink);
        var navigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = chrome.NavigationAccessibleName, Exact = true });
        await Expect(navigation).ToBeVisibleAsync();
        await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = $"Bzs.Blazor {chrome.BrandTagline}", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = chrome.CloseNavigation, Exact = true }))
            .ToBeVisibleAsync();

        foreach (var section in new[]
        {
            chrome.CatalogSection,
            chrome.ProjectSection,
            includesServerRenderModes ? chrome.RenderModesSection : chrome.RuntimeSection,
        })
        {
            await Expect(navigation.GetByText(section, new() { Exact = true })).ToBeVisibleAsync();
        }

        foreach (var link in new[]
        {
            chrome.Overview,
            chrome.ThemeFoundation,
            chrome.FoundationComponents,
            chrome.Forms,
            chrome.Productivity,
            chrome.Feedback,
            chrome.Tabs,
            chrome.Overlays,
            chrome.Layout,
            chrome.Releases,
            chrome.InteractiveWebAssembly,
        })
        {
            await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = link, Exact = true })).ToBeVisibleAsync();
        }

        if (includesServerRenderModes)
        {
            await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = chrome.StaticSsr, Exact = true }))
                .ToBeVisibleAsync();
            await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = chrome.InteractiveServer, Exact = true }))
                .ToBeVisibleAsync();
            await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = chrome.InteractiveAuto, Exact = true }))
                .ToBeVisibleAsync();
        }

        await Expect(navigation.GetByText(chrome.DemoUserAvatarInitial, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(navigation.GetByText(chrome.DemoUser, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(navigation.GetByText(chrome.Administrator, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = chrome.SignOutAccessibleName, Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByText(chrome.ComponentWorkbench, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText(hostStatus, new() { Exact = true })).ToBeVisibleAsync();

        var language = Page.GetByRole(
            AriaRole.Radiogroup,
            new() { Name = chrome.LanguageSwitcherAccessibleName, Exact = true });
        await Expect(language).ToBeVisibleAsync();
        await Expect(language.GetByRole(AriaRole.Radio, new() { Name = "English", Exact = true })).ToBeVisibleAsync();
        await Expect(language.GetByRole(AriaRole.Radio, new() { Name = "中文", Exact = true })).ToBeVisibleAsync();
    }

    protected async Task AssertGlobalThemeSwitchPersistsAndFollowsSystemPreferenceAsync(
        string baseUrl,
        string query,
        string accessibleName,
        string lightLabel,
        string darkLabel,
        string systemLabel)
    {
        await Page.AddInitScriptAsync(
            """
            if (!sessionStorage.getItem('bzs-demo-theme-mode-test-initialized')) {
                localStorage.removeItem('bzs-demo-theme-mode');
                sessionStorage.setItem('bzs-demo-theme-mode-test-initialized', 'true');
            }
            """);
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });

        await Page.GotoAsync($"{baseUrl}{query}");

        var provider = Page.GetByTestId("demo-global-theme-provider");
        await Expect(provider).ToHaveAttributeAsync("data-bzs-demo-theme-mode", "light");
        var themeSwitch = Page.GetByRole(AriaRole.Group, new() { Name = accessibleName, Exact = true });
        await Expect(themeSwitch).ToBeVisibleAsync();
        await Expect(themeSwitch.GetByRole(AriaRole.Button, new() { Name = lightLabel, Exact = true }))
            .ToBeVisibleAsync();
        var dark = themeSwitch.GetByRole(AriaRole.Button, new() { Name = darkLabel, Exact = true });
        await Expect(dark).ToBeVisibleAsync();
        var system = themeSwitch.GetByRole(AriaRole.Button, new() { Name = systemLabel, Exact = true });
        await Expect(system).ToBeVisibleAsync();

        await dark.ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        await Page.ReloadAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        themeSwitch = Page.GetByRole(AriaRole.Group, new() { Name = accessibleName, Exact = true });
        await themeSwitch.GetByRole(AriaRole.Button, new() { Name = systemLabel, Exact = true }).ClickAsync();
        await Expect(Page.GetByTestId("demo-global-theme-provider"))
            .ToHaveAttributeAsync("data-bzs-theme", "light");

        await Page.GotoAsync($"{baseUrl}/forms{query}");
        await Expect(Page.GetByTestId("demo-global-theme-provider"))
            .ToHaveAttributeAsync("data-bzs-theme", "light");

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Expect(Page.GetByTestId("demo-global-theme-provider"))
            .ToHaveAttributeAsync("data-bzs-theme", "dark");
    }

    protected async Task AssertLandingPageSectionsAsync(string baseUrl, string query)
    {
        await Page.GotoAsync($"{baseUrl}{query}");

        var expectedSections = new[]
        {
            "landing-hero",
            "landing-demo-strip",
            "landing-install",
            "landing-features",
            "landing-component-groups",
            "landing-release",
            "landing-routes",
            "landing-footer",
        };

        foreach (var section in expectedSections)
        {
            await Expect(Page.GetByTestId(section)).ToBeVisibleAsync();
        }

        var order = await Page.GetByTestId("landing-page").EvaluateAsync<string[]>(
            "root => [...root.querySelectorAll(':scope > [data-testid]')].map(element => element.getAttribute('data-testid'))");
        Assert.Equal(expectedSections, order);
    }

    protected async Task AssertLandingPageCopyFollowsCultureAsync(string baseUrl)
    {
        await Page.GotoAsync(baseUrl);
        await Expect(Page.GetByTestId("landing-hero").Locator("h1"))
            .ToHaveTextAsync("为 Blazor 而生的紧凑组件库");
        await Expect(Page.GetByTestId("landing-install").GetByRole(
            AriaRole.Heading, new() { Name = "安装", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-features").GetByRole(
            AriaRole.Heading, new() { Name = "为什么是 Bzs.Blazor", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-release").GetByRole(
            AriaRole.Heading, new() { Name = "最新版本", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-footer")).ToContainTextAsync("基于 MIT 许可证发布。");

        await Page.GotoAsync($"{baseUrl}?culture=en-US");
        await Expect(Page.GetByTestId("landing-hero").Locator("h1"))
            .ToHaveTextAsync("A compact component library for Blazor");
        await Expect(Page.GetByTestId("landing-install").GetByRole(
            AriaRole.Heading, new() { Name = "Installation", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-features").GetByRole(
            AriaRole.Heading, new() { Name = "Why Bzs.Blazor", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-release").GetByRole(
            AriaRole.Heading, new() { Name = "Latest release", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-footer")).ToContainTextAsync("Released under the MIT license.");
    }

    protected async Task AssertLandingHeroCtasReachTheirSectionsAsync(string baseUrl, string query)
    {
        await Page.GotoAsync($"{baseUrl}{query}");
        await Expect(Page.GetByTestId("landing-page")).ToHaveAttributeAsync("data-interactive", "true");

        await Page.GetByTestId("landing-cta-install").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("#landing-install$"));
        await Expect(Page.GetByTestId("landing-install")).ToBeInViewportAsync();

        await Page.GetByTestId("landing-cta-groups").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("#landing-component-groups$"));
        await Expect(Page.GetByTestId("landing-component-groups")).ToBeInViewportAsync();
    }

    protected async Task AssertLandingLiveStripAsync(string baseUrl, string query)
    {
        await Page.GotoAsync($"{baseUrl}{query}");
        await Expect(Page.GetByTestId("landing-page")).ToHaveAttributeAsync("data-interactive", "true");

        var strip = Page.GetByTestId("landing-demo-strip");

        var name = strip.Locator("#landing-demo-name");
        await name.FillAsync("Mei");
        await Expect(name).ToHaveValueAsync("Mei");

        var workspace = strip.Locator("#landing-demo-workspace");
        await workspace.ClickAsync();
        await strip.Locator("#landing-demo-workspace-option-1").ClickAsync();
        await workspace.ClickAsync();
        await Expect(strip.Locator("#landing-demo-workspace-option-1"))
            .ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(workspace).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(workspace).ToBeFocusedAsync();

        var notifications = strip.Locator("#landing-demo-notifications");
        var initiallyChecked = await notifications.IsCheckedAsync();
        await notifications.PressAsync("Space");
        await Expect(notifications).ToBeCheckedAsync(new() { Checked = !initiallyChecked });

        await strip.GetByTestId("landing-show-toast").ClickAsync();
        var toast = Page.GetByTestId("landing-overlay-host").GetByRole(AriaRole.Status);
        await Expect(toast).ToBeVisibleAsync();
        await toast.GetByRole(AriaRole.Button).ClickAsync();
        await Expect(toast).ToHaveCountAsync(0);

        await strip.GetByTestId("landing-open-dialog").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-dialog-close")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
    }

    protected async Task AssertLandingInstallSnippetAsync(string baseUrl, string query)
    {
        await Page.GotoAsync($"{baseUrl}{query}");

        var snippet = Page.GetByTestId("landing-install-snippet");
        await Expect(snippet).ToContainTextAsync("dotnet add package Bzs.Blazor");
        await Expect(snippet).ToContainTextAsync("--version 0.2.3");
        await Expect(snippet).ToContainTextAsync("AddBzsBlazor()");

        await Expect(Page.GetByTestId("landing-page")).ToHaveAttributeAsync("data-interactive", "true");
        await Page.GetByTestId("landing-copy").ClickAsync();
        await Expect(Page.GetByTestId("landing-copy-status")).ToHaveTextAsync(new Regex(".+"));
    }

    protected async Task AssertLandingReleaseSummaryAsync(string baseUrl, string query)
    {
        await Page.GotoAsync($"{baseUrl}{query}");

        var release = Page.GetByTestId("landing-release");
        await Expect(release.GetByTestId("landing-release-version"))
            .ToHaveTextAsync(new Regex(@"^\d+\.\d+\.\d+$"));

        await release.GetByTestId("landing-release-more").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($"/releases{Regex.Escape(query)}$"));
        await Expect(Page.GetByTestId("releases-page")).ToBeVisibleAsync();
    }

    protected async Task AssertLandingFooterAsync(string baseUrl, string query)
    {
        await Page.GotoAsync($"{baseUrl}{query}");

        var footer = Page.GetByTestId("landing-footer");
        await Expect(footer.Locator("a[href^='https://www.nuget.org/packages/']")).ToBeVisibleAsync();
        await Expect(footer.Locator("a[href='https://github.com/Breezesea1/Bzs.Blazor']")).ToBeVisibleAsync();
        await Expect(footer.Locator("a[href$='/LICENSE']")).ToBeVisibleAsync();
    }

    private static DemoChromeText GetDemoChromeText(bool isChinese) => isChinese
        ? new()
        {
            SkipLink = "跳至目录内容",
            NavigationAccessibleName = "Bzs.Blazor 目录",
            BrandTagline = "组件实验室",
            CloseNavigation = "关闭导航",
            CatalogSection = "目录",
            Overview = "概览",
            ThemeFoundation = "主题基础",
            FoundationComponents = "基础组件",
            Forms = "表单",
            Productivity = "生产力",
            Feedback = "反馈",
            Tabs = "选项卡",
            Overlays = "浮层",
            Layout = "布局",
            ProjectSection = "项目",
            Releases = "版本发布",
            RenderModesSection = "渲染模式",
            RuntimeSection = "运行时",
            StaticSsr = "静态 SSR",
            InteractiveServer = "交互式服务器",
            InteractiveWebAssembly = "交互式 WebAssembly",
            InteractiveAuto = "交互式自动",
            DemoUser = "演示用户",
            DemoUserAvatarInitial = "演",
            Administrator = "管理员",
            Exit = "退出",
            SignOutAccessibleName = "演示退出操作，返回概览",
            OpenNavigation = "打开导航",
            ComponentWorkbench = "组件工作台",
            LanguageSwitcherAccessibleName = "目录语言",
        }
        : new()
        {
            SkipLink = "Skip to catalog content",
            NavigationAccessibleName = "Bzs.Blazor catalog",
            BrandTagline = "Component lab",
            CloseNavigation = "Close navigation",
            CatalogSection = "Catalog",
            Overview = "Overview",
            ThemeFoundation = "Theme foundation",
            FoundationComponents = "Foundation components",
            Forms = "Forms",
            Productivity = "Productivity",
            Feedback = "Feedback",
            Tabs = "Tabs",
            Overlays = "Overlays",
            Layout = "Layout",
            ProjectSection = "Project",
            Releases = "Releases",
            RenderModesSection = "Render modes",
            RuntimeSection = "Runtime",
            StaticSsr = "Static SSR",
            InteractiveServer = "Interactive Server",
            InteractiveWebAssembly = "Interactive WebAssembly",
            InteractiveAuto = "Interactive Auto",
            DemoUser = "Demo User",
            DemoUserAvatarInitial = "D",
            Administrator = "Administrator",
            Exit = "Exit",
            SignOutAccessibleName = "Demo sign-out action, returns to overview",
            OpenNavigation = "Open navigation",
            ComponentWorkbench = "Component workbench",
            LanguageSwitcherAccessibleName = "Catalog language",
        };

    private sealed class DemoChromeText
    {
        public required string SkipLink { get; init; }

        public required string NavigationAccessibleName { get; init; }

        public required string BrandTagline { get; init; }

        public required string CloseNavigation { get; init; }

        public required string CatalogSection { get; init; }

        public required string Overview { get; init; }

        public required string ThemeFoundation { get; init; }

        public required string FoundationComponents { get; init; }

        public required string Forms { get; init; }

        public required string Productivity { get; init; }

        public required string Feedback { get; init; }

        public required string Tabs { get; init; }

        public required string Overlays { get; init; }

        public required string Layout { get; init; }

        public required string ProjectSection { get; init; }

        public required string Releases { get; init; }

        public required string RenderModesSection { get; init; }

        public required string RuntimeSection { get; init; }

        public required string StaticSsr { get; init; }

        public required string InteractiveServer { get; init; }

        public required string InteractiveWebAssembly { get; init; }

        public required string InteractiveAuto { get; init; }

        public required string DemoUser { get; init; }

        public required string DemoUserAvatarInitial { get; init; }

        public required string Administrator { get; init; }

        public required string Exit { get; init; }

        public required string SignOutAccessibleName { get; init; }

        public required string OpenNavigation { get; init; }

        public required string ComponentWorkbench { get; init; }

        public required string LanguageSwitcherAccessibleName { get; init; }
    }

    private async Task ObserveContextAsync(IBrowserContext context)
    {
        if (_observedContexts.Contains(context))
        {
            return;
        }

        _observedContexts.Add(context);
        context.Page += (_, page) => ObservePage(page);
        context.Request += (_, request) => _requests.Enqueue(
            $"{DateTimeOffset.UtcNow:O} {request.Method} {request.ResourceType} {request.Url}");
        context.RequestFailed += (_, request) =>
        {
            _requestFailures.Enqueue(
                $"{DateTimeOffset.UtcNow:O} {request.Method} {request.Url}: {request.Failure ?? "request failed without a reported reason"}");
            _failedRequests.Enqueue((request.Method, request.Url, request.Failure));
        };
        context.Response += (_, response) => _responses.Enqueue(
            $"{DateTimeOffset.UtcNow:O} {response.Status} {response.Request.Method} {response.Url}");

        foreach (var page in context.Pages)
        {
            ObservePage(page);
        }

        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    private void ObservePage(IPage page)
    {
        if (!_observedPages.AddIfMissing(page))
        {
            return;
        }

        page.Console += (_, message) =>
        {
            _consoleMessages.Enqueue($"{DateTimeOffset.UtcNow:O} [{message.Type}] {message.Text}");
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                _consoleErrors.Enqueue(message.Text);
            }
        };
        page.PageError += (_, error) => _pageErrors.Enqueue(error);
    }

    private async Task WriteFailureLogsAsync(string artifactDirectory)
    {
        await File.WriteAllLinesAsync(
            Path.Combine(artifactDirectory, "console.log"),
            _consoleMessages);
        await File.WriteAllLinesAsync(
            Path.Combine(artifactDirectory, "page-errors.log"),
            _pageErrors);
        await File.WriteAllLinesAsync(
            Path.Combine(artifactDirectory, "requests.log"),
            _requests);
        await File.WriteAllLinesAsync(
            Path.Combine(artifactDirectory, "responses.log"),
            _responses);
        await File.WriteAllLinesAsync(
            Path.Combine(artifactDirectory, "request-failures.log"),
            _requestFailures);
    }

    protected void AssertNoUnexpectedBrowserErrors(string? context = null)
    {
        var errors = _consoleErrors
            .Select(error => $"Console error: {error}")
            .Concat(_pageErrors.Select(error => $"Page error: {error}"))
            .Concat(_failedRequests
                .Where(request => !IsExpectedAbort(request.Failure))
                .Select(request =>
                    $"Request failed: {request.Method} {request.Url}: {request.Failure ?? "request failed without a reported reason"}"))
            .ToArray();

        var message = context is null
            ? "Unexpected browser errors were reported."
            : $"Unexpected browser errors were reported during {context}.";
        Assert.True(errors.Length == 0, $"{message}{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static bool IsExpectedAbort(string? failure)
    {
        if (string.IsNullOrWhiteSpace(failure))
        {
            return false;
        }

        return failure.Contains("net::ERR_ABORTED", StringComparison.OrdinalIgnoreCase)
            || failure.Contains("NS_BINDING_ABORTED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(failure.Trim(), "ERR_ABORTED", StringComparison.OrdinalIgnoreCase);
    }

    private async Task CaptureFailureScreenshotAsync(string artifactDirectory)
    {
        if (_artifactPage is null)
        {
            return;
        }

        try
        {
            await _artifactPage.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true,
                Path = Path.Combine(artifactDirectory, "screenshot.png"),
            });
        }
        catch
        {
            // A page can already be closed when a navigation or fixture setup fails.
        }
    }

    private async Task StopTracesAsync(string? artifactDirectory)
    {
        var additionalTraceIndex = 1;
        foreach (var context in _observedContexts)
        {
            try
            {
                if (artifactDirectory is null)
                {
                    await context.Tracing.StopAsync();
                    continue;
                }

                var traceName = ReferenceEquals(context, _artifactContext)
                    ? "trace.zip"
                    : $"trace-{additionalTraceIndex++}.zip";
                await context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = Path.Combine(artifactDirectory, traceName),
                });
            }
            catch
            {
                // Preserve other traces and the test's original failure if one context is unavailable.
            }
        }
    }

    private string GetArtifactDirectory() => Path.Combine(
        FindRepositoryRoot(),
        "TestResults",
        "browser-gates",
        _artifactTestName);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bzs.Blazor.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Bzs.Blazor repository root.");
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character));
    }
}

internal static class BrowserGateCollectionExtensions
{
    public static bool AddIfMissing<T>(this ICollection<T> collection, T value)
    {
        if (collection.Contains(value))
        {
            return false;
        }

        collection.Add(value);
        return true;
    }
}
