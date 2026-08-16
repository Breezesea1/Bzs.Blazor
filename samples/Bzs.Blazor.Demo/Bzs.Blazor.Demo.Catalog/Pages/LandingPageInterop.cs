using Bzs.Blazor.Demo.Client.Components;
using Microsoft.JSInterop;

namespace Bzs.Blazor.Demo.Client.Pages;

internal sealed class LandingPageInterop(IJSRuntime js) : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor.Demo.Catalog/Pages/LandingPage.razor.js";

    private Task<IJSObjectReference>? _moduleTask;
    private bool _disposed;

    public async Task<bool> CopyTextAsync(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("copyText", text);
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
        catch (Exception exception) when (DemoThemeModeSwitchInterop.IsTransientInitializationFailure(exception))
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

        try
        {
            var module = await _moduleTask;
            await module.DisposeAsync();
        }
        catch (Exception exception) when (DemoThemeModeSwitchInterop.IsTransientDisposalFailure(exception))
        {
        }
    }
}
