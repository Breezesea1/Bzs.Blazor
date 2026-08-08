using Bzs.Blazor.Localization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor;

/// <summary>
/// Selects the logical edge from which a controlled drawer is displayed.
/// </summary>
public enum BzsDrawerPlacement
{
    /// <summary>Displays the drawer at the logical inline start edge.</summary>
    Start,

    /// <summary>Displays the drawer at the logical inline end edge.</summary>
    End,

    /// <summary>Displays the drawer at the block start edge.</summary>
    Top,

    /// <summary>Displays the drawer at the block end edge.</summary>
    Bottom,
}

/// <summary>
/// Renders a controlled, declarative drawer without command-service support.
/// </summary>
public sealed partial class BzsDrawer : BzsComponentBase, IAsyncDisposable
{
    private readonly string _overlayId = $"bzs-drawer-{Guid.NewGuid():N}";
    private readonly string _titleId = $"bzs-drawer-title-{Guid.NewGuid():N}";
    private BzsOverlayInterop? _interop;
    private ElementReference _panelElement;
    private bool _isOpen;
    private bool _interopSynchronizationPending = true;
    private bool _lastModal;
    private string? _lastInitialFocusSelector;
    private bool _disposed;

    [Inject]
    private IStringLocalizer<BzsBlazorResources> Localizer { get; set; } = default!;

    /// <summary>Gets or sets whether the drawer is open.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Gets or sets the callback used to request an open-state change.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Gets or sets the optional visible title.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Gets or sets the accessible name when no visible title is available.</summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets whether the drawer blocks its background.</summary>
    [Parameter]
    public bool Modal { get; set; } = true;

    /// <summary>Gets or sets the logical placement of the drawer.</summary>
    [Parameter]
    public BzsDrawerPlacement Placement { get; set; } = BzsDrawerPlacement.End;

    /// <summary>Gets or sets whether Escape requests dismissal.</summary>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>Gets or sets whether a backdrop interaction requests dismissal.</summary>
    [Parameter]
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>Gets or sets whether a close control is rendered.</summary>
    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Gets or sets the selector used for initial interactive focus.</summary>
    [Parameter]
    public string? InitialFocusSelector { get; set; }

    /// <summary>Gets or sets the content displayed in the drawer body.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the optional content displayed in the drawer footer.</summary>
    [Parameter]
    public RenderFragment? FooterContent { get; set; }

    /// <summary>Gets or sets the callback raised after the drawer requests dismissal.</summary>
    [Parameter]
    public EventCallback<BzsDialogDismissReason> Dismissed { get; set; }

    private bool HasTitle => !string.IsNullOrWhiteSpace(Title);

    private string EffectiveTitle => Title!.Trim();

    private string EffectiveAccessibleName => !string.IsNullOrWhiteSpace(AccessibleName)
        ? AccessibleName.Trim()
        : Localizer["DrawerLabel"].Value;

    private string EffectiveCloseLabel => Localizer["CloseDrawer"].Value;

    private string PlacementName => Placement switch
    {
        BzsDrawerPlacement.Start => "start",
        BzsDrawerPlacement.End => "end",
        BzsDrawerPlacement.Top => "top",
        BzsDrawerPlacement.Bottom => "bottom",
        _ => throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "The drawer placement is not supported."),
    };

    private IReadOnlyDictionary<string, object> PanelAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-drawer__panel bzs-drawer__panel--{PlacementName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "dialog",
                ["tabindex"] = "-1",
                ["data-bzs-drawer"] = PlacementName,
            };

            attributes.Remove("aria-labelledby");
            attributes.Remove("aria-label");
            attributes.Remove("aria-modal");
            if (HasTitle)
            {
                attributes["aria-labelledby"] = _titleId;
            }
            else
            {
                attributes["aria-label"] = EffectiveAccessibleName;
            }

            if (Modal)
            {
                attributes["aria-modal"] = "true";
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "The drawer placement is not supported.");
        }

        var selector = Normalize(InitialFocusSelector);
        if (_isOpen != Open || _lastModal != Modal || _lastInitialFocusSelector != selector)
        {
            _interopSynchronizationPending = true;
        }

        _isOpen = Open;
        _lastModal = Modal;
        _lastInitialFocusSelector = selector;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || (!_interopSynchronizationPending && !firstRender))
        {
            return;
        }

        _interopSynchronizationPending = false;
        if (_isOpen)
        {
            _interop ??= new BzsOverlayInterop(JS, LoggerFactory);
            await _interop.ActivateAsync(_overlayId, _panelElement, Modal, _lastInitialFocusSelector);
        }
        else if (_interop is not null)
        {
            await _interop.DeactivateAsync(_overlayId);
        }
    }

    private Task RequestCloseAsync(MouseEventArgs _) =>
        RequestDismissAsync(BzsDialogDismissReason.CloseButton);

    private Task RequestBackdropDismissAsync()
    {
        return Modal && CloseOnBackdropClick
            ? RequestDismissAsync(BzsDialogDismissReason.Backdrop)
            : Task.CompletedTask;
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs eventArgs)
    {
        return CloseOnEscape && string.Equals(eventArgs.Key, "Escape", StringComparison.Ordinal)
            ? RequestDismissAsync(BzsDialogDismissReason.Escape)
            : Task.CompletedTask;
    }

    private async Task RequestDismissAsync(BzsDialogDismissReason reason)
    {
        await OpenChanged.InvokeAsync(false);
        await Dismissed.InvokeAsync(reason);
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
