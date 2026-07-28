using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Renders a searchable, strongly typed multi-select integrated with EditContext.</summary>
public partial class BzsMultiSelect<TValue> : BzsInputBase<IReadOnlyList<TValue>>
{
    /// <summary>Gets or sets the read-only option collection.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<BzsSelectOption<TValue>> Options { get; set; } = [];

    /// <summary>Gets or sets whether the option panel includes search and bulk actions.</summary>
    [Parameter] public bool SearchEnabled { get; set; } = true;

    /// <summary>Gets or sets the text displayed when nothing is selected.</summary>
    [Parameter] public string? PlaceholderOption { get; set; }

    /// <summary>Gets or sets the search field placeholder and accessible name.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>Gets or sets the text shown when the current search has no matches.</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>Gets or sets the suffix used when selected labels do not fit the trigger.</summary>
    [Parameter] public string? SelectionSuffix { get; set; }

    /// <summary>Gets or sets the select-all action text.</summary>
    [Parameter] public string? SelectAllText { get; set; }

    /// <summary>Gets or sets the invert-selection action text.</summary>
    [Parameter] public string? InvertSelectionText { get; set; }

    /// <summary>Gets or sets the clear-selection action text.</summary>
    [Parameter] public string? ClearSelectionText { get; set; }

    private readonly string _instanceId = $"bzs-multi-select-{Guid.NewGuid():N}";
    private ElementReference _rootReference;
    private ElementReference _triggerReference;
    private ElementReference _searchReference;
    private DotNetObjectReference<BzsMultiSelect<TValue>>? _dotNetReference;
    private BzsSelectInterop? _interop;
    private bool _isOpen;
    private bool _isInteractive;
    private bool _interopInitialized;
    private bool _positionPending;
    private bool _focusSearchPending;
    private int _activeIndex = -1;
    private string _searchText = string.Empty;

