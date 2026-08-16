using Microsoft.JSInterop;
using System.Runtime.ExceptionServices;

namespace Bzs.Blazor.Demo.Client.Components;

internal sealed class DemoThemeModeSwitchInterop(IJSRuntime js) : IAsyncDisposable
{
    internal const string ModulePath =
        "./_content/Bzs.Blazor.Demo.Catalog/Components/DemoThemeModeSwitch.razor.js";
    internal const string InitializeMethod = "initialize";
    internal const string DisposeMethod = "dispose";

    private Task<IJSObjectReference>? _moduleTask;
    private bool _disposed;
    private string? _instanceToken;

    public async ValueTask InitializeAsync()
    {
        var module = await GetModuleAsync();
        _instanceToken = await module.InvokeAsync<string>(InitializeMethod);
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var moduleTask = _moduleTask ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        try
        {
            var module = await moduleTask;
            ObjectDisposedException.ThrowIf(_disposed, this);
            return module;
        }
        catch (Exception exception) when (IsTransientInitializationFailure(exception))
        {
            if (ReferenceEquals(_moduleTask, moduleTask))
            {
                _moduleTask = null;
            }

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_moduleTask is null)
        {
            return;
        }

        Exception? disposalException = null;
        IJSObjectReference? module = null;
        try
        {
            module = await _moduleTask;
            if (_instanceToken is not null)
            {
                await module.InvokeVoidAsync(DisposeMethod, _instanceToken);
            }
        }
        catch (Exception exception) when (IsTransientDisposalFailure(exception))
        {
        }
        catch (Exception exception)
        {
            disposalException = exception;
        }

        if (module is not null)
        {
            try
            {
                await module.DisposeAsync();
            }
            catch (Exception exception) when (IsTransientDisposalFailure(exception))
            {
            }
            catch (Exception exception)
            {
                disposalException ??= exception;
            }
        }

        if (disposalException is not null)
        {
            ExceptionDispatchInfo.Capture(disposalException).Throw();
        }
    }

    private static bool IsTransientInitializationFailure(Exception exception) =>
        exception is JSDisconnectedException or InvalidOperationException or TaskCanceledException;

    private static bool IsTransientDisposalFailure(Exception exception) =>
        exception is JSDisconnectedException or TaskCanceledException;
}
