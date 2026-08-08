using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bzs.Blazor.Tests;

public sealed class JsModuleLifecycleTests
{
    private const string ModulePath = "./_content/Bzs.Blazor/test-module.js";

    [Fact]
    public async Task TransientImportFailureIsLoggedAndDoesNotPoisonRetry()
    {
        var runtime = new TestJsRuntime
        {
            ImportFailure = new TaskCanceledException("Module import was canceled."),
        };
        using var loggerFactory = new RecordingLoggerFactory();
        await using var module = new BzsJsModule(runtime, ModulePath, loggerFactory);

        Assert.False(await module.TryInvokeVoidAsync("initialize"));
        Assert.True(await module.TryInvokeVoidAsync("initialize"));

        Assert.Equal(2, runtime.ImportAttempts);
        Assert.Equal(["initialize"], runtime.Module.Invocations);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains(ModulePath, entry.Message, StringComparison.Ordinal);
        Assert.Contains("initialize", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TabsInvalidOperationIsTransientOnlyDuringImport()
    {
        var importRuntime = new TestJsRuntime
        {
            ImportFailure = new InvalidOperationException("JavaScript interop is unavailable during prerendering."),
        };
        var options = new BzsJsModuleOptions(TreatInvalidOperationDuringImportAsTransient: true);
        await using var importModule = new BzsJsModule(importRuntime, ModulePath, options: options);

        Assert.False(await importModule.TryInvokeVoidAsync("attach"));

        var invocationRuntime = new TestJsRuntime();
        invocationRuntime.Module.OperationFailure = new InvalidOperationException("Feature logic failed.");
        await using var invocationModule = new BzsJsModule(invocationRuntime, ModulePath, options: options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => invocationModule.TryInvokeVoidAsync("attach").AsTask());
    }

    [Fact]
    public async Task ObjectDisposedFailureRequiresFeatureOptIn()
    {
        var defaultRuntime = new TestJsRuntime();
        defaultRuntime.Module.OperationFailure = new ObjectDisposedException("feature-reference");
        await using var defaultModule = new BzsJsModule(defaultRuntime, ModulePath);

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => defaultModule.TryInvokeVoidAsync("initialize").AsTask());

        var optedInRuntime = new TestJsRuntime();
        optedInRuntime.Module.OperationFailure = new ObjectDisposedException("feature-reference");
        var options = new BzsJsModuleOptions(TreatObjectDisposedAsTransient: true);
        await using var optedInModule = new BzsJsModule(optedInRuntime, ModulePath, options: options);

        Assert.False(await optedInModule.TryInvokeVoidAsync("initialize"));
    }

    [Fact]
    public async Task DisposeIsIdempotentAndSafeAfterDisconnect()
    {
        var runtime = new TestJsRuntime();
        runtime.Module.DisposeFailure = new JSDisconnectedException("Circuit disconnected.");
        using var loggerFactory = new RecordingLoggerFactory();
        var module = new BzsJsModule(runtime, ModulePath, loggerFactory);
        Assert.True(await module.TryInvokeVoidAsync("initialize"));

        await module.DisposeAsync();
        await module.DisposeAsync();

        Assert.Equal(1, runtime.Module.DisposeAttempts);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains(ModulePath, entry.Message, StringComparison.Ordinal);
        Assert.Contains("dispose-module", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConcurrentInvocationsShareOneModuleImport()
    {
        var runtime = new TestJsRuntime { BlockImport = true };
        await using var module = new BzsJsModule(runtime, ModulePath);

        var first = module.TryInvokeVoidAsync("first").AsTask();
        var second = module.TryInvokeVoidAsync("second").AsTask();
        await runtime.WaitForImportAttemptAsync();
        Assert.Equal(1, runtime.ImportAttempts);

        runtime.ReleaseImport();
        Assert.True(await first);
        Assert.True(await second);

        Assert.Equal(1, runtime.ImportAttempts);
        Assert.Equal(2, runtime.Module.Invocations.Count);
        Assert.Contains("first", runtime.Module.Invocations);
        Assert.Contains("second", runtime.Module.Invocations);
    }

    [Fact]
    public async Task DisposeDuringImportDisposesLateModuleAndDoesNotInvokeFeature()
    {
        var runtime = new TestJsRuntime { BlockImport = true };
        await using var module = new BzsJsModule(runtime, ModulePath);

        var invocation = module.TryInvokeVoidAsync("initialize").AsTask();
        await runtime.WaitForImportAttemptAsync();
        Assert.Equal(1, runtime.ImportAttempts);

        await module.DisposeAsync();
        runtime.ReleaseImport();

        Assert.False(await invocation);
        Assert.Empty(runtime.Module.Invocations);
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    [Fact]
    public async Task CancellationDuringImportDoesNotInvokeFeature()
    {
        var runtime = new TestJsRuntime { BlockImport = true };
        await using var module = new BzsJsModule(runtime, ModulePath);
        using var cancellation = new CancellationTokenSource();

        var invocation = module.TryInvokeAsync<string>(
            "initialize",
            cancellation.Token).AsTask();
        await runtime.WaitForImportAttemptAsync();
        Assert.Equal(1, runtime.ImportAttempts);

        cancellation.Cancel();

        Assert.False((await invocation.WaitAsync(TimeSpan.FromSeconds(5))).Succeeded);
        Assert.Empty(runtime.Module.Invocations);
        Assert.Equal(0, runtime.Module.CancellableInvocationAttempts);

        await module.DisposeAsync();
        runtime.ReleaseImport();
        await runtime.Module.WaitForDisposalAsync();
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    [Fact]
    public async Task CanceledImportWaitObservesFailureAndAllowsRetry()
    {
        var runtime = new TestJsRuntime { BlockImport = true };
        var lateFailure = new InvalidOperationException("Late import failure.");
        using var loggerFactory = new RecordingLoggerFactory();
        await using var module = new BzsJsModule(runtime, ModulePath, loggerFactory);
        using var cancellation = new CancellationTokenSource();

        var firstInvocation = module.TryInvokeAsync<string>(
            "initialize",
            cancellation.Token).AsTask();
        await runtime.WaitForImportAttemptAsync();

        cancellation.Cancel();
        Assert.False((await firstInvocation).Succeeded);

        runtime.FailImport(lateFailure);
        var lateFailureEntry = await loggerFactory.WaitForEntryAsync(
            entry => ReferenceEquals(entry.Exception, lateFailure));

        Assert.Equal(LogLevel.Error, lateFailureEntry.Level);
        Assert.True((await module.TryInvokeAsync<string>("initialize")).Succeeded);
        Assert.Equal(2, runtime.ImportAttempts);
        Assert.Equal(["initialize"], runtime.Module.Invocations);
    }

    [Fact]
    public async Task NonTransientLateImportFailureAfterDisposalIsLoggedAsError()
    {
        var runtime = new TestJsRuntime { BlockImport = true };
        var lateFailure = new InvalidOperationException("Late disposed import failure.");
        using var loggerFactory = new RecordingLoggerFactory();
        var module = new BzsJsModule(runtime, ModulePath, loggerFactory);

        var invocation = module.TryInvokeVoidAsync("initialize").AsTask();
        await runtime.WaitForImportAttemptAsync();
        await module.DisposeAsync();

        runtime.FailImport(lateFailure);

        await Assert.ThrowsAsync<InvalidOperationException>(() => invocation);
        var entry = await loggerFactory.WaitForEntryAsync(
            candidate => ReferenceEquals(candidate.Exception, lateFailure));
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    [Fact]
    public async Task NonTransientLateFeatureFailureAfterCancellationIsLoggedAsError()
    {
        var runtime = new TestJsRuntime();
        runtime.Module.BlockOperation = true;
        var lateFailure = new InvalidOperationException("Late feature failure.");
        using var loggerFactory = new RecordingLoggerFactory();
        await using var module = new BzsJsModule(runtime, ModulePath, loggerFactory);
        using var cancellation = new CancellationTokenSource();

        var invocation = module.TryInvokeAsync<string>(
            "initialize",
            cancellation.Token).AsTask();
        await runtime.Module.WaitForInvocationAsync();

        cancellation.Cancel();
        Assert.False((await invocation).Succeeded);
        runtime.Module.FailOperation(lateFailure);

        var entry = await loggerFactory.WaitForEntryAsync(
            candidate => ReferenceEquals(candidate.Exception, lateFailure));
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    [Fact]
    public async Task TransientLateImportFailureAfterCancellationIsLoggedAsDebug()
    {
        var runtime = new TestJsRuntime { BlockImport = true };
        var lateFailure = new JSDisconnectedException("Circuit disconnected during import.");
        using var loggerFactory = new RecordingLoggerFactory();
        await using var module = new BzsJsModule(runtime, ModulePath, loggerFactory);
        using var cancellation = new CancellationTokenSource();

        var invocation = module.TryInvokeAsync<string>(
            "initialize",
            cancellation.Token).AsTask();
        await runtime.WaitForImportAttemptAsync();

        cancellation.Cancel();
        Assert.False((await invocation).Succeeded);
        runtime.FailImport(lateFailure);

        var entry = await loggerFactory.WaitForEntryAsync(
            candidate => ReferenceEquals(candidate.Exception, lateFailure));
        Assert.Equal(LogLevel.Debug, entry.Level);
    }

    [Fact]
    public async Task TransientLateFeatureFailureAfterCancellationIsLoggedAsDebug()
    {
        var runtime = new TestJsRuntime();
        runtime.Module.BlockOperation = true;
        var lateFailure = new TaskCanceledException("Feature invocation timed out.");
        using var loggerFactory = new RecordingLoggerFactory();
        await using var module = new BzsJsModule(runtime, ModulePath, loggerFactory);
        using var cancellation = new CancellationTokenSource();

        var invocation = module.TryInvokeAsync<string>(
            "initialize",
            cancellation.Token).AsTask();
        await runtime.Module.WaitForInvocationAsync();

        cancellation.Cancel();
        Assert.False((await invocation).Succeeded);
        runtime.Module.FailOperation(lateFailure);

        var entry = await loggerFactory.WaitForEntryAsync(
            candidate => ReferenceEquals(candidate.Exception, lateFailure));
        Assert.Equal(LogLevel.Debug, entry.Level);
    }

    [Fact]
    public async Task CancellableFeatureInvocationUsesTokenlessModuleOverload()
    {
        var runtime = new TestJsRuntime();
        await using var module = new BzsJsModule(runtime, ModulePath);
        using var cancellation = new CancellationTokenSource();

        var invocation = await module.TryInvokeAsync<string>(
            "initialize",
            cancellation.Token);

        Assert.True(invocation.Succeeded);
        Assert.Equal(["initialize"], runtime.Module.Invocations);
        Assert.Equal(0, runtime.Module.CancellableInvocationAttempts);
    }

    private sealed class TestJsRuntime : IJSRuntime
    {
        private readonly object _importGate = new();
        private readonly TaskCompletionSource<bool> _importStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource<IJSObjectReference>? _blockedImport;
        private int _importAttempts;

        internal TestJsModule Module { get; } = new();

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
                Task<IJSObjectReference> blockedImport;
                lock (_importGate)
                {
                    _blockedImport ??= new TaskCompletionSource<IJSObjectReference>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    blockedImport = _blockedImport.Task;
                }

                return AwaitImportAsync<TValue>(blockedImport);
            }

            if (ImportFailure is { } exception)
            {
                ImportFailure = null;
                return ValueTask.FromException<TValue>(exception);
            }

            return ValueTask.FromResult((TValue)(object)Module);
        }

        internal void ReleaseImport()
        {
            TaskCompletionSource<IJSObjectReference>? blockedImport;
            lock (_importGate)
            {
                BlockImport = false;
                blockedImport = _blockedImport;
            }

            blockedImport?.TrySetResult(Module);
        }

        internal void FailImport(Exception exception)
        {
            TaskCompletionSource<IJSObjectReference>? blockedImport;
            lock (_importGate)
            {
                BlockImport = false;
                blockedImport = _blockedImport;
            }

            Assert.NotNull(blockedImport);
            blockedImport.TrySetException(exception);
        }

        internal async Task WaitForImportAttemptAsync() =>
            await _importStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        private static async ValueTask<TValue> AwaitImportAsync<TValue>(
            Task<IJSObjectReference> import)
        {
            var module = await import;
            return (TValue)module;
        }
    }

    private sealed class TestJsModule : IJSObjectReference
    {
        private readonly TaskCompletionSource<bool> _disposalStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _invocationStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<object?> _operationCompletion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _cancellableInvocationAttempts;
        private int _disposeAttempts;

        internal ConcurrentQueue<string> Invocations { get; } = [];

        internal Exception? OperationFailure { get; set; }

        internal Exception? DisposeFailure { get; set; }

        internal bool BlockOperation { get; set; }

        internal int DisposeAttempts => Volatile.Read(ref _disposeAttempts);

        internal int CancellableInvocationAttempts =>
            Volatile.Read(ref _cancellableInvocationAttempts);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeCoreAsync<TValue>(identifier);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Interlocked.Increment(ref _cancellableInvocationAttempts);
            return InvokeCoreAsync<TValue>(identifier);
        }

        private ValueTask<TValue> InvokeCoreAsync<TValue>(string identifier)
        {
            _invocationStarted.TrySetResult(true);
            if (BlockOperation)
            {
                return AwaitOperationAsync<TValue>();
            }

            if (OperationFailure is { } exception)
            {
                return ValueTask.FromException<TValue>(exception);
            }

            Invocations.Enqueue(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        internal void FailOperation(Exception exception) =>
            _operationCompletion.TrySetException(exception);

        internal async Task WaitForDisposalAsync() =>
            await _disposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        internal async Task WaitForInvocationAsync() =>
            await _invocationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        private async ValueTask<TValue> AwaitOperationAsync<TValue>()
        {
            await _operationCompletion.Task;
            return default!;
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

    private sealed class RecordingLoggerFactory : ILoggerFactory
    {
        private readonly SemaphoreSlim _entryAdded = new(0);

        internal ConcurrentQueue<LogEntry> Entries { get; } = [];

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) =>
            new RecordingLogger(Entries, _entryAdded);

        internal async Task<LogEntry> WaitForEntryAsync(Func<LogEntry, bool> predicate)
        {
            while (true)
            {
                var entry = Entries.FirstOrDefault(predicate);
                if (entry is not null)
                {
                    return entry;
                }

                Assert.True(
                    await _entryAdded.WaitAsync(TimeSpan.FromSeconds(5)),
                    "Timed out waiting for the expected log entry.");
            }
        }

        public void Dispose()
        {
            _entryAdded.Dispose();
        }
    }

    private sealed class RecordingLogger(
        ConcurrentQueue<LogEntry> entries,
        SemaphoreSlim entryAdded) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
            entryAdded.Release();
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
