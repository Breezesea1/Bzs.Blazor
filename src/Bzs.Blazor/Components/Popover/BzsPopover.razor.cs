namespace Bzs.Blazor;

/// <summary>Renders controlled content anchored to an owned native trigger.</summary>
public sealed partial class BzsPopover : BzsComponentBase, IAsyncDisposable
{
    private const int ImmediateInteropAttemptLimit = 2;
    private readonly string _panelId = $"bzs-popover-panel-{Guid.NewGuid():N}";
    private ElementReference _rootElement;
    private BzsAnchoredOverlaySession? _overlaySession;
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
    private string PlacementName => BzsAnchoredOverlaySession.GetPlacementName(Placement);

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

        GetOverlaySession().SetDesiredState(new BzsAnchoredOverlayState(
            Open,
            Placement,
            CloseOnOutsideInteraction,
            CloseOnEscape,
            RestoreFocusOnBrowserClose: RestoreFocusOnEscape));
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

    private Task ToggleAsync()
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        return OpenChanged.InvokeAsync(!Open);
    }

    /// <summary>Requests closure after a browser-owned outside or Escape interaction.</summary>
    public Task CloseFromBrowserAsync(bool restoreFocus = false)
    {
        if (_disposed || !Open)
        {
            return Task.CompletedTask;
        }

        return GetOverlaySession().CloseFromBrowserAsync(restoreFocus);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_overlaySession is not null)
        {
            await _overlaySession.DisposeAsync();
            _overlaySession = null;
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private BzsAnchoredOverlaySession GetOverlaySession() =>
        _overlaySession ??= new BzsAnchoredOverlaySession(
            JS,
            HandleCloseRequestedAsync,
            ImmediateInteropAttemptLimit,
            LoggerFactory);

    private Task HandleCloseRequestedAsync()
    {
        if (_disposed || !Open)
        {
            return Task.CompletedTask;
        }

        return InvokeAsync(async () =>
        {
            if (!_disposed && Open)
            {
                await OpenChanged.InvokeAsync(false);
            }
        });
    }
}
