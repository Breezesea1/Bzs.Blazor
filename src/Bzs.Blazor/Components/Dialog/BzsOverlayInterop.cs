namespace Bzs.Blazor;

internal sealed class BzsOverlayInterop
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Dialog/BzsDialog.razor.js";
    internal const string ActivateMethod = "activate";
    internal const string ActivateNavigationDrawerMethod = "activateNavigationDrawer";
    internal const string DeactivateMethod = "deactivate";

    private readonly BzsJsModule _module;

    internal BzsOverlayInterop(IJSRuntime js, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null)
    {
        _module = new BzsJsModule(js, ModulePath, loggerFactory);
    }

    public async ValueTask ActivateAsync(
        string overlayId,
        ElementReference panel,
        bool modal,
        string? initialFocusSelector)
    {
        await _module.TryInvokeVoidAsync(
            ActivateMethod,
            overlayId,
            panel,
            modal,
            initialFocusSelector);
    }

    public ValueTask<bool> ActivateNavigationDrawerAsync(
        string overlayId,
        ElementReference root,
        ElementReference panel,
        ElementReference escapeTrigger,
        string? initialFocusSelector,
        string variant)
    {
        return _module.TryInvokeVoidAsync(
            ActivateNavigationDrawerMethod,
            overlayId,
            root,
            panel,
            escapeTrigger,
            initialFocusSelector,
            variant);
    }

    public async ValueTask DeactivateAsync(string overlayId)
    {
        if (_module.IsLoaded)
        {
            await _module.TryInvokeVoidAsync(DeactivateMethod, overlayId);
        }
    }

    public async ValueTask DisposeAsync(string overlayId)
    {
        Exception? disposalException = null;
        if (_module.IsLoaded)
        {
            try
            {
                await _module.TryInvokeVoidAsync(DeactivateMethod, overlayId);
            }
            catch (Exception exception)
            {
                disposalException = exception;
            }
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (Exception exception)
        {
            disposalException ??= exception;
        }

        if (disposalException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disposalException).Throw();
        }
    }
}
