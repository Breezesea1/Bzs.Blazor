using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bzs.Blazor;

internal sealed class BzsJsModule : IAsyncDisposable
{
    private const string ImportIdentifier = "import";
    private const string ModuleDisposeOperation = "dispose-module";

    private readonly IJSRuntime _jsRuntime;
    private readonly string _modulePath;
    private readonly ILogger<BzsJsModule> _logger;
    private readonly BzsJsModuleOptions _options;
    private readonly object _moduleGate = new();
    private IJSObjectReference? _module;
    private Task<IJSObjectReference>? _moduleLoadTask;
    private bool _disposed;

    internal BzsJsModule(
        IJSRuntime jsRuntime,
        string modulePath,
        ILoggerFactory? loggerFactory = null,
        BzsJsModuleOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);

        _jsRuntime = jsRuntime;
        _modulePath = modulePath;
        _logger = loggerFactory?.CreateLogger<BzsJsModule>() ?? NullLogger<BzsJsModule>.Instance;
        _options = options;
    }

    internal bool IsLoaded
    {
        get
        {
            lock (_moduleGate)
            {
                return _module is not null;
            }
        }
    }

    internal async ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicFields
            | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string operationName,
        params object?[]? args)
    {
        var importing = !IsLoaded;
        try
        {
            var module = await GetModuleAsync();
            importing = false;
            return await module.InvokeAsync<TValue>(operationName, args);
        }
        catch (Exception exception) when (IsTransientFailure(exception, importing, _options))
        {
            LogTransientFailure(exception, operationName);
            throw;
        }
    }

    internal ValueTask<BzsJsInvocation<TValue>> TryInvokeAsync<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicFields
            | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string operationName,
        params object?[]? args) =>
        TryInvokeAsync<TValue>(operationName, CancellationToken.None, args);

    internal async ValueTask<BzsJsInvocation<TValue>> TryInvokeAsync<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicFields
            | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string operationName,
        CancellationToken cancellationToken,
        params object?[]? args)
    {
        var importing = !IsLoaded;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var module = await GetModuleAsync(cancellationToken, operationName);
            importing = false;
            cancellationToken.ThrowIfCancellationRequested();
            var invocationTask = module.InvokeAsync<TValue>(operationName, args).AsTask();
            TValue result;
            try
            {
                result = cancellationToken.CanBeCanceled
                    ? await invocationTask.WaitAsync(cancellationToken)
                    : await invocationTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _ = ObserveInvocationAfterCancellationAsync(invocationTask, operationName);
                throw;
            }
            return new BzsJsInvocation<TValue>(true, result);
        }
        catch (Exception exception) when (
            IsTransientFailure(exception, importing, _options)
            || (cancellationToken.IsCancellationRequested && exception is OperationCanceledException))
        {
            LogTransientFailure(exception, operationName);
            return default;
        }
    }

    internal async ValueTask<bool> TryInvokeVoidAsync(string operationName, params object?[]? args)
    {
        var importing = !IsLoaded;
        try
        {
            var module = await GetModuleAsync();
            importing = false;
            await module.InvokeVoidAsync(operationName, args);
            return true;
        }
        catch (Exception exception) when (IsTransientFailure(exception, importing, _options))
        {
            LogTransientFailure(exception, operationName);
            return false;
        }
    }

    internal static bool IsTransientFailure(Exception exception) =>
        exception is JSDisconnectedException
            or TaskCanceledException
            or BzsJsModuleDisposalRaceException;

    public async ValueTask DisposeAsync()
    {
        IJSObjectReference? module;
        Task<IJSObjectReference>? moduleLoadTask;
        lock (_moduleGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            module = _module;
            _module = null;
            moduleLoadTask = _moduleLoadTask;
        }

        if (module is null)
        {
            if (moduleLoadTask is not null)
            {
                _ = ObserveAbandonedModuleLoadAsync(
                    moduleLoadTask,
                    ModuleDisposeOperation,
                    abandonedAfterDisposal: true);
            }
            return;
        }

        await DisposeModuleAsync(module);
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync(
        CancellationToken cancellationToken = default,
        string operationName = ImportIdentifier)
    {
        Task<IJSObjectReference> loadTask;
        lock (_moduleGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_module is not null)
            {
                return _module;
            }

            loadTask = _moduleLoadTask ??= LoadModuleAsync();
        }

        try
        {
            return cancellationToken.CanBeCanceled
                ? await loadTask.WaitAsync(cancellationToken)
                : await loadTask;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _ = ObserveAbandonedModuleLoadAsync(
                loadTask,
                operationName,
                abandonedAfterDisposal: false);
            throw;
        }
        catch
        {
            ClearModuleLoadTask(loadTask);
            throw;
        }
    }

    private async Task<IJSObjectReference> LoadModuleAsync()
    {
        var module = await _jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, _modulePath);
        var disposeAfterLoad = false;
        lock (_moduleGate)
        {
            if (_disposed)
            {
                disposeAfterLoad = true;
            }
            else
            {
                _module = module;
            }
        }

        if (disposeAfterLoad)
        {
            await DisposeModuleAsync(module);
            throw new BzsJsModuleDisposalRaceException();
        }

        return module;
    }

    private async Task ObserveAbandonedModuleLoadAsync(
        Task<IJSObjectReference> moduleLoadTask,
        string operationName,
        bool abandonedAfterDisposal)
    {
        try
        {
            await moduleLoadTask;
        }
        catch (Exception exception)
        {
            if (!ClearModuleLoadTask(moduleLoadTask))
            {
                return;
            }

            if (IsTransientFailure(exception, importing: true, _options))
            {
                LogTransientFailure(exception, operationName);
            }
            else if (abandonedAfterDisposal)
            {
                _logger.LogError(
                    exception,
                    "JavaScript module {ModulePath} failed while completing a load after disposal.",
                    _modulePath);
            }
            else
            {
                _logger.LogError(
                    exception,
                    "JavaScript module {ModulePath} failed while completing a load after the caller canceled operation {OperationName}.",
                    _modulePath,
                    operationName);
            }
        }
    }

    private async Task ObserveInvocationAfterCancellationAsync<TValue>(
        Task<TValue> invocationTask,
        string operationName)
    {
        try
        {
            await invocationTask;
        }
        catch (Exception exception) when (IsTransientFailure(exception, importing: false, _options))
        {
            LogTransientFailure(exception, operationName);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "JavaScript module {ModulePath} operation {OperationName} failed after its caller canceled.",
                _modulePath,
                operationName);
        }
    }

    private bool ClearModuleLoadTask(Task<IJSObjectReference> moduleLoadTask)
    {
        lock (_moduleGate)
        {
            if (!ReferenceEquals(_moduleLoadTask, moduleLoadTask))
            {
                return false;
            }

            _moduleLoadTask = null;
            return true;
        }
    }

    private async ValueTask DisposeModuleAsync(IJSObjectReference module)
    {
        try
        {
            await module.DisposeAsync();
        }
        catch (Exception exception) when (IsTransientFailure(exception, importing: false, _options))
        {
            LogTransientFailure(exception, ModuleDisposeOperation);
        }
    }

    private static bool IsTransientFailure(
        Exception exception,
        bool importing,
        BzsJsModuleOptions options) =>
        IsTransientFailure(exception)
        || (options.TreatObjectDisposedAsTransient && exception is ObjectDisposedException)
        || (importing
            && options.TreatInvalidOperationDuringImportAsTransient
            && exception is InvalidOperationException);

    private void LogTransientFailure(Exception exception, string operationName) =>
        _logger.LogDebug(
            exception,
            "Transient JavaScript interop failure for module {ModulePath} during operation {OperationName}.",
            _modulePath,
            operationName);

    private sealed class BzsJsModuleDisposalRaceException()
        : ObjectDisposedException(nameof(BzsJsModule));
}

internal readonly record struct BzsJsModuleOptions(
    bool TreatObjectDisposedAsTransient = false,
    bool TreatInvalidOperationDuringImportAsTransient = false);

internal readonly record struct BzsJsInvocation<TValue>(bool Succeeded, TValue Result);
