namespace Bzs.Blazor;

/// <summary>
/// Renders controlled application navigation that can be docked or overlaid by CSS.
/// </summary>
public sealed partial class BzsNavigationDrawer : BzsComponentBase
{
    private const double DefaultDrawerWidth = 256;
    private const string DefaultResizeHandleAccessibleName = "Resize navigation drawer";
    private static readonly TimeSpan InteropRetryInitialDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan InteropRetryMaximumDelay = TimeSpan.FromSeconds(5);

    private readonly string _overlayId = $"bzs-navigation-drawer-{Guid.NewGuid():N}";
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private BzsOverlayInterop? _interop;
    private BzsNavigationDrawerResizeInterop? _resizeInterop;
    private DotNetObjectReference<BzsNavigationDrawer>? _resizeReference;
    private ElementReference _rootElement;
    private ElementReference _panelElement;
    private ElementReference _resizeHandleElement;
    private ElementReference _escapeTriggerElement;
    private bool _interopSynchronizationPending = true;
    private bool _resizeSynchronizationPending = true;
    private bool _lastOpen;
    private BzsNavigationDrawerVariant _lastVariant;
    private BzsNavigationDrawerPosition _lastResizePosition;
    private bool _lastResizable;
    private double _lastMinimumWidth;
    private double _lastMaximumWidth;
    private double _lastResizeStep;
    private string? _lastInitialFocusSelector;
    private CancellationTokenSource? _interopRetryCancellation;
    private Task? _interopRetryTask;
    private CancellationTokenSource? _resizeRetryCancellation;
    private Task? _resizeRetryTask;
    private int _interopRetryAttempts;
    private int _resizeRetryAttempts;
    private double _currentWidth = DefaultDrawerWidth;
    private bool _disposed;

    /// <summary>
    /// Gets or sets whether the navigation drawer is open.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>
    /// Gets or sets the callback used to request an open-state change.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    /// Gets or sets how the drawer participates in the application frame.
    /// </summary>
    [Parameter]
    public BzsNavigationDrawerVariant Variant { get; set; } = BzsNavigationDrawerVariant.Responsive;

    /// <summary>
    /// Gets or sets the logical edge where the navigation drawer is anchored.
    /// </summary>
    [Parameter]
    public BzsNavigationDrawerPosition Position { get; set; } = BzsNavigationDrawerPosition.Start;

    /// <summary>
    /// Gets or sets whether selecting the overlay backdrop requests that the drawer close.
    /// </summary>
    [Parameter]
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>
    /// Gets or sets whether Escape requests that the navigation drawer close while it is modal.
    /// </summary>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Gets or sets the selector used for initial focus while the navigation drawer is modal.
    /// </summary>
    [Parameter]
    public string? InitialFocusSelector { get; set; }

    /// <summary>
    /// Gets or sets the accessible name of the navigation landmark.
    /// </summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>
    /// Gets or sets whether the drawer exposes an interactive width resize handle.
    /// </summary>
    [Parameter]
    public bool Resizable { get; set; }

    /// <summary>
    /// Gets or sets the minimum drawer width, in CSS pixels, while resizing.
    /// </summary>
    [Parameter]
    public double MinimumWidth { get; set; } = 192;

    /// <summary>
    /// Gets or sets the maximum drawer width, in CSS pixels, while resizing.
    /// </summary>
    [Parameter]
    public double MaximumWidth { get; set; } = 480;

    /// <summary>
    /// Gets or sets the number of CSS pixels applied by each keyboard resize step.
    /// </summary>
    [Parameter]
    public double ResizeStep { get; set; } = 16;

    /// <summary>
    /// Gets or sets the accessible name of the resize handle.
    /// </summary>
    [Parameter]
    public string? ResizeHandleAccessibleName { get; set; }

    /// <summary>
    /// Gets or sets the callback raised after pointer resizing completes or a keyboard resize step is applied.
    /// </summary>
    [Parameter]
    public EventCallback<double> ResizeCompleted { get; set; }

    /// <summary>
    /// Gets or sets the navigation content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string EffectiveResizeHandleAccessibleName =>
        Normalize(ResizeHandleAccessibleName) ?? DefaultResizeHandleAccessibleName;

