using Microsoft.JSInterop;

namespace Bzs.Blazor.Demo.Client.Components;

internal sealed class CatalogShellInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath =
        "./_content/Bzs.Blazor.Demo.Catalog/Components/CatalogShell.razor.js";
    private Task<IJSObjectReference>? _moduleTask;
    private string? _shellId;
    private string? _connectionId;
    private bool _disposed;

    internal async ValueTask<CatalogShellNavigationState> InitializeAsync(
        string shellId,
        DotNetObjectReference<CatalogShell> callback)
    {
        _shellId = shellId;
        var module = await GetModuleAsync();
        var state = await module.InvokeAsync<CatalogShellNavigationState>("initialize", shellId, callback);
        _connectionId = state.ConnectionId;
        return state;
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

        try
        {
            var module = await _moduleTask;
            if (_shellId is not null && _connectionId is not null)
            {
                await module.InvokeVoidAsync("dispose", _shellId, _connectionId);
            }

            await module.DisposeAsync();
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _moduleTask ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        var module = await _moduleTask;
        ObjectDisposedException.ThrowIf(_disposed, this);
        return module;
    }
}

internal sealed record CatalogShellNavigationState(bool Open, string ConnectionId);
