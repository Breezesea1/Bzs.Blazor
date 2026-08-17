using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class CatalogShell : ComponentBase, IAsyncDisposable
{
    private const string ShellId = "demo-app-shell";
    private CatalogShellInterop? _interop;
    private DotNetObjectReference<CatalogShell>? _selfReference;
    private bool _navigationOpen = true;
    private bool _interactiveControllerReady;
    private bool _disposed;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment NavigationContent { get; set; } = default!;

    [Parameter, EditorRequired]
    public string Status { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public RenderFragment HeaderTools { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment Content { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !RendererInfo.IsInteractive || _disposed)
        {
            return;
        }

        _interop = new CatalogShellInterop(JS);
        _selfReference = DotNetObjectReference.Create(this);
        try
        {
            var state = await _interop.InitializeAsync(ShellId, _selfReference);
            _navigationOpen = state.Open;
            _interactiveControllerReady = true;
            StateHasChanged();
        }
        catch (JSException)
        {
            // The static shell controller remains active when interop is unavailable.
        }
        catch (InvalidOperationException)
        {
            // Static SSR and a disconnected renderer retain passive shell behavior.
        }
    }

    private Task AcceptNavigationOpenAsync(bool open)
    {
        _navigationOpen = open;
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task HandleNavigationRequested(bool open)
    {
        if (!_disposed)
        {
            _navigationOpen = open;
            StateHasChanged();
        }

        return Task.CompletedTask;
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
            await _interop.DisposeAsync();
        }

        _selfReference?.Dispose();
    }
}
