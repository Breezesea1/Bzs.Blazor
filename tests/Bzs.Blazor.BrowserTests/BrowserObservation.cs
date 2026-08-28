using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace Bzs.Blazor.BrowserTests;

/// <summary>
/// Watches every browser context and page a test touches, so a failing test leaves console output,
/// network logs, screenshots, and traces behind, and so a test can fail on browser errors it did
/// not expect. Every browser test observes through this module regardless of how it acquired its
/// page, so no acquisition path silently loses its evidence.
/// </summary>
internal sealed class BrowserObservation
{
    private readonly ConcurrentQueue<string> _consoleMessages = new();
    private readonly ConcurrentQueue<string> _consoleErrors = new();
    private readonly ConcurrentQueue<string> _pageErrors = new();
    private readonly ConcurrentQueue<string> _requests = new();
    private readonly ConcurrentQueue<string> _responses = new();
    private readonly ConcurrentQueue<string> _requestFailures = new();
    private readonly ConcurrentQueue<(string Method, string Url, string? Failure)> _failedRequests = new();
    private readonly ConcurrentQueue<(int Status, string Url)> _badResponses = new();

    // Playwright raises context and page events on its own connection thread, so a page can be
    // observed while a failing test is already capturing artifacts. The gate guards both lists.
    private readonly object _gate = new();
    private readonly List<IBrowserContext> _contexts = [];
    private readonly List<ObservedPage> _pages = [];

    /// <summary>Gets or sets the context whose trace is written as <c>trace.zip</c>.</summary>
    internal IBrowserContext? PrimaryContext { get; set; }

    /// <summary>Gets or sets the page a failure screenshot is taken from first.</summary>
    internal IPage? PrimaryPage { get; set; }

