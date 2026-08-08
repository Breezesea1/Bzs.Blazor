using System.Globalization;

namespace Bzs.Blazor;

internal sealed class BzsDateInputInterop : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Form/BzsDateInput.razor.js";
    internal const string InitializeMethod = "initialize";
    internal const string SetOpenMethod = "setOpen";
    internal const string FocusActiveDayMethod = "focusActiveDay";
    internal const string ScrollActivePeriodOptionMethod = "scrollActivePeriodOption";
    internal const string DisposeMethod = "dispose";

    private readonly BzsJsModule _module;

    internal BzsDateInputInterop(
        IJSRuntime jsRuntime,
        Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null)
    {
        _module = new BzsJsModule(
            jsRuntime,
            ModulePath,
            loggerFactory,
            new BzsJsModuleOptions(TreatObjectDisposedAsTransient: true));
    }

    internal async ValueTask<BzsDateInputInitialization> InitializeAsync<T>(
        string instanceId,
        ElementReference root,
        DotNetObjectReference<T> dotNetReference,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var invocation = await _module.TryInvokeAsync<string?>(
            InitializeMethod,
            cancellationToken,
            instanceId,
            root,
            dotNetReference);
        if (!invocation.Succeeded || string.IsNullOrWhiteSpace(invocation.Result))
        {
            return default;
        }

        var parsed = DateOnly.TryParseExact(
            invocation.Result,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var today);
        return new BzsDateInputInitialization(true, parsed ? today : null);
    }

    internal async ValueTask<bool> SetOpenAsync(
        string instanceId,
        bool open,
        double? pointerX,
        double? pointerY,
        bool focusCalendar,
        ElementReference? focusTarget = null)
    {
        return await _module.TryInvokeVoidAsync(
            SetOpenMethod,
            instanceId,
            open,
            pointerX,
            pointerY,
            focusCalendar,
            focusTarget);
    }

    internal async ValueTask FocusActiveDayAsync(string instanceId)
    {
        await _module.TryInvokeVoidAsync(FocusActiveDayMethod, instanceId);
    }

    internal async ValueTask ScrollActivePeriodOptionAsync(ElementReference menu)
    {
        await _module.TryInvokeVoidAsync(ScrollActivePeriodOptionMethod, menu);
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

internal readonly record struct BzsDateInputInitialization(bool Initialized, DateOnly? BrowserToday);
