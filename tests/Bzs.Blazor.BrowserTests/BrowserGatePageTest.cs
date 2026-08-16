using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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
        var chrome = isChinese
            ? new DemoChromeText(
                "跳至目录内容",
                "Bzs.Blazor 目录",
                "组件实验室",
                "关闭导航",
                "目录",
                "概览",
                "主题基础",
                "基础组件",
                "表单",
                "生产力",
                "反馈",
                "选项卡",
                "浮层",
                "布局",
                "项目",
                "版本发布",
                "渲染模式",
                "运行时",
                "静态 SSR",
                "交互式服务器",
                "交互式 WebAssembly",
                "交互式自动",
                "演示用户",
                "管理员",
                "退出",
                "演示退出操作，返回概览",
                "打开导航",
                "组件工作台",
                "目录语言")
            : new DemoChromeText(
                "Skip to catalog content",
                "Bzs.Blazor catalog",
                "Component lab",
                "Close navigation",
                "Catalog",
                "Overview",
                "Theme foundation",
                "Foundation components",
                "Forms",
                "Productivity",
                "Feedback",
                "Tabs",
                "Overlays",
                "Layout",
                "Project",
                "Releases",
                "Render modes",
                "Runtime",
                "Static SSR",
                "Interactive Server",
                "Interactive WebAssembly",
                "Interactive Auto",
                "Demo User",
                "Administrator",
                "Exit",
                "Demo sign-out action, returns to overview",
                "Open navigation",
                "Component workbench",
                "Catalog language");

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

    private sealed record DemoChromeText(
        string SkipLink,
        string NavigationAccessibleName,
        string BrandTagline,
        string CloseNavigation,
        string CatalogSection,
        string Overview,
        string ThemeFoundation,
        string FoundationComponents,
        string Forms,
        string Productivity,
        string Feedback,
        string Tabs,
        string Overlays,
        string Layout,
        string ProjectSection,
        string Releases,
        string RenderModesSection,
        string RuntimeSection,
        string StaticSsr,
        string InteractiveServer,
        string InteractiveWebAssembly,
        string InteractiveAuto,
        string DemoUser,
        string Administrator,
        string Exit,
        string SignOutAccessibleName,
        string OpenNavigation,
        string ComponentWorkbench,
        string LanguageSwitcherAccessibleName);

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
