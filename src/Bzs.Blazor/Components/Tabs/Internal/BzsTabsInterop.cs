namespace Bzs.Blazor;

internal sealed class BzsTabsInterop : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Tabs/BzsTabs.razor.js";
    internal const string AttachMethod = "attach";
    internal const string GetDirectionMethod = "getDirection";
    internal const string DetachMethod = "detach";

    private readonly BzsJsModule _module;

    internal BzsTabsInterop(IJSRuntime jsRuntime, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null)
    {
        _module = new BzsJsModule(
            jsRuntime,
            ModulePath,
            loggerFactory,
            new BzsJsModuleOptions(
                TreatObjectDisposedAsTransient: true,
                TreatInvalidOperationDuringImportAsTransient: true));
    }

    internal async ValueTask AttachAsync(
        ElementReference tabList,
        string orientation,
        string activationMode)
    {
        await _module.TryInvokeVoidAsync(AttachMethod, tabList, orientation, activationMode);
    }

    internal async ValueTask<bool> IsRightToLeftAsync(ElementReference root)
    {
        var invocation = await _module.TryInvokeAsync<string>(GetDirectionMethod, root);
        return invocation.Succeeded
            && string.Equals(invocation.Result, "rtl", StringComparison.Ordinal);
    }

    internal async ValueTask DisposeAsync(ElementReference tabList)
    {
        Exception? disposalException = null;
        if (_module.IsLoaded)
        {
            try
            {
                await _module.TryInvokeVoidAsync(DetachMethod, tabList);
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

    ValueTask IAsyncDisposable.DisposeAsync() => _module.DisposeAsync();
}
