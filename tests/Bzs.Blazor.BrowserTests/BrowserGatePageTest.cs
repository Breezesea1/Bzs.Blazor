using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

public abstract class BrowserGatePageTest : PageTest
{
    private readonly ConcurrentQueue<string> _consoleMessages = new();
    private readonly List<IBrowserContext> _observedContexts = [];
    private readonly List<IPage> _observedPages = [];
    private readonly ConcurrentQueue<string> _requestFailures = new();
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
        context.RequestFailed += (_, request) => _requestFailures.Enqueue(
            $"{DateTimeOffset.UtcNow:O} {request.Method} {request.Url}: {request.Failure ?? "request failed without a reported reason"}");
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

        page.Console += (_, message) => _consoleMessages.Enqueue(
            $"{DateTimeOffset.UtcNow:O} [{message.Type}] {message.Text}");
    }

    private async Task WriteFailureLogsAsync(string artifactDirectory)
    {
        await File.WriteAllLinesAsync(
            Path.Combine(artifactDirectory, "console.log"),
            _consoleMessages);
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
