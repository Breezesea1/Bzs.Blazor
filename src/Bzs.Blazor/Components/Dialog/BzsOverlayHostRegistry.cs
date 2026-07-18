namespace Bzs.Blazor;

/// <summary>
/// Tracks the one overlay host allowed within a Blazor service scope.
/// </summary>
internal sealed class BzsOverlayHostRegistry : IDisposable
{
    private readonly object _gate = new();
    private readonly BzsOverlayCoordinator _coordinator;
    private BzsOverlayHostState _state;

    internal BzsOverlayHostRegistry(BzsOverlayCoordinator coordinator)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    /// <summary>Gets the current host lifecycle state.</summary>
    public BzsOverlayHostState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    /// <summary>Registers the host during passive static rendering.</summary>
    public void RegisterStaticHost()
    {
        lock (_gate)
        {
            switch (_state)
            {
                case BzsOverlayHostState.Missing:
                    _state = BzsOverlayHostState.PresentStatic;
                    return;

                case BzsOverlayHostState.PresentStatic:
                case BzsOverlayHostState.ActiveInteractive:
                    throw new InvalidOperationException(
                        "Only one BzsOverlayHost can be registered per Blazor service scope. Remove the duplicate host or render it only once per interactive root.");

                case BzsOverlayHostState.Disposed:
                    throw new InvalidOperationException(
                        "The BzsOverlayHost for this service scope was disposed and cannot be registered again.");

                default:
                    throw new InvalidOperationException("The overlay host lifecycle state is not supported.");
            }
        }
    }

    /// <summary>Marks the registered host as available for interactive commands.</summary>
    public void ActivateInteractiveHost()
    {
        lock (_gate)
        {
            switch (_state)
            {
                case BzsOverlayHostState.PresentStatic:
                    _state = BzsOverlayHostState.ActiveInteractive;
                    return;

                case BzsOverlayHostState.Missing:
                    throw new InvalidOperationException(
                        "BzsOverlayHost must register during static rendering before it can activate interactively.");

                case BzsOverlayHostState.ActiveInteractive:
                    throw new InvalidOperationException(
                        "Only one BzsOverlayHost can be active per Blazor service scope.");

                case BzsOverlayHostState.Disposed:
                    throw new InvalidOperationException(
                        "The BzsOverlayHost for this service scope was disposed and cannot activate again.");

                default:
                    throw new InvalidOperationException("The overlay host lifecycle state is not supported.");
            }
        }
    }

    /// <summary>Marks the host disposed and completes active dialog requests.</summary>
    public void DisposeHost()
    {
        var shouldDisposeCoordinator = false;
        lock (_gate)
        {
            if (_state != BzsOverlayHostState.Disposed)
            {
                _state = BzsOverlayHostState.Disposed;
                shouldDisposeCoordinator = true;
            }
        }

        if (shouldDisposeCoordinator)
        {
            _coordinator.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose() => DisposeHost();
}
