using Bzs.Blazor.Localization;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor;

/// <summary>
/// Renders the scoped command-dialog and toast snapshots for one application root.
/// </summary>
public sealed partial class BzsOverlayHost : BzsComponentBase, IAsyncDisposable
{
    private IReadOnlyList<BzsOverlayDialogSnapshot> _dialogs = [];
    private IReadOnlyList<BzsToastSnapshot> _toasts = [];
    private bool _registered;
    private bool _subscribed;
    private bool _disposed;

    [Inject]
    private BzsOverlayCoordinator Coordinator { get; set; } = default!;

    [Inject]
    private BzsOverlayHostRegistry HostRegistry { get; set; } = default!;

    [Inject]
    private IBzsToastService Toasts { get; set; } = default!;

    [Inject]
    private IStringLocalizer<BzsBlazorResources> Localizer { get; set; } = default!;

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-overlay-host"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-overlay-host"] = "true",
            };

            return attributes;
        }
    }

    private string NotificationsLabel => Localizer["Notifications"].Value;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        HostRegistry.RegisterStaticHost();
        _registered = true;
        _dialogs = Coordinator.Snapshot;
        _toasts = Toasts.Snapshot;
        Coordinator.Changed += HandleCoordinatorChanged;
        Toasts.Changed += HandleToastsChanged;
        _subscribed = true;
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed)
        {
            HostRegistry.ActivateInteractiveHost();
        }

        return Task.CompletedTask;
    }

    private async void HandleCoordinatorChanged(object? sender, BzsOverlayChangedEventArgs args)
    {
        try
        {
            if (_disposed)
            {
                return;
            }

            await InvokeAsync(() =>
            {
                if (!_disposed)
                {
                    _dialogs = args.Snapshot;
                    StateHasChanged();
                }
            });
        }
        catch (Exception exception) when (!_disposed)
        {
            await DispatchExceptionAsync(exception);
        }
    }

    private async void HandleToastsChanged(object? sender, BzsToastChangedEventArgs args)
    {
        try
        {
            if (_disposed)
            {
                return;
            }

            await InvokeAsync(() =>
            {
                if (!_disposed)
                {
                    _toasts = args.Snapshot;
                    StateHasChanged();
                }
            });
        }
        catch (Exception exception) when (!_disposed)
        {
            await DispatchExceptionAsync(exception);
        }
    }

    private Task DismissDialogAsync(BzsOverlayDialogId id, BzsDialogDismissReason reason)
    {
        if (!_disposed)
        {
            Coordinator.Dismiss(id, reason);
        }

        return Task.CompletedTask;
    }

    private Task DismissToastAsync(BzsToastId id, BzsToastDismissReason reason)
    {
        if (!_disposed)
        {
            Toasts.Dismiss(id, reason);
        }

        return Task.CompletedTask;
    }

    private Task PauseToastAsync(BzsToastId id, BzsToastPauseReason reason)
    {
        if (!_disposed)
        {
            Toasts.Pause(id, reason);
        }

        return Task.CompletedTask;
    }

    private Task ResumeToastAsync(BzsToastId id, BzsToastPauseReason reason)
    {
        if (!_disposed)
        {
            Toasts.Resume(id, reason);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        if (_subscribed)
        {
            Coordinator.Changed -= HandleCoordinatorChanged;
            Toasts.Changed -= HandleToastsChanged;
            _subscribed = false;
        }

        if (_registered)
        {
            HostRegistry.UnregisterHost();
            _registered = false;
        }

        return ValueTask.CompletedTask;
    }
}