    private string VariantName => Variant switch
    {
        BzsNavigationDrawerVariant.Persistent => "persistent",
        BzsNavigationDrawerVariant.Temporary => "temporary",
        BzsNavigationDrawerVariant.Responsive => "responsive",
        _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "The navigation drawer variant is not supported."),
    };

    private string PositionName => Position switch
    {
        BzsNavigationDrawerPosition.Start => "start",
        BzsNavigationDrawerPosition.End => "end",
        _ => throw new ArgumentOutOfRangeException(nameof(Position), Position, "The navigation drawer position is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes(
                    $"bzs-navigation-drawer bzs-navigation-drawer--{VariantName} " +
                    $"bzs-navigation-drawer--{PositionName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-navigation-drawer"] = VariantName,
                ["data-bzs-navigation-drawer-variant"] = VariantName,
                ["data-bzs-navigation-drawer-position"] = PositionName,
                ["data-bzs-open"] = Open ? "true" : "false",
                ["data-bzs-navigation-drawer-resizable"] = Resizable ? "true" : "false",
                ["data-bzs-navigation-drawer-minimum-width"] = FormatWidth(MinimumWidth),
                ["data-bzs-navigation-drawer-maximum-width"] = FormatWidth(MaximumWidth),
                ["data-bzs-navigation-drawer-resize-step"] = FormatWidth(ResizeStep),
            };

            if (!string.IsNullOrWhiteSpace(AccessibleName))
            {
                attributes["aria-label"] = AccessibleName.Trim();
            }

            if (!Open)
            {
                attributes["aria-hidden"] = "true";
                attributes["inert"] = string.Empty;
            }
            else
            {
                attributes.Remove("aria-hidden");
                attributes.Remove("inert");
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "The navigation drawer variant is not supported.");
        }

        if (!Enum.IsDefined(Position))
        {
            throw new ArgumentOutOfRangeException(nameof(Position), Position, "The navigation drawer position is not supported.");
        }

        ValidatePositiveFinite(MinimumWidth, nameof(MinimumWidth));
        ValidatePositiveFinite(MaximumWidth, nameof(MaximumWidth));
        ValidatePositiveFinite(ResizeStep, nameof(ResizeStep));
        if (MaximumWidth < MinimumWidth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumWidth),
                MaximumWidth,
                "The maximum drawer width must be greater than or equal to the minimum drawer width.");
        }

        var initialFocusSelector = Normalize(InitialFocusSelector);
        if (_lastOpen != Open
            || _lastVariant != Variant
            || _lastInitialFocusSelector != initialFocusSelector)
        {
            CancelInteropRetry();
            _interopRetryAttempts = 0;
            _interopSynchronizationPending = true;
        }

        _lastOpen = Open;
        _lastVariant = Variant;
        _lastInitialFocusSelector = initialFocusSelector;

        if (_lastResizable != Resizable
            || _lastResizePosition != Position
            || _lastMinimumWidth != MinimumWidth
            || _lastMaximumWidth != MaximumWidth
            || _lastResizeStep != ResizeStep)
        {
            CancelResizeRetry();
            _resizeRetryAttempts = 0;
            _resizeSynchronizationPending = true;
        }

        _lastResizable = Resizable;
        _lastResizePosition = Position;
        _lastMinimumWidth = MinimumWidth;
        _lastMaximumWidth = MaximumWidth;
        _lastResizeStep = ResizeStep;
        _currentWidth = Math.Clamp(_currentWidth, MinimumWidth, MaximumWidth);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (_interopSynchronizationPending || firstRender)
        {
            await SynchronizeInteropAsync();
        }

        var shouldSynchronizeResize = Resizable
            ? RendererInfo.IsInteractive
            : _resizeInterop is not null;
        if (shouldSynchronizeResize && (_resizeSynchronizationPending || firstRender))
        {
            await SynchronizeResizeInteropAsync();
        }
    }

    private async Task SynchronizeResizeInteropAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (!Resizable)
        {
            _resizeSynchronizationPending = false;
            CancelResizeRetry();
            if (_resizeInterop is not null)
            {
                await _resizeInterop.DisableAsync(_rootElement);
            }

            return;
        }

        _resizeInterop ??= new BzsNavigationDrawerResizeInterop(JS);
        _resizeReference ??= DotNetObjectReference.Create(this);
        var invocation = await _resizeInterop.ConfigureAsync(
            _rootElement,
            _panelElement,
            _resizeHandleElement,
            _resizeReference,
            MinimumWidth,
            MaximumWidth,
            ResizeStep,
            PositionName);
        _resizeSynchronizationPending = !invocation.Succeeded;
        if (invocation.Succeeded)
        {
            CancelResizeRetry();
            _resizeRetryAttempts = 0;
            var width = Math.Clamp(invocation.Result, MinimumWidth, MaximumWidth);
            if (Math.Abs(width - _currentWidth) > 0.01)
            {
                _currentWidth = width;
                StateHasChanged();
            }
        }
        else
        {
            ScheduleResizeRetry();
        }
    }

    private async Task SynchronizeInteropAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (Open)
        {
            _interop ??= new BzsOverlayInterop(JS);
            var synchronized = await _interop.ActivateNavigationDrawerAsync(
                _overlayId,
                _rootElement,
                _panelElement,
                _escapeTriggerElement,
                _lastInitialFocusSelector,
                VariantName);
            _interopSynchronizationPending = !synchronized;
            if (synchronized)
            {
                CancelInteropRetry();
                _interopRetryAttempts = 0;
            }
            else
            {
                ScheduleInteropRetry();
            }
        }
        else if (_interop is not null)
        {
            _interopSynchronizationPending = false;
            await _interop.DeactivateAsync(_overlayId);
        }
        else
        {
            _interopSynchronizationPending = false;
        }
    }

    private async Task HandleBackdropClickAsync()
    {
        if (!Open || !CloseOnBackdropClick)
        {
            return;
        }

        await OpenChanged.InvokeAsync(false);
    }

    private Task HandleKeyDownAsync(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs eventArgs)
    {
        return Open
            && Variant == BzsNavigationDrawerVariant.Temporary
            && CloseOnEscape
            && string.Equals(eventArgs.Key, "Escape", StringComparison.Ordinal)
            ? OpenChanged.InvokeAsync(false)
            : Task.CompletedTask;
    }

    private Task HandleModalEscapeAsync() => Open && CloseOnEscape
        ? OpenChanged.InvokeAsync(false)
        : Task.CompletedTask;

    /// <summary>
    /// Receives a completed browser resize operation.
    /// </summary>
    [JSInvokable]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public async Task NotifyResizeCompletedAsync(double width)
    {
        if (_disposed || !double.IsFinite(width) || width <= 0)
        {
            return;
        }

        var constrainedWidth = Math.Min(width, MaximumWidth);
        await InvokeAsync(async () =>
        {
            _currentWidth = constrainedWidth;
            StateHasChanged();
            await ResizeCompleted.InvokeAsync(constrainedWidth);
        });
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatWidth(double value) =>
        value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite and greater than zero.");
        }
    }

    private void ScheduleInteropRetry()
    {
        if (_disposed || _interopRetryTask is not null)
        {
            return;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
        var retryDelay = GetInteropRetryDelay();
        _interopRetryCancellation = cancellation;
        _interopRetryTask = RetryInteropSynchronizationAsync(cancellation, retryDelay);
    }

    private async Task RetryInteropSynchronizationAsync(
        CancellationTokenSource cancellation,
        TimeSpan retryDelay)
    {
        try
        {
            await Task.Delay(retryDelay, cancellation.Token);
            if (ReferenceEquals(_interopRetryCancellation, cancellation))
            {
                _interopRetryCancellation = null;
                _interopRetryTask = null;
            }

            await InvokeAsync(SynchronizeInteropAsync);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_interopRetryCancellation, cancellation))
            {
                _interopRetryCancellation = null;
                _interopRetryTask = null;
            }

            cancellation.Dispose();
        }
    }

    private TimeSpan GetInteropRetryDelay()
    {
        var exponent = Math.Min(_interopRetryAttempts, 6);
        var delayMilliseconds = Math.Min(
            InteropRetryInitialDelay.TotalMilliseconds * (1 << exponent),
            InteropRetryMaximumDelay.TotalMilliseconds);
        _interopRetryAttempts = Math.Min(_interopRetryAttempts + 1, 6);
        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private void CancelInteropRetry()
    {
        _interopRetryCancellation?.Cancel();
        _interopRetryCancellation = null;
        _interopRetryTask = null;
    }

    private void ScheduleResizeRetry()
    {
        if (_disposed || _resizeRetryTask is not null)
        {
            return;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
        var retryDelay = GetResizeRetryDelay();
        _resizeRetryCancellation = cancellation;
        _resizeRetryTask = RetryResizeSynchronizationAsync(cancellation, retryDelay);
    }

    private async Task RetryResizeSynchronizationAsync(
        CancellationTokenSource cancellation,
        TimeSpan retryDelay)
    {
        try
        {
            await Task.Delay(retryDelay, cancellation.Token);
            if (ReferenceEquals(_resizeRetryCancellation, cancellation))
            {
                _resizeRetryCancellation = null;
                _resizeRetryTask = null;
            }

            await InvokeAsync(SynchronizeResizeInteropAsync);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_resizeRetryCancellation, cancellation))
            {
                _resizeRetryCancellation = null;
                _resizeRetryTask = null;
            }

            cancellation.Dispose();
        }
    }

    private TimeSpan GetResizeRetryDelay()
    {
        var exponent = Math.Min(_resizeRetryAttempts, 6);
        var delayMilliseconds = Math.Min(
            InteropRetryInitialDelay.TotalMilliseconds * (1 << exponent),
            InteropRetryMaximumDelay.TotalMilliseconds);
        _resizeRetryAttempts = Math.Min(_resizeRetryAttempts + 1, 6);
        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private void CancelResizeRetry()
    {
        _resizeRetryCancellation?.Cancel();
        _resizeRetryCancellation = null;
        _resizeRetryTask = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetimeCancellation.Cancel();
        CancelInteropRetry();
        CancelResizeRetry();
        if (_resizeInterop is not null)
        {
            await _resizeInterop.DisposeAsync(_rootElement);
        }

        _resizeReference?.Dispose();
        if (_interop is not null)
        {
            await _interop.DisposeAsync(_overlayId);
        }

        _lifetimeCancellation.Dispose();
    }
}
