using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bzs.Blazor.Tests;

public sealed class InteropLifecycleTests
{
    [Fact]
    public async Task TabsObjectDisposedFailuresAreTransientAcrossLifecycle()
    {
        var attachRuntime = new LifecycleJsRuntime();
        attachRuntime.Module.OperationFailures[BzsTabsInterop.AttachMethod] =
            new ObjectDisposedException("attach-reference");
        await using (var interop = new BzsTabsInterop(attachRuntime))
        {
            await interop.AttachAsync(default, "horizontal", "automatic");
        }

        var directionRuntime = new LifecycleJsRuntime();
        directionRuntime.Module.OperationFailures[BzsTabsInterop.GetDirectionMethod] =
            new ObjectDisposedException("direction-reference");
        await using (var interop = new BzsTabsInterop(directionRuntime))
        {
            Assert.False(await interop.IsRightToLeftAsync(default));
        }

        var detachRuntime = new LifecycleJsRuntime();
        var detachInterop = new BzsTabsInterop(detachRuntime);
        await detachInterop.AttachAsync(default, "horizontal", "automatic");
        detachRuntime.Module.OperationFailures[BzsTabsInterop.DetachMethod] =
            new ObjectDisposedException("detach-reference");

        await detachInterop.DisposeAsync(default);

        Assert.Equal(1, detachRuntime.Module.DisposeAttempts);

        var moduleDisposeRuntime = new LifecycleJsRuntime();
        var moduleDisposeInterop = new BzsTabsInterop(moduleDisposeRuntime);
        await moduleDisposeInterop.AttachAsync(default, "horizontal", "automatic");
        moduleDisposeRuntime.Module.DisposeFailure =
            new ObjectDisposedException("module-reference");

        await ((IAsyncDisposable)moduleDisposeInterop).DisposeAsync();

        Assert.Equal(1, moduleDisposeRuntime.Module.DisposeAttempts);
    }

    [Fact]
    public async Task TabsInvalidOperationIsTransientOnlyDuringImport()
    {
        var importRuntime = new LifecycleJsRuntime
        {
            ImportFailure = new InvalidOperationException(
                "JavaScript interop is unavailable during prerendering."),
        };
        await using (var interop = new BzsTabsInterop(importRuntime))
        {
            await interop.AttachAsync(default, "horizontal", "automatic");
        }

        var operationFailure = new InvalidOperationException("Feature logic failed.");
        var invocationRuntime = new LifecycleJsRuntime();
        invocationRuntime.Module.OperationFailures[BzsTabsInterop.AttachMethod] = operationFailure;
        await using var invocationInterop = new BzsTabsInterop(invocationRuntime);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => invocationInterop.AttachAsync(default, "horizontal", "automatic").AsTask());

