using System.Globalization;

namespace Bzs.Blazor;

/// <summary>
/// Carries the calendar-owned browser capabilities: the browser's current date, day focus, and
/// scrolling the active period option into view. Panel positioning, outside interaction, and Escape
/// belong to the anchored overlay module instead.
/// </summary>
internal sealed class BzsDateInputInterop : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Form/BzsDateInput.razor.js";
    internal const string InitializeMethod = "initialize";
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

    internal async ValueTask<BzsDateInputInitialization> InitializeAsync(
        string instanceId,
        ElementReference root,
        CancellationToken cancellationToken = default)
    {
        var invocation = await _module.TryInvokeAsync<string?>(
            InitializeMethod,
            cancellationToken,
            instanceId,
            root);
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
