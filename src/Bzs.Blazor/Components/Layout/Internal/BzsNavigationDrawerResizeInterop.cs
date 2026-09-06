namespace Bzs.Blazor;

internal sealed class BzsNavigationDrawerResizeInterop
{
    internal const string ModulePath =
        "./_content/Bzs.Blazor/Components/Layout/BzsNavigationDrawer.razor.js";
    internal const string ConfigureMethod = "configure";
    internal const string DisableMethod = "disable";

    private readonly BzsJsModule _module;

    internal BzsNavigationDrawerResizeInterop(IJSRuntime js)
    {
        _module = new BzsJsModule(
            js,
            ModulePath,
            options: new BzsJsModuleOptions(
                TreatInvalidOperationDuringImportAsTransient: true));
    }

    internal ValueTask<BzsJsInvocation<double>> ConfigureAsync(
        ElementReference root,
        ElementReference panel,
        ElementReference handle,
        DotNetObjectReference<BzsNavigationDrawer> dotNetReference,
        double minimumWidth,
        double maximumWidth,
        double resizeStep,
        string position)
    {
        return _module.TryInvokeAsync<double>(
            ConfigureMethod,
            root,
            panel,
            handle,
            dotNetReference,
            minimumWidth,
            maximumWidth,
            resizeStep,
            position);
    }

    internal async ValueTask DisableAsync(ElementReference root)
    {
        if (_module.IsLoaded)
        {
            await _module.TryInvokeVoidAsync(DisableMethod, root);
        }
    }

    internal async ValueTask DisposeAsync(ElementReference root)
    {
        await DisableAsync(root);
        await _module.DisposeAsync();
    }
}
