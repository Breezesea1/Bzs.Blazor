namespace Bzs.Blazor;

/// <summary>
/// Carries native required-validation focus recovery for the enhanced select-family controls. The
/// panel's positioning, outside interaction, and Escape handling belong to the anchored overlay
/// module instead.
/// </summary>
internal sealed class BzsSelectInterop : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Form/BzsSelect.razor.js";
    internal const string InitializeMethod = "initialize";
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
