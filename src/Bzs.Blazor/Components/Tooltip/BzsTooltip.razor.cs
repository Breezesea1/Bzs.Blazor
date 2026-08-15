using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Decorates supplied content with a transient, non-interactive tooltip.</summary>
public sealed partial class BzsTooltip : BzsComponentBase, IAsyncDisposable
{
    private const int ImmediateInteropAttemptLimit = 3;
    private readonly string _tooltipId = $"bzs-tooltip-content-{Guid.NewGuid():N}";
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private ElementReference _rootElement;
    private BzsAnchoredOverlaySession? _overlaySession;
    private CancellationTokenSource? _delayCancellation;
    private bool _pointerInside;
    private bool _focusInside;
    private long? _touchPointerId;
    private bool _open;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the template that renders the single interactive element
    /// that reveals the tooltip on hover or focus.
    /// </summary>
    /// <remarks>
    /// Apply <see cref="BzsTooltipTriggerContext.Attributes" /> to that element.
    /// The tooltip does not add a focusable wrapper around the supplied trigger.
    /// </remarks>
    [Parameter, EditorRequired]
    public RenderFragment<BzsTooltipTriggerContext>? TriggerContent { get; set; }

    /// <summary>Gets or sets plain tooltip text. Supply exactly one of Text or TooltipContent.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>Gets or sets rich, non-interactive tooltip content.</summary>
    [Parameter]
    public RenderFragment? TooltipContent { get; set; }

    /// <summary>Gets or sets an accessible name for an icon-only trigger.</summary>
    [Parameter]
    public string? TriggerAccessibleName { get; set; }

    /// <summary>Gets or sets the preferred logical tooltip placement.</summary>
    [Parameter]
    public BzsPopoverPlacement Placement { get; set; } = BzsPopoverPlacement.Top;

    /// <summary>Gets or sets the delay before a hovered or focused tooltip appears.</summary>
    [Parameter]
    public TimeSpan ShowDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Gets or sets the delay before a tooltip disappears after hover or focus leaves.</summary>
    [Parameter]
    public TimeSpan HideDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Gets or sets whether the tooltip cannot be revealed.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    internal Func<TimeSpan, CancellationToken, Task> DelayAsync { get; set; } =
        static (delay, cancellationToken) => Task.Delay(delay, cancellationToken);

    private string? EffectiveTriggerAccessibleName => Normalize(TriggerAccessibleName);

