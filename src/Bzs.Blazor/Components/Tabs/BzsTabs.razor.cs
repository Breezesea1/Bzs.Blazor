using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>
/// Selects the keyboard axis used by <see cref="BzsTabs" />.
/// </summary>
public enum BzsTabsOrientation
{
    /// <summary>Arranges tabs along the inline axis.</summary>
    Horizontal,

    /// <summary>Arranges tabs along the block axis.</summary>
    Vertical,
}

/// <summary>
/// Selects when keyboard focus changes the active tab.
/// </summary>
public enum BzsTabActivationMode
{
    /// <summary>Activates an enabled tab as soon as focus moves to it.</summary>
    Automatic,

    /// <summary>Requires Enter or Space to activate the focused tab.</summary>
    Manual,
}

/// <summary>
/// Renders composed tab items with controlled or internally initialized selection.
/// </summary>
public partial class BzsTabs : BzsComponentBase, IAsyncDisposable
{
    private readonly List<BzsTabItem> _items = [];
    private readonly Dictionary<BzsTabItem, TabItemState> _itemStates = [];
    private string? _internalActiveValue;
    private BzsTabItem? _focusedItem;
    private bool _hasActiveValueParameter;
    private bool _selectionInitialized;
    private bool _initialValueResolved;
    private ElementReference _rootElement;
    private ElementReference _tabListElement;
    private IJSObjectReference? _module;
    private bool _interopPending = true;
    private bool _disposed;
    private BzsTabsOrientation _lastOrientation;
    private BzsTabActivationMode _lastActivationMode;

    /// <summary>
    /// Gets or sets the consumer-controlled active tab value. Supplying this parameter,
    /// including a <see langword="null" /> value, makes the component controlled.
    /// </summary>
    [Parameter]
    public string? ActiveValue { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the component requests an active tab change.
    /// </summary>
    [Parameter]
    public EventCallback<string> ActiveValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the initial active tab value when <see cref="ActiveValue" /> is not supplied.
    /// An enabled first item is used when this value is not supplied.
    /// </summary>
    [Parameter]
    public string? InitialActiveValue { get; set; }

    /// <summary>
    /// Gets or sets the layout and arrow-key axis for the tab list.
    /// </summary>
    [Parameter]
    public BzsTabsOrientation Orientation { get; set; } = BzsTabsOrientation.Horizontal;

    /// <summary>
    /// Gets or sets whether keyboard focus automatically selects a tab.
    /// </summary>
    [Parameter]
    public BzsTabActivationMode ActivationMode { get; set; } = BzsTabActivationMode.Automatic;

    /// <summary>Gets or sets the accessible name of the tab list.</summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets the ID of an element that labels the tab list.</summary>
    [Parameter]
    public string? LabelledBy { get; set; }

    /// <summary>
    /// Gets or sets the declarative tab items consumed by this component.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private IReadOnlyList<BzsTabItem> Items => _items;

    private string OrientationName => Orientation switch
    {
        BzsTabsOrientation.Horizontal => "horizontal",
        BzsTabsOrientation.Vertical => "vertical",
        _ => throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "The tabs orientation is not supported."),
    };

    private string ActivationName => ActivationMode switch
    {
        BzsTabActivationMode.Automatic => "automatic",
        BzsTabActivationMode.Manual => "manual",
        _ => throw new ArgumentOutOfRangeException(nameof(ActivationMode), ActivationMode, "The tabs activation mode is not supported."),
    };

