namespace Bzs.Blazor;

internal sealed class BzsOverlayInterop(IJSRuntime js)
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Dialog/BzsDialog.razor.js";
    internal const string ActivateMethod = "activate";
    internal const string DeactivateMethod = "deactivate";

    private IJSObjectReference? _module;

    public async ValueTask ActivateAsync(
        string overlayId,
        ElementReference panel,
        bool modal,
        string? initialFocusSelector)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync(ActivateMethod, overlayId, panel, modal, initialFocusSelector);
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    public async ValueTask DeactivateAsync(string overlayId)
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync(DeactivateMethod, overlayId);
            }
            catch (Exception exception) when (IsTransientInteropFailure(exception))
            {
            }
        }
    }

    public async ValueTask DisposeAsync(string overlayId)
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync(DeactivateMethod, overlayId);
                await _module.DisposeAsync();
            }
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    private static bool IsTransientInteropFailure(Exception exception) =>
        exception is JSDisconnectedException or TaskCanceledException;
}
