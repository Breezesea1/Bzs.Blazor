using System.Globalization;

namespace Bzs.Blazor;

internal sealed class BzsDateInputInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Form/BzsDateInput.razor.js";
    internal const string InitializeMethod = "initialize";
    internal const string SetOpenMethod = "setOpen";
    internal const string FocusActiveDayMethod = "focusActiveDay";
    internal const string ScrollActivePeriodOptionMethod = "scrollActivePeriodOption";
    internal const string DisposeMethod = "dispose";

    private IJSObjectReference? _module;

    private async ValueTask<IJSObjectReference> GetModuleAsync() =>
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);

    internal async ValueTask<BzsDateInputInitialization> InitializeAsync<T>(
        string instanceId,
        ElementReference root,
        DotNetObjectReference<T> dotNetReference)
        where T : class
    {
        try
        {
            var module = await GetModuleAsync();
            var browserDate = await module.InvokeAsync<string>(InitializeMethod, instanceId, root, dotNetReference);
            var parsed = DateOnly.TryParseExact(
                browserDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var today);
            return new BzsDateInputInitialization(true, parsed ? today : null);
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
            return default;
        }
    }

    internal async ValueTask<bool> SetOpenAsync(
        string instanceId,
        bool open,
        double? pointerX,
        double? pointerY,
        bool focusCalendar,
        ElementReference? focusTarget = null)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync(
                SetOpenMethod,
                instanceId,
                open,
                pointerX,
                pointerY,
                focusCalendar,
                focusTarget);
            return true;
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
            return false;
        }
    }

    internal async ValueTask FocusActiveDayAsync(string instanceId)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync(FocusActiveDayMethod, instanceId);
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    internal async ValueTask ScrollActivePeriodOptionAsync(ElementReference menu)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync(ScrollActivePeriodOptionMethod, menu);
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    internal async ValueTask DisposeInstanceAsync(string instanceId)
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync(DisposeMethod, instanceId);
            }
            catch (Exception exception) when (IsTransientInteropFailure(exception))
            {
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    private static bool IsTransientInteropFailure(Exception exception) =>
        exception is JSDisconnectedException or TaskCanceledException or ObjectDisposedException;
}

internal readonly record struct BzsDateInputInitialization(bool Initialized, DateOnly? BrowserToday);
