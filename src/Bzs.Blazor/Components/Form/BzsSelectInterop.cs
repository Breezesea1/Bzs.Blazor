namespace Bzs.Blazor;

internal sealed class BzsSelectInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Form/BzsSelect.razor.js";
    internal const string InitializeMethod = "initialize";
    internal const string SetOpenMethod = "setOpen";
    internal const string DisposeMethod = "dispose";

    private IJSObjectReference? _module;

    private async ValueTask<IJSObjectReference> GetModuleAsync() =>
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);

    internal async ValueTask<bool> InitializeAsync<T>(
        string instanceId,
        ElementReference root,
        DotNetObjectReference<T> dotNetReference)
        where T : class
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync(InitializeMethod, instanceId, root, dotNetReference);
            return true;
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
            return false;
        }
    }

    internal async ValueTask SetOpenAsync(
        string instanceId,
        bool open,
        ElementReference? focusTarget = null)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync(SetOpenMethod, instanceId, open, focusTarget);
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    internal async ValueTask DisposeInstanceAsync(string instanceId)
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync(DisposeMethod, instanceId);
            }
            catch (Exception exception) when (IsTransientInteropFailure(exception))
            {
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    private static bool IsTransientInteropFailure(Exception exception) =>
        exception is JSDisconnectedException or TaskCanceledException or ObjectDisposedException;
}
