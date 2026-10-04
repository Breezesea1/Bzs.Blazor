using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bzs.Blazor;

/// <summary>
/// Owns the shared activation lifecycle of the modal panel components.
/// </summary>
/// <remarks>
/// Dialog and drawer shells own their parameters, markup, and public events.
/// This type owns the render-diff synchronization and the lazily created
/// JavaScript bridge that keeps the panel focus and modality in sync.
/// </remarks>
internal sealed class BzsModalPanelLifecycle : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly string _overlayId;
    private BzsOverlayInterop? _interop;
    private bool _interopSynchronizationPending = true;
    private bool _lastOpen;
    private bool _lastModal;
    private string? _lastInitialFocusSelector;
    private bool _disposed;

    internal BzsModalPanelLifecycle(
        IJSRuntime jsRuntime,
        ILoggerFactory? loggerFactory,
        string overlayId)
    {
        _jsRuntime = jsRuntime;
        _loggerFactory = loggerFactory;
        _overlayId = overlayId;
    }

    /// <summary>
    /// Records the latest panel parameters and flags a pending synchronization
    /// when the open state, modality, or initial focus selector changed.
    /// </summary>
    internal void NotifyParameters(bool open, bool modal, string? initialFocusSelector)
    {
        var selector = Normalize(initialFocusSelector);
        if (_lastOpen != open || _lastModal != modal || _lastInitialFocusSelector != selector)
        {
            _interopSynchronizationPending = true;
        }

        _lastOpen = open;
        _lastModal = modal;
        _lastInitialFocusSelector = selector;
    }

    /// <summary>
    /// Applies the pending activation or deactivation against the overlay bridge.
    /// </summary>
    internal async Task SynchronizeAsync(bool open, bool firstRender, ElementReference panelElement)
    {
        if (_disposed || (!_interopSynchronizationPending && !firstRender))
        {
            return;
        }

        _interopSynchronizationPending = false;
        if (open)
        {
            _interop ??= new BzsOverlayInterop(_jsRuntime, _loggerFactory);
            await _interop.ActivateAsync(_overlayId, panelElement, _lastModal, _lastInitialFocusSelector);
        }
        else if (_interop is not null)
        {
            await _interop.DeactivateAsync(_overlayId);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        return _interop is not null
            ? _interop.DisposeAsync(_overlayId)
            : ValueTask.CompletedTask;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
