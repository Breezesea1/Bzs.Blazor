namespace Bzs.Blazor;

internal sealed class BzsSelectInterop : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Form/BzsSelect.razor.js";
    internal const string InitializeMethod = "initialize";
    internal const string SetOpenMethod = "setOpen";
    internal const string DisposeMethod = "dispose";

    private readonly BzsJsModule _module;

    internal BzsSelectInterop(
        IJSRuntime jsRuntime,
        Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null)
    {
        _module = new BzsJsModule(
            jsRuntime,
            ModulePath,
            loggerFactory,
            new BzsJsModuleOptions(TreatObjectDisposedAsTransient: true));
    }

    internal async ValueTask<bool> InitializeAsync<T>(
        string instanceId,
        ElementReference root,
        DotNetObjectReference<T> dotNetReference)
        where T : class
    {
        return await _module.TryInvokeVoidAsync(
            InitializeMethod,
            instanceId,
            root,
            dotNetReference);
    }

    internal async ValueTask SetOpenAsync(
        string instanceId,
        bool open,
        ElementReference? focusTarget = null)
    {
        await _module.TryInvokeVoidAsync(SetOpenMethod, instanceId, open, focusTarget);
    }

    internal async ValueTask DisposeInstanceAsync(string instanceId)
    {
        if (_module.IsLoaded)
        {
            await _module.TryInvokeVoidAsync(DisposeMethod, instanceId);
        }
    }

    public ValueTask DisposeAsync() => _module.DisposeAsync();
}
