using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class DemoThemeModeSwitch : ComponentBase, IAsyncDisposable
{
    private const int MaximumInitializationAttempts = 3;
    private DemoThemeModeSwitchInterop? _interop;
    private bool _disposed;
    private bool _initialized;
    private int _initializationAttempts;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || _initialized || _initializationAttempts >= MaximumInitializationAttempts)
        {
            return;
        }

        _initializationAttempts++;
        _interop ??= new DemoThemeModeSwitchInterop(JS);
        try
        {
            await _interop.InitializeAsync();
            _initialized = true;
        }
        catch (Exception exception) when (DemoThemeModeSwitchInterop.IsTransientInitializationFailure(exception))
        {
        }

        if (!_disposed && !_initialized && _initializationAttempts < MaximumInitializationAttempts)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_interop is not null)
        {
            try
            {
                await _interop.DisposeAsync();
            }
            catch (Exception exception) when (DemoThemeModeSwitchInterop.IsTransientDisposalFailure(exception))
            {
            }
        }
    }
}