    private string DirectionName => GetDirection() ?? "inherit";

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-tabs"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-tabs"] = OrientationName,
                ["data-bzs-tabs-orientation"] = OrientationName,
                ["data-bzs-tabs-activation"] = ActivationName,
                ["data-bzs-tabs-direction"] = DirectionName,
            };

            attributes.Remove("aria-label");
            attributes.Remove("aria-labelledby");

            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> TabListAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["class"] = "bzs-tabs__tablist",
                ["role"] = "tablist",
                ["aria-orientation"] = OrientationName,
            };

            var accessibleName = FirstNonEmpty(AccessibleName, GetAdditionalAttribute("aria-label"));
            var labelledBy = FirstNonEmpty(LabelledBy, GetAdditionalAttribute("aria-labelledby"));
            if (labelledBy is not null)
            {
                attributes["aria-labelledby"] = labelledBy;
            }
            else if (accessibleName is not null)
            {
                attributes["aria-label"] = accessibleName;
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _hasActiveValueParameter = parameters.TryGetValue<string?>(nameof(ActiveValue), out _);
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "The tabs orientation is not supported.");
        }

        if (!Enum.IsDefined(ActivationMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ActivationMode), ActivationMode, "The tabs activation mode is not supported.");
        }

        if (_hasActiveValueParameter && ActiveValue is not null && string.IsNullOrWhiteSpace(ActiveValue))
        {
            throw new ArgumentException("ActiveValue must be non-empty when supplied.", nameof(ActiveValue));
        }

        if (InitialActiveValue is not null && string.IsNullOrWhiteSpace(InitialActiveValue))
        {
            throw new ArgumentException("InitialActiveValue must be non-empty when supplied.", nameof(InitialActiveValue));
        }

        if (_lastOrientation != Orientation || _lastActivationMode != ActivationMode)
        {
            _lastOrientation = Orientation;
            _lastActivationMode = ActivationMode;
            _interopPending = true;
        }

        SynchronizeSelection();
    }

    internal void RegisterOrUpdate(BzsTabItem item)
    {
        var nextState = TabItemState.Create(item);
        if (_itemStates.TryGetValue(item, out var currentState) && currentState == nextState)
        {
            return;
        }

        ValidateItemState(item, nextState);
        if (!_itemStates.ContainsKey(item))
        {
            _items.Add(item);
        }

        _itemStates[item] = nextState;
        SynchronizeSelection();
        StateHasChanged();
    }

    internal void Unregister(BzsTabItem item)
    {
        if (!_itemStates.Remove(item))
        {
            return;
        }

        _items.Remove(item);
        if (ReferenceEquals(_focusedItem, item))
        {
            _focusedItem = null;
        }

        SynchronizeSelection();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || (!_interopPending && !firstRender))
        {
            return;
        }

        _interopPending = false;
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("attach", _tabListElement, OrientationName, ActivationName);
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    private bool IsSelected(BzsTabItem item) =>
        !item.Disabled
        && string.Equals(GetSelectedItem()?.EffectiveValue, item.EffectiveValue, StringComparison.Ordinal);

    private bool IsFocusable(BzsTabItem item) =>
        !item.Disabled && ReferenceEquals(GetFocusableItem(), item);

    private BzsTabItem? GetSelectedItem()
    {
        var selectedValue = _hasActiveValueParameter ? ActiveValue : _internalActiveValue;
        if (string.IsNullOrEmpty(selectedValue))
        {
            return null;
        }

        return _items.FirstOrDefault(item =>
            !item.Disabled
            && string.Equals(item.EffectiveValue, selectedValue, StringComparison.Ordinal));
    }

    private BzsTabItem? GetFocusableItem() =>
        _focusedItem is { Disabled: false } && _items.Contains(_focusedItem)
            ? _focusedItem
            : GetSelectedItem();

    private async Task HandleTabClickAsync(BzsTabItem item, MouseEventArgs _)
    {
        if (item.Disabled)
        {
            return;
        }

        _focusedItem = item;
        await RequestSelectionAsync(item);
    }

    private async Task HandleTabKeyDownAsync(BzsTabItem currentItem, KeyboardEventArgs eventArgs)
    {
        var target = await GetNavigationTargetAsync(currentItem, eventArgs.Key);
        if (target is not null)
        {
            _focusedItem = target;
            if (ActivationMode == BzsTabActivationMode.Automatic)
            {
                await RequestSelectionAsync(target);
            }
            else
            {
                StateHasChanged();
            }

            await target.FocusAsync();
            return;
        }

        if (ActivationMode == BzsTabActivationMode.Manual && IsActivationKey(eventArgs))
        {
            await RequestSelectionAsync(currentItem);
        }
    }

    private async Task RequestSelectionAsync(BzsTabItem item)
    {
        if (item.Disabled)
        {
            return;
        }

        var wasSelected = IsSelected(item);
        _focusedItem = item;
        if (!_hasActiveValueParameter)
        {
            _internalActiveValue = item.EffectiveValue;
            _selectionInitialized = true;
            _initialValueResolved = true;
        }

        if (!wasSelected)
        {
            await ActiveValueChanged.InvokeAsync(item.EffectiveValue);
        }

        StateHasChanged();
    }

    private async ValueTask<BzsTabItem?> GetNavigationTargetAsync(BzsTabItem currentItem, string? key)
    {
        var enabledItems = _items.Where(static item => !item.Disabled).ToList();
        if (enabledItems.Count == 0)
        {
            return null;
        }

        if (string.Equals(key, "Home", StringComparison.Ordinal))
        {
            return enabledItems[0];
        }

        if (string.Equals(key, "End", StringComparison.Ordinal))
        {
            return enabledItems[^1];
        }

        var offset = await GetArrowOffsetAsync(key);
        if (offset == 0)
        {
            return null;
        }

        var currentIndex = enabledItems.IndexOf(currentItem);
        if (currentIndex < 0)
        {
            return offset > 0 ? enabledItems[0] : enabledItems[^1];
        }

        return enabledItems[(currentIndex + offset + enabledItems.Count) % enabledItems.Count];
    }

    private async ValueTask<int> GetArrowOffsetAsync(string? key)
    {
        if (Orientation == BzsTabsOrientation.Vertical)
        {
            return key switch
            {
                "ArrowUp" => -1,
                "ArrowDown" => 1,
                _ => 0,
            };
        }

        var isRightToLeft = await IsRightToLeftAsync();
        return key switch
        {
            "ArrowLeft" => isRightToLeft ? 1 : -1,
            "ArrowRight" => isRightToLeft ? -1 : 1,
            _ => 0,
        };
    }

    private static bool IsActivationKey(KeyboardEventArgs eventArgs) =>
        string.Equals(eventArgs.Key, "Enter", StringComparison.Ordinal)
        || string.Equals(eventArgs.Key, " ", StringComparison.Ordinal)
        || string.Equals(eventArgs.Code, "Space", StringComparison.Ordinal);

    private void SynchronizeSelection()
    {
        var enabledItems = _items.Where(static item => !item.Disabled).ToList();
        if (enabledItems.Count == 0)
        {
            _focusedItem = null;
            if (!_hasActiveValueParameter)
            {
                _internalActiveValue = null;
            }

            return;
        }

        if (!_hasActiveValueParameter)
        {
            var selectedItem = GetSelectedItem();
            var initialItem = FindEnabledItem(InitialActiveValue);
            if (!_initialValueResolved && initialItem is not null)
            {
                selectedItem = initialItem;
                _internalActiveValue = initialItem.EffectiveValue;
                _selectionInitialized = true;
                _initialValueResolved = true;
            }

            if (!_selectionInitialized || selectedItem is null)
            {
                selectedItem = initialItem ?? enabledItems[0];
                _internalActiveValue = selectedItem.EffectiveValue;
                _selectionInitialized = true;
                _initialValueResolved = InitialActiveValue is null || initialItem is not null;
            }

            if (_focusedItem is null || _focusedItem.Disabled || !_items.Contains(_focusedItem))
            {
                _focusedItem = selectedItem;
            }

            return;
        }

        var controlledItem = GetSelectedItem();
        if (_focusedItem is null || _focusedItem.Disabled || !_items.Contains(_focusedItem)
            || ActivationMode == BzsTabActivationMode.Automatic)
        {
            _focusedItem = controlledItem ?? enabledItems[0];
        }
    }

    private BzsTabItem? FindEnabledItem(string? value) =>
        string.IsNullOrEmpty(value)
            ? null
            : _items.FirstOrDefault(item =>
                !item.Disabled
                && string.Equals(item.EffectiveValue, value, StringComparison.Ordinal));

    private void ValidateItemState(BzsTabItem item, TabItemState candidate)
    {
        foreach (var existing in _items)
        {
            if (ReferenceEquals(existing, item))
            {
                continue;
            }

            if (string.Equals(existing.EffectiveValue, candidate.Value, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"BzsTabs requires each BzsTabItem Value to be unique. The value '{candidate.Value}' appears more than once.");
            }

            if (string.Equals(existing.TabId, candidate.TabId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"BzsTabs requires each BzsTabItem Id to be unique. The id '{candidate.TabId}' appears more than once.");
            }

            if (string.Equals(existing.TabId, candidate.PanelId, StringComparison.Ordinal)
                || string.Equals(existing.PanelId, candidate.TabId, StringComparison.Ordinal)
                || string.Equals(existing.PanelId, candidate.PanelId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "BzsTabs requires tab and panel IDs to be unique across the entire tab set.");
            }
        }
    }

    private string? GetDirection()
    {
        if (AdditionalAttributes is null)
        {
            return null;
        }

        foreach (var attribute in AdditionalAttributes)
        {
            if (!attribute.Key.Equals("dir", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var direction = attribute.Value?.ToString()?.Trim().ToLowerInvariant();
            return direction is "ltr" or "rtl" or "auto" ? direction : null;
        }

        return null;
    }

    private async ValueTask<bool> IsRightToLeftAsync()
    {
        var explicitDirection = GetDirection();
        if (explicitDirection is "ltr" or "rtl")
        {
            return explicitDirection == "rtl";
        }

        try
        {
            var module = await GetModuleAsync();
            return string.Equals(
                await module.InvokeAsync<string>("getDirection", _rootElement),
                "rtl",
                StringComparison.Ordinal);
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
            return false;
        }
    }

    private ValueTask<IJSObjectReference> GetModuleAsync() =>
        _module is not null
            ? ValueTask.FromResult(_module)
            : LoadModuleAsync();

    private async ValueTask<IJSObjectReference> LoadModuleAsync() =>
        _module = await JS.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/Bzs.Blazor/Components/Tabs/BzsTabs.razor.js");

    private string? GetAdditionalAttribute(string name)
    {
        if (AdditionalAttributes is null)
        {
            return null;
        }

        foreach (var attribute in AdditionalAttributes)
        {
            if (attribute.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(attribute.Value?.ToString())
                    ? null
                    : attribute.Value.ToString()!.Trim();
            }
        }

        return null;
    }

    private static string? FirstNonEmpty(string? primary, string? fallback) =>
        !string.IsNullOrWhiteSpace(primary)
            ? primary.Trim()
            : string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();

    private static bool IsTransientInteropFailure(Exception exception) =>
        exception is JSDisconnectedException or TaskCanceledException or InvalidOperationException;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("detach", _tabListElement);
            await _module.DisposeAsync();
        }
        catch (Exception exception) when (IsTransientInteropFailure(exception))
        {
        }
    }

    private sealed record TabItemState(
        string Value,
        string Title,
        bool Disabled,
        string TabId,
        string PanelId,
        string? Id,
        string? CssClass,
        string? Style,
        string? AttributesFingerprint)
    {
        public static TabItemState Create(BzsTabItem item) => new(
            item.EffectiveValue,
            item.EffectiveTitle,
            item.Disabled,
            item.TabId,
            item.PanelId,
            item.Id,
            item.Class,
            item.Style,
            CreateAttributesFingerprint(item.AdditionalAttributes));

        private static string? CreateAttributesFingerprint(IReadOnlyDictionary<string, object>? attributes) =>
            attributes is null
                ? null
                : string.Join(
                    "\u001f",
                    attributes
                        .OrderBy(static attribute => attribute.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(static attribute => $"{attribute.Key}={attribute.Value}"));
    }
}
