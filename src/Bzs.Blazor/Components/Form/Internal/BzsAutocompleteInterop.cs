namespace Bzs.Blazor;

internal sealed class BzsAutocompleteInterop : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Form/BzsAutocomplete.razor.js";
    internal const string InitializeMethod = "initialize";
    internal const string DisposeMethod = "dispose";

    private readonly BzsJsModule _module;

    internal BzsAutocompleteInterop(
        IJSRuntime jsRuntime,
        Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null)
    {
        _module = new BzsJsModule(
            jsRuntime,
            ModulePath,
            loggerFactory,
            new BzsJsModuleOptions(TreatObjectDisposedAsTransient: true));
    }

    internal ValueTask<bool> InitializeAsync(string instanceId, ElementReference root) =>
        _module.TryInvokeVoidAsync(InitializeMethod, instanceId, root);

    internal async ValueTask DisposeInstanceAsync(string instanceId)
    {
        if (_module.IsLoaded)
        {
            await _module.TryInvokeVoidAsync(DisposeMethod, instanceId);
        }
    }

    public ValueTask DisposeAsync() => _module.DisposeAsync();
}
