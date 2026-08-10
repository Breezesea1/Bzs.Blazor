namespace Bzs.Blazor;

internal sealed class BzsAnchoredOverlayInterop : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Popover/BzsPopover.razor.js";
    internal const string InitializeMethod = "initialize";
    internal const string SetOpenMethod = "setOpen";
    internal const string SetOpenAtMethod = "setOpenAt";
    internal const string DisposeMethod = "dispose";

    private readonly BzsJsModule _module;

    internal BzsAnchoredOverlayInterop(
        IJSRuntime jsRuntime,
        Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null)
    {
        _module = new BzsJsModule(
            jsRuntime,
            ModulePath,
            loggerFactory,
            new BzsJsModuleOptions(TreatObjectDisposedAsTransient: true));
    }

    internal ValueTask<bool> InitializeAsync<T>(
        string instanceId,
        ElementReference root,
        DotNetObjectReference<T> dotNetReference)
        where T : class =>
        _module.TryInvokeVoidAsync(InitializeMethod, instanceId, root, dotNetReference);

    internal ValueTask<bool> SetOpenAsync(
        string instanceId,
        bool open,
        string placement,
        bool closeOnOutsideInteraction,
        bool closeOnEscape,
        bool restoreFocus) =>
        _module.TryInvokeVoidAsync(
            SetOpenMethod,
            instanceId,
            open,
            placement,
            closeOnOutsideInteraction,
            closeOnEscape,
            restoreFocus);

    internal ValueTask<bool> SetOpenAtAsync(
        string instanceId,
        bool open,
        string placement,
        bool closeOnOutsideInteraction,
        bool closeOnEscape,
        bool restoreFocus,
        double? clientX,
        double? clientY) =>
        _module.TryInvokeVoidAsync(
            SetOpenAtMethod,
            instanceId,
            open,
            placement,
            closeOnOutsideInteraction,
            closeOnEscape,
            restoreFocus,
            clientX,
            clientY);

    internal async ValueTask DisposeInstanceAsync(string instanceId)
    {
        if (_module.IsLoaded)
        {
            await _module.TryInvokeVoidAsync(DisposeMethod, instanceId);
        }
    }

    public ValueTask DisposeAsync() => _module.DisposeAsync();
}
