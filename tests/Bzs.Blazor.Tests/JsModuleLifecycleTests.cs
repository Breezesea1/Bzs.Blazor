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
        Assert.Equal(1, runtime.ImportAttempts);

        await module.DisposeAsync();
        runtime.ReleaseImport();

        Assert.False(await invocation);
        Assert.Empty(runtime.Module.Invocations);
        Assert.Equal(1, runtime.Module.DisposeAttempts);
    }

    private sealed class TestJsRuntime : IJSRuntime
    {
        internal TestJsModule Module { get; } = new();

        internal Exception? ImportFailure { get; set; }

        internal bool BlockImport { get; set; }

        internal int ImportAttempts { get; private set; }

        private TaskCompletionSource<bool>? ImportRelease { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Assert.Equal("import", identifier);
            ImportAttempts++;
            if (BlockImport)
            {
                ImportRelease ??= new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                return AwaitImportAsync<TValue>(ImportRelease.Task);
            }

            if (ImportFailure is { } exception)
            {
                ImportFailure = null;
                return ValueTask.FromException<TValue>(exception);
            }

            return ValueTask.FromResult((TValue)(object)Module);
        }

        internal void ReleaseImport() => ImportRelease?.TrySetResult(true);

        private async ValueTask<TValue> AwaitImportAsync<TValue>(Task release)
        {
            await release;
            return (TValue)(object)Module;
        }
    }

    private sealed class TestJsModule : IJSObjectReference
    {
        internal ConcurrentQueue<string> Invocations { get; } = [];

        internal Exception? OperationFailure { get; set; }

        internal Exception? DisposeFailure { get; set; }

        internal int DisposeAttempts { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (OperationFailure is { } exception)
            {
                return ValueTask.FromException<TValue>(exception);
            }

            Invocations.Enqueue(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync()
        {
            DisposeAttempts++;
            return DisposeFailure is { } exception
                ? ValueTask.FromException(exception)
                : ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingLoggerFactory : ILoggerFactory
    {
        internal List<LogEntry> Entries { get; } = [];

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(Entries);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(List<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    private sealed record LogEntry(LogLevel Level, string Message);
}