        Assert.Same(operationFailure, thrown);
    }

    [Fact]
    public async Task OverlayDisposeDuringImportDisposesLateModuleWithoutActivating()
    {
        var runtime = new LifecycleJsRuntime { BlockImport = true };
        var interop = new BzsOverlayInterop(runtime);

        var activation = interop.ActivateAsync("overlay", default, true, null).AsTask();
        await runtime.WaitForImportAsync();

        await interop.DisposeAsync("overlay");
        runtime.ReleaseImport();

        await activation;
        Assert.Empty(runtime.Module.Invocations);
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    [Fact]
    public async Task NavigationDrawerActivationReportsTransientFailureThenSucceeds()
    {
        var runtime = new LifecycleJsRuntime();
        runtime.Module.FailNextOperation[BzsOverlayInterop.ActivateNavigationDrawerMethod] =
            new JSDisconnectedException("The circuit is reconnecting.");
        var interop = new BzsOverlayInterop(runtime);

        var firstActivation = await interop.ActivateNavigationDrawerAsync(
            "navigation",
            default,
            default,
            default,
            "#first",
            "temporary");
        var secondActivation = await interop.ActivateNavigationDrawerAsync(
            "navigation",
            default,
            default,
            default,
            "#first",
            "temporary");

        Assert.False(firstActivation);
        Assert.True(secondActivation);
        Assert.Equal(
            [BzsOverlayInterop.ActivateNavigationDrawerMethod, BzsOverlayInterop.ActivateNavigationDrawerMethod],
            runtime.Module.Invocations);

        await interop.DisposeAsync("navigation");
    }

    [Fact]
    public async Task ThemeDisposeDuringImportProducesComponentRecoverableTransientFailure()
    {
        var runtime = new LifecycleJsRuntime { BlockImport = true };
        var interop = new BzsThemeProviderInterop(runtime);
        using var dotNetReference = DotNetObjectReference.Create(new BzsThemeProvider());

        var setSystemMode = interop.SetSystemModeAsync(default, dotNetReference, true).AsTask();
        await runtime.WaitForImportAsync();

        await interop.DisposeAsync(default);
        runtime.ReleaseImport();

        var thrown = await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => setSystemMode);
        Assert.True(BzsJsModule.IsTransientFailure(thrown));
        Assert.Empty(runtime.Module.Invocations);
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    [Fact]
    public async Task DateInputCancellationDuringImportDoesNotInitializeAnInstance()
    {
        var runtime = new LifecycleJsRuntime { BlockImport = true };
        await using var interop = new BzsDateInputInterop(runtime);
        using var dotNetReference = DotNetObjectReference.Create(new object());
        using var cancellation = new CancellationTokenSource();

        var initialization = interop.InitializeAsync(
            "date-input",
            default,
            dotNetReference,
            cancellation.Token).AsTask();
        await runtime.WaitForImportAsync();

        cancellation.Cancel();

        Assert.False((await initialization.WaitAsync(TimeSpan.FromSeconds(5))).Initialized);
        Assert.Empty(runtime.Module.Invocations);

        await interop.DisposeAsync();
        runtime.ReleaseImport();
        await runtime.Module.WaitForDisposalAsync();
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    [Fact]
    public async Task DateInputNullInitializationResultRemainsUninitialized()
    {
        var runtime = new LifecycleJsRuntime();
        await using var interop = new BzsDateInputInterop(runtime);
        using var dotNetReference = DotNetObjectReference.Create(new object());

        var initialization = await interop.InitializeAsync(
            "date-input",
            default,
            dotNetReference);

        Assert.False(initialization.Initialized);
        Assert.Null(initialization.BrowserToday);
        Assert.Equal([BzsDateInputInterop.InitializeMethod], runtime.Module.Invocations);
    }

    [Fact]
    public async Task OverlayDisposePreservesDeactivateFailureAndStillDisposesModule()
    {
        var runtime = new LifecycleJsRuntime();
        var interop = new BzsOverlayInterop(runtime);
        await interop.ActivateAsync("overlay", default, true, null);
        var deactivateFailure = new InvalidOperationException("Deactivate failed.");
        runtime.Module.OperationFailures[BzsOverlayInterop.DeactivateMethod] = deactivateFailure;
        runtime.Module.DisposeFailure = new InvalidOperationException("Module disposal failed.");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => interop.DisposeAsync("overlay").AsTask());

        Assert.Same(deactivateFailure, thrown);
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    [Fact]
    public async Task TabsDisposePreservesDetachFailureAndStillDisposesModule()
    {
        var runtime = new LifecycleJsRuntime();
        var interop = new BzsTabsInterop(runtime);
        await interop.AttachAsync(default, "horizontal", "automatic");
        var detachFailure = new InvalidOperationException("Detach failed.");
        runtime.Module.OperationFailures[BzsTabsInterop.DetachMethod] = detachFailure;
        runtime.Module.DisposeFailure = new InvalidOperationException("Module disposal failed.");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => interop.DisposeAsync(default).AsTask());

        Assert.Same(detachFailure, thrown);
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    [Fact]
    public async Task ThemeDisposePreservesCleanupFailureAndStillDisposesModule()
    {
        var runtime = new LifecycleJsRuntime();
        runtime.Module.Results[BzsThemeProviderInterop.SetSystemModeMethod] = true;
        var interop = new BzsThemeProviderInterop(runtime);
        using var dotNetReference = DotNetObjectReference.Create(new BzsThemeProvider());
        await interop.SetSystemModeAsync(default, dotNetReference, true);
        var cleanupFailure = new InvalidOperationException("Theme cleanup failed.");
        runtime.Module.OperationFailures[BzsThemeProviderInterop.DisposeMethod] = cleanupFailure;
        runtime.Module.DisposeFailure = new InvalidOperationException("Module disposal failed.");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => interop.DisposeAsync(default).AsTask());

        Assert.Same(cleanupFailure, thrown);
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    private sealed class LifecycleJsRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<bool> _importStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _importRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _importAttempts;

        internal LifecycleJsModule Module { get; } = new();

        internal Exception? ImportFailure { get; set; }

        internal bool BlockImport { get; set; }

        internal int ImportAttempts => Volatile.Read(ref _importAttempts);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Assert.Equal("import", identifier);
            Interlocked.Increment(ref _importAttempts);
            _importStarted.TrySetResult(true);
            if (BlockImport)
            {
                return AwaitImportAsync<TValue>(_importRelease.Task);
            }

            if (ImportFailure is { } exception)
            {
                ImportFailure = null;
                return ValueTask.FromException<TValue>(exception);
            }

            return ValueTask.FromResult((TValue)(object)Module);
        }

        internal async Task WaitForImportAsync() =>
            await _importStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        internal void ReleaseImport() => _importRelease.TrySetResult(true);

        private async ValueTask<TValue> AwaitImportAsync<TValue>(Task release)
        {
            await release;
            return (TValue)(object)Module;
        }
    }

    private sealed class LifecycleJsModule : IJSObjectReference
    {
        private readonly TaskCompletionSource<bool> _disposalStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeAttempts;

        internal ConcurrentQueue<string> Invocations { get; } = [];

        internal ConcurrentDictionary<string, Exception> OperationFailures { get; } = [];

        internal ConcurrentDictionary<string, Exception> FailNextOperation { get; } = [];

        internal ConcurrentDictionary<string, object?> Results { get; } = [];

        internal Exception? DisposeFailure { get; set; }

        internal int DisposeAttempts => Volatile.Read(ref _disposeAttempts);

        internal async Task WaitForDisposalAsync() =>
            await _disposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Invocations.Enqueue(identifier);
            if (FailNextOperation.TryRemove(identifier, out var transientFailure))
            {
                return ValueTask.FromException<TValue>(transientFailure);
            }

            if (OperationFailures.TryGetValue(identifier, out var exception))
            {
                return ValueTask.FromException<TValue>(exception);
            }

            if (Results.TryGetValue(identifier, out var result))
            {
                return ValueTask.FromResult((TValue)result!);
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeAttempts);
            _disposalStarted.TrySetResult(true);
            return DisposeFailure is { } exception
                ? ValueTask.FromException(exception)
                : ValueTask.CompletedTask;
        }
    }
}
