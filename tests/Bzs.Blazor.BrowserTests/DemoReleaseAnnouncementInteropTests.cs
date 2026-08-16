using Bzs.Blazor.Demo.Client.Components;
using Microsoft.JSInterop;

namespace Bzs.Blazor.BrowserTests;

public sealed class DemoReleaseAnnouncementInteropTests
{
    [Fact]
    public void LatestUnreadStateIgnoresAcknowledgedHistoricalReleases()
    {
        IReadOnlySet<string> acknowledgedHistoricalIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "v0.1.0",
        };
        IReadOnlySet<string> acknowledgedLatestAndHistoricalIds = new HashSet<string>(
            ["v0.1.0", "v0.2.0", "v0.2.1", "v0.2.3"],
            StringComparer.Ordinal);

        Assert.Equal(
            1,
            DemoReleaseAnnouncement.GetLatestUnreadCount(
                "v0.2.3",
                acknowledgedHistoricalIds));
        Assert.Equal(
            0,
            DemoReleaseAnnouncement.GetLatestUnreadCount(
                "v0.2.3",
                acknowledgedLatestAndHistoricalIds));
    }

    [Fact]
    public async Task DisposeDoesNotRethrowFailedJavaScriptImport()
    {
        await AssertFailedImportIsSafeToDisposeAsync(
            new JSException("The announcement module could not be loaded."));
    }

    [Fact]
    public async Task DisposeDoesNotRethrowUnavailableJavaScriptRuntime()
    {
        await AssertFailedImportIsSafeToDisposeAsync(
            new InvalidOperationException("JavaScript interop is unavailable."));
    }

    private static async Task AssertFailedImportIsSafeToDisposeAsync(Exception importFailure)
    {
        var runtime = new FailingImportJsRuntime(importFailure);
        var interop = new DemoReleaseAnnouncementInterop(runtime);

        var failure = await Record.ExceptionAsync(
            () => interop.ReadAcknowledgedIdsAsync("test-key").AsTask());

        Assert.Same(importFailure, failure);
        await interop.DisposeAsync();
        Assert.Equal(1, runtime.ImportAttempts);
    }

    private sealed class FailingImportJsRuntime(Exception importFailure) : IJSRuntime
    {
        internal int ImportAttempts { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Assert.Equal("import", identifier);
            ImportAttempts++;
            return ValueTask.FromException<TValue>(importFailure);
        }
    }
}