    /// <summary>Starts watching a context, its pages, and its trace. Repeat calls are ignored.</summary>
    internal async Task ObserveAsync(IBrowserContext context)
    {
        lock (_gate)
        {
            if (_contexts.Contains(context))
            {
                return;
            }

            _contexts.Add(context);
        }

        context.Page += (_, page) => Observe(page);
        context.Request += (_, request) => _requests.Enqueue(
            $"{DateTimeOffset.UtcNow:O} {request.Method} {request.ResourceType} {request.Url}");
        context.RequestFailed += (_, request) =>
        {
            _requestFailures.Enqueue(
                $"{DateTimeOffset.UtcNow:O} {request.Method} {request.Url}: {request.Failure ?? "request failed without a reported reason"}");
            _failedRequests.Enqueue((request.Method, request.Url, request.Failure));
        };
        context.Response += (_, response) =>
        {
            _responses.Enqueue(
                $"{DateTimeOffset.UtcNow:O} {response.Status} {response.Request.Method} {response.Url}");
            if (response.Status >= 400)
            {
                _badResponses.Enqueue((response.Status, response.Url));
            }
        };

        foreach (var page in context.Pages)
        {
            Observe(page);
        }

        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    /// <summary>
    /// Starts watching a page's console and unhandled errors. Repeat calls are ignored. Supply a
    /// <paramref name="label"/> so a failure screenshot says which workflow it shows.
    /// </summary>
    internal void Observe(IPage page, string? label = null)
    {
        lock (_gate)
        {
            var existing = _pages.FindIndex(observed => observed.Page == page);
            if (existing >= 0)
            {
                if (label is not null && _pages[existing].Label is null)
                {
                    _pages[existing] = _pages[existing] with { Label = label };
                }

                return;
            }

            _pages.Add(new ObservedPage(page, label));
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

    /// <summary>Gets the browser errors observed so far, excluding aborts a navigation causes.</summary>
    internal IReadOnlyList<string> GetUnexpectedErrors() =>
    [
        .. _consoleErrors.Select(error => $"Console error: {error}"),
        .. _pageErrors.Select(error => $"Page error: {error}"),
        .. _failedRequests
            .Where(request => !IsExpectedAbort(request.Failure))
            .Select(request =>
                $"Request failed: {request.Method} {request.Url}: {request.Failure ?? "request failed without a reported reason"}"),
    ];

    /// <summary>
    /// Gets the error responses observed so far. Kept apart from <see cref="GetUnexpectedErrors"/>
    /// because a demo page may legitimately probe a missing asset, so only the tests that own a
    /// whole workflow assert on it.
    /// </summary>
    internal IReadOnlyList<string> GetErrorResponses() =>
        [.. _badResponses.Select(response => $"HTTP {response.Status}: {response.Url}")];

    /// <summary>
    /// Writes logs, screenshots, and traces to <paramref name="artifactDirectory"/>. Pass null to
    /// stop tracing without keeping anything, which is what a passing test wants. Capture failures
    /// are swallowed so they never replace the failure the test was already reporting.
    /// </summary>
    internal async Task CaptureAsync(string? artifactDirectory)
    {
        if (artifactDirectory is not null)
        {
            await TryCaptureAsync(() => WriteLogsAsync(artifactDirectory));
            await TryCaptureAsync(() => CaptureScreenshotsAsync(artifactDirectory));
        }

        await StopTracesAsync(artifactDirectory);
    }

    /// <summary>
    /// Prepares an empty artifact directory, replacing any run left by a previous attempt. Returns
    /// null when the directory cannot be prepared, so a locked leftover file cannot throw over the
    /// failure a test is already reporting.
    /// </summary>
    internal static string? TryPrepareArtifactDirectory(string testName)
    {
        var artifactDirectory = RepositoryLayout.GetBrowserGateArtifactDirectory(testName);
        try
        {
            if (Directory.Exists(artifactDirectory))
            {
                Directory.Delete(artifactDirectory, recursive: true);
            }

            Directory.CreateDirectory(artifactDirectory);
            return artifactDirectory;
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteLogsAsync(string artifactDirectory)
    {
        await File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "console.log"), _consoleMessages);
        await File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "page-errors.log"), _pageErrors);
        await File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "requests.log"), _requests);
        await File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "responses.log"), _responses);
        await File.WriteAllLinesAsync(Path.Combine(artifactDirectory, "request-failures.log"), _requestFailures);
    }

    private async Task CaptureScreenshotsAsync(string artifactDirectory)
    {
        ObservedPage[] pages;
        lock (_gate)
        {
            pages = PrimaryPage is null
                ? [.. _pages]
                : [
                    .. _pages.Where(observed => observed.Page == PrimaryPage),
                    .. _pages.Where(observed => observed.Page != PrimaryPage),
                ];
        }

        for (var index = 0; index < pages.Length; index++)
        {
            var page = pages[index];
            var name = page.Label is null
                ? index == 0 ? "screenshot.png" : $"screenshot-{index}.png"
                : $"screenshot-{page.Label}.png";
            await TryCaptureAsync(() => pages[index].Page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true,
                Path = Path.Combine(artifactDirectory, RepositoryLayout.SanitizePathSegment(name)),
            }));
        }
    }

    private async Task StopTracesAsync(string? artifactDirectory)
    {
        IBrowserContext[] contexts;
        lock (_gate)
        {
            contexts = [.. _contexts];
        }

        for (var index = 0; index < contexts.Length; index++)
        {
            var context = contexts[index];
            try
            {
                if (artifactDirectory is null)
                {
                    await context.Tracing.StopAsync();
                    continue;
                }

                // Without a primary context every trace is named positionally, so none overwrites
                // another.
                var traceName = PrimaryContext is null
                    ? index == 0 ? "trace.zip" : $"trace-{index}.zip"
                    : ReferenceEquals(context, PrimaryContext) ? "trace.zip" : $"trace-{index}.zip";
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

    private static async Task TryCaptureAsync(Func<Task> capture)
    {
        try
        {
            await capture();
        }
        catch
        {
            // Keep the original test failure as the reported failure when artifact capture also fails.
        }
    }

    private static bool IsExpectedAbort(string? failure)
    {
        if (string.IsNullOrWhiteSpace(failure))
        {
            return false;
        }

        // Chromium reports net::ERR_ABORTED and Firefox NS_BINDING_ABORTED for the same thing: a
        // request the browser cancelled because a navigation superseded it.
        return failure.Contains("net::ERR_ABORTED", StringComparison.OrdinalIgnoreCase)
            || failure.Contains("NS_BINDING_ABORTED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(failure.Trim(), "ERR_ABORTED", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct ObservedPage(IPage Page, string? Label);
}
