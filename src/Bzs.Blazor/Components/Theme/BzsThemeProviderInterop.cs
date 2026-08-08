namespace Bzs.Blazor;

internal sealed class BzsThemeProviderInterop
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Theme/BzsThemeProvider.razor.js";
    internal const string SetSystemModeMethod = "setSystemMode";
    internal const string DisposeMethod = "dispose";

    private readonly BzsJsModule _module;

    internal BzsThemeProviderInterop(IJSRuntime js, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null)
    {
        _module = new BzsJsModule(js, ModulePath, loggerFactory);
    }

    public async ValueTask<bool> SetSystemModeAsync(
        ElementReference element,
        DotNetObjectReference<BzsThemeProvider> dotNetReference,
        bool enabled)
    {
        return await _module.InvokeAsync<bool>(SetSystemModeMethod, element, dotNetReference, enabled);
    }

    public async ValueTask DisposeAsync(ElementReference element)
    {
        Exception? disposalException = null;
        if (_module.IsLoaded)
        {
            try
            {
                await _module.TryInvokeVoidAsync(DisposeMethod, element);
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
