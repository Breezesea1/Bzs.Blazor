using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Decorates a target region with a controlled pointer- or keyboard-invoked command menu.</summary>
public sealed partial class BzsContextMenu : BzsComponentBase, IBzsMenuOwner, IAsyncDisposable
{
    private const int ImmediateInteropAttemptLimit = 2;
    private const int TypeaheadResetMilliseconds = 700;
    private readonly string _targetId = $"bzs-context-menu-target-{Guid.NewGuid():N}";
    private readonly string _menuId = $"bzs-context-menu-list-{Guid.NewGuid():N}";
    private readonly BzsMenuState _menuState = new();
    private ElementReference _rootElement;
    private BzsAnchoredOverlaySession? _overlaySession;
    private bool _lastOpen;
    private bool _focusPending;
    private double? _clientX;
    private double? _clientY;
    private bool _invocationOpenRequestPending;
    private long _invocationOpenRequestVersion;
    private long _activeInvocationOpenRequestVersion;
    private long _closedInvocationOpenRequestVersion;
    private string _typeahead = string.Empty;
    private DateTimeOffset _lastTypeaheadAt;
    private bool _disposed;

    /// <summary>Gets or sets the controlled open state.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Gets or sets the callback used to request an open-state change.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Gets or sets whether context-menu invocation is unavailable.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets an accessible name for the target wrapper.</summary>
    [Parameter]
    public string? TargetAccessibleName { get; set; }

    /// <summary>Gets or sets the accessible name of the command menu.</summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets the decorated target region.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? TargetContent { get; set; }

    /// <summary>Gets or sets the composed menu items.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    internal Func<DateTimeOffset> GetUtcNow { get; set; } = static () => DateTimeOffset.UtcNow;

    private string? EffectiveTargetAccessibleName => Normalize(TargetAccessibleName);
    private string? EffectiveAccessibleName => Normalize(AccessibleName);
    private string? MenuLabelledBy => EffectiveAccessibleName is null ? _targetId : null;

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-context-menu"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-context-menu"] = "true",
                ["data-bzs-open"] = Open ? "true" : "false",
            };
            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (TargetContent is null || ChildContent is null)
        {
            throw new InvalidOperationException("BzsContextMenu requires TargetContent and ChildContent.");
        }

        if (!Open && _invocationOpenRequestPending)
        {
            if (_activeInvocationOpenRequestVersion == 0)
            {
                ClearInvocationPoint();
            }
            else
            {
                _closedInvocationOpenRequestVersion = _activeInvocationOpenRequestVersion;
            }
        }

        if (!_lastOpen && Open)
        {
            _focusPending = true;
            _invocationOpenRequestPending = false;
            _closedInvocationOpenRequestVersion = 0;
        }
        else if (_lastOpen && !Open)
        {
            _typeahead = string.Empty;
            _menuState.ClearFocus();
            ClearInvocationPoint();
        }

        _lastOpen = Open;
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
        if (_disposed)
        {
            return;
        }

        if (Open && _focusPending)
        {
            _focusPending = false;
            if (_menuState.SetBoundary(last: false) is { } item)
            {
                await RefreshItemsAsync();
                await item.FocusAsync();
            }
        }
    }

    private async Task HandleContextMenuAsync(MouseEventArgs args)
    {
        if (Disabled)
        {
            return;
        }

        _clientX = args.ClientX;
        _clientY = args.ClientY;
        _focusPending = true;
        if (Open)
        {
            _invocationOpenRequestPending = false;
            UpdateOverlayState();
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            var requestVersion = ++_invocationOpenRequestVersion;
            _invocationOpenRequestPending = true;
            _activeInvocationOpenRequestVersion = requestVersion;
            try
            {
                await OpenChanged.InvokeAsync(true);
            }
            finally
            {
                if (_activeInvocationOpenRequestVersion == requestVersion)
                {
                    _activeInvocationOpenRequestVersion = 0;
                    if (_invocationOpenRequestPending
                        && _closedInvocationOpenRequestVersion == requestVersion
                        && !Open)
                    {
                        ClearInvocationPoint();
                    }
                }
            }
        }
    }

    void IBzsMenuOwner.RegisterOrUpdate(BzsMenuItem item) => _menuState.RegisterOrUpdate(item);

    void IBzsMenuOwner.Unregister(BzsMenuItem item) => _menuState.Unregister(item);

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

    private void ClearInvocationPoint()
    {
        _clientX = null;
        _clientY = null;
        _invocationOpenRequestPending = false;
        _closedInvocationOpenRequestVersion = 0;
    }

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

    private void UpdateOverlayState() =>
        GetOverlaySession().SetDesiredState(new BzsAnchoredOverlayState(
            Open,
            BzsPopoverPlacement.BottomStart,
            CloseOnOutsideInteraction: true,
            CloseOnEscape: true,
            _clientX is { } clientX && _clientY is { } clientY
                ? new BzsAnchoredOverlayInvocationPoint(clientX, clientY)
                : null));
}
