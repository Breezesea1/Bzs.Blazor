namespace Bzs.Blazor;

/// <summary>
/// Renders controlled application navigation that can be docked or overlaid by CSS.
/// </summary>
public sealed partial class BzsNavigationDrawer : BzsComponentBase
{
    private readonly string _overlayId = $"bzs-navigation-drawer-{Guid.NewGuid():N}";
    private BzsOverlayInterop? _interop;
    private ElementReference _rootElement;
    private ElementReference _panelElement;
    private bool _interopSynchronizationPending = true;
    private bool _lastOpen;
    private BzsNavigationDrawerVariant _lastVariant;
    private string? _lastInitialFocusSelector;
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
    /// Gets or sets the navigation content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

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

        var initialFocusSelector = Normalize(InitialFocusSelector);
        if (_lastOpen != Open
            || _lastVariant != Variant
            || _lastInitialFocusSelector != initialFocusSelector)
        {
            _interopSynchronizationPending = true;
        }

        _lastOpen = Open;
        _lastVariant = Variant;
        _lastInitialFocusSelector = initialFocusSelector;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || (!_interopSynchronizationPending && !firstRender))
        {
            return;
        }

        _interopSynchronizationPending = false;
        if (Open)
        {
            _interop ??= new BzsOverlayInterop(JS);
            await _interop.ActivateNavigationDrawerAsync(
                _overlayId,
                _rootElement,
                _panelElement,
                _lastInitialFocusSelector);
        }
        else if (_interop is not null)
        {
            await _interop.DeactivateAsync(_overlayId);
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
        return Open && CloseOnEscape && string.Equals(eventArgs.Key, "Escape", StringComparison.Ordinal)
            ? OpenChanged.InvokeAsync(false)
            : Task.CompletedTask;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_interop is not null)
        {
            await _interop.DisposeAsync(_overlayId);
        }
    }
}
