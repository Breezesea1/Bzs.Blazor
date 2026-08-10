namespace Bzs.Blazor;

/// <summary>Renders controlled content anchored to an owned native trigger.</summary>
public sealed partial class BzsPopover : BzsComponentBase, IAsyncDisposable
{
    private const int ImmediateInteropAttemptLimit = 2;
    private readonly string _instanceId = $"bzs-popover-{Guid.NewGuid():N}";
    private readonly string _panelId = $"bzs-popover-panel-{Guid.NewGuid():N}";
    private ElementReference _rootElement;
    private DotNetObjectReference<BzsPopover>? _dotNetReference;
    private BzsAnchoredOverlayInterop? _interop;
    private bool _interopInitialized;
    private int _initializationAttemptCount;
    private bool _synchronizationPending = true;
    private int _synchronizationVersion;
    private int _synchronizationAttemptCount;
    private bool _lastOpen;
    private BzsPopoverPlacement _lastPlacement;
    private bool _lastCloseOnOutsideInteraction;
    private bool _lastCloseOnEscape;
    private bool _restoreFocusPending;
    private bool _disposed;

    /// <summary>Gets or sets whether the popover content is visible.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Gets or sets the callback used to request an open-state change.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Gets or sets the preferred logical content placement.</summary>
    [Parameter]
    public BzsPopoverPlacement Placement { get; set; } = BzsPopoverPlacement.BottomStart;

    /// <summary>Gets or sets whether interaction outside the popover requests closure.</summary>
    [Parameter]
    public bool CloseOnOutsideInteraction { get; set; } = true;

    /// <summary>Gets or sets whether Escape requests closure.</summary>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>Gets or sets whether Escape dismissal restores focus to the trigger.</summary>
    [Parameter]
    public bool RestoreFocusOnEscape { get; set; } = true;

    /// <summary>Gets or sets whether the trigger is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets an explicit accessible name for an icon-only trigger.</summary>
    [Parameter]
    public string? TriggerAccessibleName { get; set; }

    /// <summary>Gets or sets the optional role of the anchored content.</summary>
    [Parameter]
    public string? ContentRole { get; set; }

    /// <summary>Gets or sets an accessible name for the anchored content.</summary>
    [Parameter]
    public string? ContentAccessibleName { get; set; }

    /// <summary>Gets or sets the content rendered inside the native trigger.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? TriggerContent { get; set; }

    /// <summary>Gets or sets the anchored content.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    private string? EffectiveTriggerAccessibleName => Normalize(TriggerAccessibleName);
    private string? EffectivePanelAccessibleName => Normalize(ContentAccessibleName);
    private string? PanelRole => Normalize(ContentRole);

    private string PlacementName => Placement switch
    {
        BzsPopoverPlacement.BottomStart => "bottom-start",
        BzsPopoverPlacement.Bottom => "bottom",
        BzsPopoverPlacement.BottomEnd => "bottom-end",
        BzsPopoverPlacement.TopStart => "top-start",
        BzsPopoverPlacement.Top => "top",
        BzsPopoverPlacement.TopEnd => "top-end",
        BzsPopoverPlacement.Start => "start",
        BzsPopoverPlacement.End => "end",
        _ => throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "The popover placement is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-popover"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-popover"] = "true",
                ["data-bzs-open"] = Open ? "true" : "false",
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "The popover placement is not supported.");
        }

        if (TriggerContent is null)
        {
            throw new InvalidOperationException("BzsPopover requires TriggerContent.");
        }

        if (ChildContent is null)
        {
            throw new InvalidOperationException("BzsPopover requires ChildContent.");
        }

        if (_lastOpen != Open
            || _lastPlacement != Placement
            || _lastCloseOnOutsideInteraction != CloseOnOutsideInteraction
            || _lastCloseOnEscape != CloseOnEscape)
        {
            RequestSynchronization();
        }

        if (Open)
        {
            _restoreFocusPending = false;
        }

        _lastOpen = Open;
        _lastPlacement = Placement;
        _lastCloseOnOutsideInteraction = CloseOnOutsideInteraction;
        _lastCloseOnEscape = CloseOnEscape;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (!_interopInitialized)
        {
            _interop ??= new BzsAnchoredOverlayInterop(JS, LoggerFactory);
            _dotNetReference ??= DotNetObjectReference.Create(this);
            _initializationAttemptCount++;
            _interopInitialized = await _interop.InitializeAsync(
                _instanceId,
                _rootElement,
                _dotNetReference);
            if (_disposed || !_interopInitialized)
            {
                if (!_disposed && _initializationAttemptCount < ImmediateInteropAttemptLimit)
                {
                    await InvokeAsync(StateHasChanged);
                }
                return;
            }

            _initializationAttemptCount = 0;
            RequestSynchronization();
        }

        if (_synchronizationPending && _interop is not null)
        {
            var version = _synchronizationVersion;
            var restoreFocus = !Open && RestoreFocusOnEscape && _restoreFocusPending;
            _synchronizationAttemptCount++;
            var synchronized = await _interop.SetOpenAsync(
                _instanceId,
                Open,
                PlacementName,
                CloseOnOutsideInteraction,
                CloseOnEscape,
                restoreFocus);
            if (_disposed)
            {
                return;
            }

            if (!synchronized || version != _synchronizationVersion)
            {
                if (_synchronizationPending
                    && _synchronizationAttemptCount < ImmediateInteropAttemptLimit)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
            else
            {
                _synchronizationPending = false;
                _restoreFocusPending = false;
            }
        }
    }

    private Task ToggleAsync()
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        return OpenChanged.InvokeAsync(!Open);
    }

    /// <summary>Requests closure after a browser-owned outside or Escape interaction.</summary>
    [JSInvokable]
    public Task CloseFromBrowserAsync(bool restoreFocus = false)
    {
        if (_disposed || !Open)
        {
            return Task.CompletedTask;
        }

        return InvokeAsync(async () =>
        {
            if (_disposed || !Open)
            {
                return;
            }

            _restoreFocusPending = restoreFocus;
            await OpenChanged.InvokeAsync(false);
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
        Exception? disposalException = null;
        try
        {
            if (_interop is not null)
            {
                try
                {
                    await _interop.DisposeInstanceAsync(_instanceId);
                }
                catch (Exception exception)
                {
                    disposalException = exception;
                }

                try
                {
                    await _interop.DisposeAsync();
                }
                catch (Exception exception)
                {
                    disposalException ??= exception;
                }
            }
        }
        finally
        {
            try
            {
                _dotNetReference?.Dispose();
            }
            catch (Exception exception)
            {
                disposalException ??= exception;
            }

            _dotNetReference = null;
        }

        if (disposalException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disposalException).Throw();
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void RequestSynchronization()
    {
        _synchronizationVersion++;
        _synchronizationAttemptCount = 0;
        _synchronizationPending = true;
    }
}
