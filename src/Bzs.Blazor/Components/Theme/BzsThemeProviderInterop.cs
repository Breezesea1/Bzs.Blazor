namespace Bzs.Blazor;

internal sealed class BzsThemeProviderInterop(IJSRuntime js)
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Theme/BzsThemeProvider.razor.js";
    internal const string SetSystemModeMethod = "setSystemMode";
    internal const string DisposeMethod = "dispose";

    private IJSObjectReference? _module;

    public async ValueTask<bool> SetSystemModeAsync(
        ElementReference element,
        DotNetObjectReference<BzsThemeProvider> dotNetReference,
        bool enabled)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>(SetSystemModeMethod, element, dotNetReference, enabled);
    }

    public async ValueTask DisposeAsync(ElementReference element)
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync(DisposeMethod, element);
                await _module.DisposeAsync();
            }
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    private static bool IsTransientInteropFailure(Exception exception) =>
        exception is JSDisconnectedException or TaskCanceledException;
}