    private BzsTooltipTriggerContext TriggerContext => new(BuildTriggerAttributes());
    private string PlacementName => BzsAnchoredOverlaySession.GetPlacementName(Placement);

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-tooltip"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-tooltip"] = "true",
                ["data-bzs-open"] = _open ? "true" : "false",
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (TriggerContent is null)
        {
            throw new InvalidOperationException("BzsTooltip requires TriggerContent.");
        }

        if (string.IsNullOrWhiteSpace(Text) == (TooltipContent is null))
        {
            throw new InvalidOperationException("BzsTooltip requires exactly one of Text or TooltipContent.");
        }

        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "The tooltip placement is not supported.");
        }

        if (ShowDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ShowDelay), ShowDelay, "ShowDelay cannot be negative.");
        }

        if (HideDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(HideDelay), HideDelay, "HideDelay cannot be negative.");
        }

        if (Disabled)
        {
            CancelDelay();
            _pointerInside = false;
            _focusInside = false;
            _touchPointerId = null;
            if (_open)
            {
                _open = false;
            }
        }

        UpdateOverlayState();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        await GetOverlaySession().AfterRenderAsync(_rootElement);
    }

    private Task HandlePointerEnterAsync(PointerEventArgs args)
    {
        if (Disabled || string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        _pointerInside = true;
        return ScheduleVisibilityAsync(true, ShowDelay);
    }

    private Task HandlePointerLeaveAsync(PointerEventArgs args)
    {
        if (string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        _pointerInside = false;
        return _focusInside ? Task.CompletedTask : ScheduleVisibilityAsync(false, HideDelay);
    }

    private Task HandleFocusInAsync(FocusEventArgs _)
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        _focusInside = true;
        return ScheduleVisibilityAsync(true, ShowDelay);
    }

    private Task HandleFocusOutAsync(FocusEventArgs _)
    {
        _focusInside = false;
        return _pointerInside ? Task.CompletedTask : ScheduleVisibilityAsync(false, HideDelay);
    }

    private Task HandlePointerDownAsync(PointerEventArgs args)
    {
        if (Disabled || !string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        CancelDelay();
        _pointerInside = false;
        _focusInside = false;
        _touchPointerId = args.PointerId;
        return Task.CompletedTask;
    }

    private Task HandlePointerUpAsync(PointerEventArgs args)
    {
        if (Disabled
            || !string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase)
            || _touchPointerId != args.PointerId)
        {
            return Task.CompletedTask;
        }

        _touchPointerId = null;
        return ScheduleVisibilityAsync(!_open, TimeSpan.Zero);
    }

    private Task HandlePointerCancelAsync(PointerEventArgs args)
    {
        if (string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase)
            && _touchPointerId == args.PointerId)
        {
            _touchPointerId = null;
            CancelDelay();
        }

        return Task.CompletedTask;
    }

    private async Task ScheduleVisibilityAsync(bool open, TimeSpan delay)
    {
        CancelDelay();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
        _delayCancellation = cancellation;
        try
        {
            if (delay > TimeSpan.Zero)
            {
                await DelayAsync(delay, cancellation.Token);
            }

            if (_disposed || cancellation.IsCancellationRequested || !ReferenceEquals(_delayCancellation, cancellation))
            {
                return;
            }

            var shouldOpen = !Disabled && (_pointerInside || _focusInside || delay == TimeSpan.Zero);
            var nextOpen = open && shouldOpen;
            if (_open == nextOpen)
            {
                return;
            }

            _open = nextOpen;
            UpdateOverlayState();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_delayCancellation, cancellation))
            {
                _delayCancellation = null;
            }
            cancellation.Dispose();
        }
    }

    /// <summary>Dismisses the tooltip after a browser-owned outside or Escape interaction.</summary>
    public Task CloseFromBrowserAsync(bool restoreFocus = false)
    {
        if (_disposed || !_open)
        {
            return Task.CompletedTask;
        }

        return GetOverlaySession().CloseFromBrowserAsync(restoreFocus);
    }

    private Task HandleCloseRequestedAsync()
    {
        if (_disposed || !_open)
        {
            return Task.CompletedTask;
        }

        return InvokeAsync(() =>
        {
            if (!_disposed && _open)
            {
                CancelDelay();
                _pointerInside = false;
                _focusInside = false;
                _touchPointerId = null;
                _open = false;
                UpdateOverlayState();
                StateHasChanged();
            }
        });
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
        CancelDelay();
        Exception? disposalException = null;
        if (_overlaySession is not null)
        {
            try
            {
                await _overlaySession.DisposeAsync();
            }
            catch (Exception exception)
            {
                disposalException = exception;
            }

            _overlaySession = null;
        }

        _lifetimeCancellation.Dispose();

        if (disposalException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disposalException).Throw();
        }
    }

    private BzsAnchoredOverlaySession GetOverlaySession() =>
        _overlaySession ??= new BzsAnchoredOverlaySession(
            JS,
            HandleCloseRequestedAsync,
            ImmediateInteropAttemptLimit,
            LoggerFactory);

    private void UpdateOverlayState() =>
        GetOverlaySession().SetDesiredState(new BzsAnchoredOverlayState(
            _open,
            Placement,
            CloseOnOutsideInteraction: true,
            CloseOnEscape: true,
            RestoreFocusOnBrowserClose: false));

    private void CancelDelay()
    {
        _delayCancellation?.Cancel();
        _delayCancellation = null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IReadOnlyDictionary<string, object> BuildTriggerAttributes()
    {
        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["data-bzs-anchor"] = "true",
            ["onpointerenter"] = EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerEnterAsync),
            ["onpointerleave"] = EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerLeaveAsync),
            ["onpointerdown"] = EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerDownAsync),
            ["onpointerup"] = EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerUpAsync),
            ["onpointercancel"] = EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerCancelAsync),
        };

        if (_open)
        {
            attributes["aria-describedby"] = _tooltipId;
        }

        if (EffectiveTriggerAccessibleName is { } accessibleName)
        {
            attributes["aria-label"] = accessibleName;
        }

        return attributes;
    }
}
