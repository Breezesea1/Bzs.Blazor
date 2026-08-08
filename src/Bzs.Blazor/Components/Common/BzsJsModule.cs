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

    internal async ValueTask<BzsJsInvocation<TValue>> TryInvokeAsync<
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
            var result = await module.InvokeAsync<TValue>(operationName, args);
            return new BzsJsInvocation<TValue>(true, result);
        }
        catch (Exception exception) when (IsTransientFailure(exception, importing, _options))
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
        lock (_moduleGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            module = _module;
            _module = null;
        }

        if (module is null)
        {
            return;
        }

        await DisposeModuleAsync(module);
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
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
            return await loadTask;
        }
        catch
        {
            lock (_moduleGate)
            {
                if (ReferenceEquals(_moduleLoadTask, loadTask))
                {
                    _moduleLoadTask = null;
                }
            }

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