    private string ListboxId => $"{InputId}-listbox";
    private string SearchId => $"{InputId}-search";
    private string ConstraintId => $"{InputId}-constraint";
    private string EffectiveSearchPlaceholder => string.IsNullOrWhiteSpace(SearchPlaceholder)
        ? Localize("SelectSearchPlaceholder")
        : SearchPlaceholder.Trim();
    private string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText)
        ? Localize("SelectNoMatches")
        : EmptyText.Trim();
    private string EffectiveSelectionSuffix => string.IsNullOrWhiteSpace(SelectionSuffix)
        ? Localize("MultiSelectSelectionSuffix")
        : SelectionSuffix.Trim();
    private string EffectiveSelectAllText => string.IsNullOrWhiteSpace(SelectAllText)
        ? Localize("MultiSelectSelectAll")
        : SelectAllText.Trim();
    private string EffectiveInvertSelectionText => string.IsNullOrWhiteSpace(InvertSelectionText)
        ? Localize("MultiSelectInvert")
        : InvertSelectionText.Trim();
    private string EffectiveClearSelectionText => string.IsNullOrWhiteSpace(ClearSelectionText)
        ? Localize("MultiSelectClear")
        : ClearSelectionText.Trim();
    private string? ActiveOptionId => _activeIndex >= 0 && _activeIndex < FilteredOptions.Count
        ? GetOptionId(FilteredOptions[_activeIndex])
        : null;
    private IReadOnlyList<TValue> SelectedValues => CurrentValue ?? [];
    private HashSet<TValue> SelectedSet => SelectedValues.ToHashSet(EqualityComparer<TValue>.Default);
    private IReadOnlyList<BzsSelectOption<TValue>> SelectedOptions => Options.Where(option => IsSelected(option.Value)).ToArray();
    private IReadOnlyList<BzsSelectOption<TValue>> FilteredOptions => string.IsNullOrWhiteSpace(_searchText)
        ? Options
        : Options.Where(MatchesSearch).ToArray();
    private bool HasEnabledFilteredOptions => FilteredOptions.Any(static option => !option.Disabled);
    private bool HasClearableFilteredSelection => FilteredOptions.Any(option => !option.Disabled && IsSelected(option.Value));
    private int NativeSize => Math.Clamp(Options.Count, 2, 6);
    private string SelectedText => SelectedOptions.Count switch
    {
        0 => string.IsNullOrWhiteSpace(PlaceholderOption) ? Localize("MultiSelectPlaceholder") : PlaceholderOption.Trim(),
        <= 2 => string.Join(", ", SelectedOptions.Select(static option => option.Label)),
        _ => $"{SelectedOptions.Count} {EffectiveSelectionSuffix}",
    };
    private string EffectiveAccessibleName => !string.IsNullOrWhiteSpace(Label) ? Label.Trim() : SelectedText;

    private IReadOnlyDictionary<string, object> TriggerAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-input bzs-multi-select__trigger", supportsReadOnly: false),
                StringComparer.OrdinalIgnoreCase)
            {
                ["type"] = "button",
                ["role"] = "combobox",
                ["aria-haspopup"] = "listbox",
                ["aria-expanded"] = _isOpen ? "true" : "false",
                ["aria-controls"] = ListboxId,
                ["aria-autocomplete"] = SearchEnabled ? "list" : "none",
            };

            attributes.Remove("name");
            attributes.Remove("placeholder");
            attributes.Remove("required");
            if (_activeIndex >= 0 && _activeIndex < FilteredOptions.Count)
            {
                attributes["aria-activedescendant"] = ActiveOptionId!;
            }
            if (string.IsNullOrWhiteSpace(Label)
                && !attributes.ContainsKey("aria-label")
                && !attributes.ContainsKey("aria-labelledby"))
            {
                attributes["aria-label"] = EffectiveAccessibleName;
            }

            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> NativeInputAttributes =>
        BuildInputAttributes("bzs-input bzs-multi-select__native", supportsReadOnly: false);

    private IReadOnlyDictionary<string, object> ConstraintInputAttributes => new Dictionary<string, object>
    {
        ["id"] = ConstraintId,
        ["class"] = "bzs-multi-select__constraint",
        ["required"] = "required",
        ["multiple"] = "multiple",
        ["tabindex"] = "-1",
        ["aria-hidden"] = "true",
        ["data-bzs-select-constraint"] = "true",
    };

    /// <inheritdoc />
    protected override string FormatValueAsString(IReadOnlyList<TValue>? value) => string.Join(",", (value ?? [])
        .Select(selected => Options.FirstOrDefault(option => EqualityComparer<TValue>.Default.Equals(option.Value, selected))?.ValueText)
        .Where(static valueText => valueText is not null));

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out IReadOnlyList<TValue> result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = [];
        validationErrorMessage = FormatValidationError("FormValidationSelection");
        return false;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ValidateOptions();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isInteractive)
        {
            _isInteractive = true;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (!_interopInitialized)
        {
            _interop ??= new BzsSelectInterop(JsRuntime);
            _dotNetReference ??= DotNetObjectReference.Create(this);
            _interopInitialized = await _interop.InitializeAsync(
                _instanceId,
                _rootReference,
                _dotNetReference);
            if (!_interopInitialized)
            {
                return;
            }
        }

        if (_positionPending && _interop is not null)
        {
            _positionPending = false;
            var focus = _focusSearchPending ? _searchReference : (ElementReference?)null;
            _focusSearchPending = false;
            await _interop.SetOpenAsync(_instanceId, _isOpen, focus);
        }
    }

    private bool MatchesSearch(BzsSelectOption<TValue> option) =>
        option.Label.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
        || option.Description?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true;

    private bool IsSelected(TValue value) => SelectedSet.Contains(value);

    private string GetOptionId(BzsSelectOption<TValue> option)
    {
        for (var index = 0; index < Options.Count; index++)
        {
            if (ReferenceEquals(Options[index], option) || Options[index].Equals(option))
            {
                return $"{InputId}-option-{index}";
            }
        }
        return $"{InputId}-option";
    }

    private string GetOptionClass(int index, bool selected, bool disabled) => string.Join(" ", new[]
    {
        "bzs-multi-select__option",
        selected ? "bzs-multi-select__option--selected" : null,
        index == _activeIndex ? "bzs-multi-select__option--active" : null,
        disabled ? "bzs-multi-select__option--disabled" : null,
    }.Where(static value => value is not null));

    private void Activate(int index)
    {
        if (index >= 0 && index < FilteredOptions.Count && !FilteredOptions[index].Disabled)
        {
            _activeIndex = index;
        }
    }

    private async Task ToggleAsync()
    {
        if (Disabled || ReadOnly) return;
        if (_isOpen) await CloseAsync(false); else Open();
    }

    private void Open()
    {
        _isOpen = true;
        _searchText = string.Empty;
        _activeIndex = FindFirstEnabledIndex();
        _positionPending = true;
        _focusSearchPending = SearchEnabled;
    }

    private async Task CloseAsync(bool restoreFocus)
    {
        if (!_isOpen) return;
        SetClosedState();
        if (_interop is not null)
        {
            await _interop.SetOpenAsync(_instanceId, false, restoreFocus ? _triggerReference : null);
        }
    }

    private void SetClosedState()
    {
        _isOpen = false;
        _searchText = string.Empty;
        _activeIndex = -1;
        _positionPending = false;
        _focusSearchPending = false;
    }

    private Task ToggleOptionAsync(BzsSelectOption<TValue> option)
    {
        if (Disabled || ReadOnly || option.Disabled) return Task.CompletedTask;

        var selected = SelectedSet;
        if (!selected.Add(option.Value)) selected.Remove(option.Value);
        SetSelection(selected);
        return Task.CompletedTask;
    }

    private Task SelectVisibleAsync()
    {
        var selected = SelectedSet;
        foreach (var option in FilteredOptions.Where(static option => !option.Disabled)) selected.Add(option.Value);
        SetSelection(selected);
        return Task.CompletedTask;
    }

    private Task InvertVisibleAsync()
    {
        var selected = SelectedSet;
        foreach (var option in FilteredOptions.Where(static option => !option.Disabled))
        {
            if (!selected.Add(option.Value)) selected.Remove(option.Value);
        }
        SetSelection(selected);
        return Task.CompletedTask;
    }

    private Task ClearAsync()
    {
        var selected = SelectedSet;
        foreach (var option in FilteredOptions.Where(static option => !option.Disabled))
        {
            selected.Remove(option.Value);
        }
        SetSelection(selected);
        return Task.CompletedTask;
    }

    private void SetSelection(HashSet<TValue> selected)
    {
        var ordered = Options.Where(option => selected.Contains(option.Value)).Select(static option => option.Value).ToList();
        ordered.AddRange(SelectedValues.Where(value => !Options.Any(option => EqualityComparer<TValue>.Default.Equals(option.Value, value)) && selected.Contains(value)));
        CurrentValue = ordered;
    }

    private void OnSearchInput(ChangeEventArgs args)
    {
        _searchText = args.Value?.ToString() ?? string.Empty;
        _activeIndex = FindFirstEnabledIndex();
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled || ReadOnly) return;

        switch (args.Key)
        {
            case "ArrowDown":
                if (!_isOpen) Open(); else MoveActive(1);
                break;
            case "ArrowUp":
                if (!_isOpen) Open(); else MoveActive(-1);
                break;
            case "Home" when _isOpen:
                _activeIndex = FindFirstEnabledIndex();
                break;
            case "End" when _isOpen:
                _activeIndex = FindLastEnabledIndex();
                break;
            case "Enter" when _isOpen:
                await ToggleActiveAsync();
                break;
            case " " when !_isOpen:
                Open();
                break;
            case " " when _isOpen && !SearchEnabled:
                await ToggleActiveAsync();
                break;
            case "Escape" when _isOpen:
                await CloseAsync(true);
                break;
        }
    }

    private Task ToggleActiveAsync()
    {
        var options = FilteredOptions;
        return _activeIndex >= 0 && _activeIndex < options.Count
            ? ToggleOptionAsync(options[_activeIndex])
            : Task.CompletedTask;
    }

    private void MoveActive(int delta)
    {
        var options = FilteredOptions;
        if (options.Count == 0) { _activeIndex = -1; return; }
        for (var offset = 1; offset <= options.Count; offset++)
        {
            var candidate = (_activeIndex + delta * offset + options.Count) % options.Count;
            if (!options[candidate].Disabled) { _activeIndex = candidate; return; }
        }
    }

    private int FindFirstEnabledIndex() => FilteredOptions.ToList().FindIndex(static option => !option.Disabled);

    private int FindLastEnabledIndex()
    {
        for (var index = FilteredOptions.Count - 1; index >= 0; index--)
        {
            if (!FilteredOptions[index].Disabled) return index;
        }
        return -1;
    }

    private void ValidateOptions()
    {
        if (Options is null) throw new InvalidOperationException("BzsMultiSelect requires an Options collection.");
        if (Options.GroupBy(static option => option.ValueText, StringComparer.Ordinal).Any(static group => group.Count() > 1))
        {
            throw new InvalidOperationException("BzsMultiSelect option ValueText values must be unique.");
        }
        if (Options.GroupBy(static option => option.Value, EqualityComparer<TValue>.Default).Any(static group => group.Count() > 1))
        {
            throw new InvalidOperationException("BzsMultiSelect option values must be unique.");
        }
    }

    /// <summary>Closes the option panel after an outside pointer interaction.</summary>
    [JSInvokable]
    public Task CloseFromBrowserAsync() => InvokeAsync(() =>
    {
        if (!_isOpen) return;

        SetClosedState();
        StateHasChanged();
    });

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_interop is not null)
        {
            await _interop.DisposeInstanceAsync(_instanceId);
            await _interop.DisposeAsync();
        }
        _dotNetReference?.Dispose();
    }
}
