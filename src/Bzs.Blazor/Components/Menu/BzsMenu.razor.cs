using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Renders a controlled button-triggered command menu.</summary>
public sealed partial class BzsMenu : BzsComponentBase, IBzsMenuOwner, IAsyncDisposable
{
    private const int ImmediateInteropAttemptLimit = 2;
    private const int TypeaheadResetMilliseconds = 700;
    private readonly string _triggerId = $"bzs-menu-trigger-{Guid.NewGuid():N}";
    private readonly string _menuId = $"bzs-menu-list-{Guid.NewGuid():N}";
    private readonly BzsMenuState _menuState = new();
    private ElementReference _rootElement;
    private BzsAnchoredOverlaySession? _overlaySession;
    private bool _lastOpen;
    private bool _focusPending;
    private bool _focusFromEnd;
    private string _typeahead = string.Empty;
    private DateTimeOffset _lastTypeaheadAt;
    private bool _disposed;

    /// <summary>Gets or sets the controlled open state.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Gets or sets the callback used to request an open-state change.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Gets or sets the preferred logical menu placement.</summary>
    [Parameter]
    public BzsPopoverPlacement Placement { get; set; } = BzsPopoverPlacement.BottomStart;

    /// <summary>Gets or sets whether the trigger is unavailable.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets an accessible name for an icon-only trigger.</summary>
    [Parameter]
    public string? TriggerAccessibleName { get; set; }

    /// <summary>Gets or sets the accessible name of the menu.</summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets content rendered inside the native trigger.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? TriggerContent { get; set; }

    /// <summary>Gets or sets the composed menu items.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    internal Func<DateTimeOffset> GetUtcNow { get; set; } = static () => DateTimeOffset.UtcNow;

    private string? EffectiveTriggerAccessibleName => Normalize(TriggerAccessibleName);
    private string? EffectiveAccessibleName => Normalize(AccessibleName);
    private string? MenuLabelledBy => EffectiveAccessibleName is null ? _triggerId : null;
    private string PlacementName => BzsAnchoredOverlaySession.GetPlacementName(Placement);

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-menu"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-menu"] = "true",
                ["data-bzs-open"] = Open ? "true" : "false",
            };
            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (TriggerContent is null || ChildContent is null)
        {
            throw new InvalidOperationException("BzsMenu requires TriggerContent and ChildContent.");
        }

        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "The menu placement is not supported.");
        }

        if (!_lastOpen && Open)
        {
            _focusPending = true;
        }
        else if (_lastOpen && !Open)
        {
            _typeahead = string.Empty;
            _menuState.ClearFocus();
        }

        _lastOpen = Open;
        GetOverlaySession().SetDesiredState(new BzsAnchoredOverlayState(
            Open,
            Placement,
            CloseOnOutsideInteraction: true,
            CloseOnEscape: true));
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        await GetOverlaySession().AfterRenderAsync(_rootElement);
        if (_disposed)
        {
            return;
        }

        if (Open && _focusPending)
        {
            _focusPending = false;
            var focusFromEnd = _focusFromEnd;
            _focusFromEnd = false;
            if (_menuState.SetBoundary(focusFromEnd) is { } item)
            {
                await RefreshItemsAsync();
                await item.FocusAsync();
            }
        }
    }

    private Task ToggleAsync()
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }
        _focusFromEnd = false;
        return OpenChanged.InvokeAsync(!Open);
    }

    private Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled || Open)
        {
            return Task.CompletedTask;
        }

        if (args.Key is "ArrowDown" or "ArrowUp")
        {
            _focusFromEnd = args.Key == "ArrowUp";
            return OpenChanged.InvokeAsync(true);
        }
        return Task.CompletedTask;
    }

    void IBzsMenuOwner.RegisterOrUpdate(BzsMenuItem item)
    {
        _menuState.RegisterOrUpdate(item);
    }

    void IBzsMenuOwner.Unregister(BzsMenuItem item)
    {
        _menuState.Unregister(item);
    }

    int IBzsMenuOwner.GetTabIndex(BzsMenuItem item) => _menuState.GetTabIndex(item);

    async Task IBzsMenuOwner.ActivateItemAsync(BzsMenuItem item)
    {
        if (!Open || item.Disabled || item.Separator)
        {
            return;
        }

        await item.InvokeCommandAsync();
        await RequestCloseAsync(restoreFocus: true);
    }

    async Task IBzsMenuOwner.HandleItemKeyDownAsync(BzsMenuItem item, KeyboardEventArgs args)
    {
        if (!Open)
        {
            return;
        }

        BzsMenuItem? target = args.Key switch
        {
            "ArrowDown" => _menuState.Move(item, 1),
            "ArrowUp" => _menuState.Move(item, -1),
            "Home" => _menuState.SetBoundary(last: false),
            "End" => _menuState.SetBoundary(last: true),
            _ => null,
        };
        if (target is not null)
        {
            await RefreshItemsAsync();
            await target.FocusAsync();
            return;
        }

        if (args.Key == "Tab")
        {
            await RequestCloseAsync(restoreFocus: false);
        }
        else if (args.Key?.Length == 1 && !char.IsControl(args.Key[0]))
        {
            var now = GetUtcNow();
            _typeahead = now - _lastTypeaheadAt > TimeSpan.FromMilliseconds(TypeaheadResetMilliseconds)
                ? args.Key
                : _typeahead + args.Key;
            _lastTypeaheadAt = now;
            if (_menuState.FindTypeahead(item, _typeahead) is { } match)
            {
                await RefreshItemsAsync();
                await match.FocusAsync();
            }
        }
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

    private Task RefreshItemsAsync() =>
        Task.WhenAll(_menuState.Items.Select(static item => item.RefreshAsync()));

    private Task RequestCloseAsync(bool restoreFocus)
    {
        if (_disposed || !Open)
        {
            return Task.CompletedTask;
        }

        return GetOverlaySession().RequestCloseAsync(restoreFocus);
    }

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

    private BzsAnchoredOverlaySession GetOverlaySession() =>
        _overlaySession ??= new BzsAnchoredOverlaySession(
            JS,
            HandleCloseRequestedAsync,
            ImmediateInteropAttemptLimit,
            LoggerFactory);
}
