using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Renders a searchable, strongly typed combobox integrated with EditContext.</summary>
public sealed partial class BzsSelect<TValue> : BzsInputBase<TValue>
{
    /// <summary>Gets or sets the read-only option collection.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<BzsSelectOption<TValue>> Options { get; set; } = [];

    /// <summary>Gets or sets the optional placeholder shown when no option is selected.</summary>
    [Parameter] public string? PlaceholderOption { get; set; }

    /// <summary>Gets or sets whether the option panel includes a search field.</summary>
    [Parameter] public bool SearchEnabled { get; set; } = true;

    /// <summary>Gets or sets the search field placeholder and accessible name.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>Gets or sets the text shown when the current search has no matches.</summary>
    [Parameter] public string? EmptyText { get; set; }

    private readonly string _instanceId = $"bzs-select-{Guid.NewGuid():N}";
    private ElementReference _rootReference;
    private ElementReference _triggerReference;
    private ElementReference _searchReference;
    private DotNetObjectReference<BzsSelect<TValue>>? _dotNetReference;
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
    private string? ActiveOptionId => _activeIndex >= 0 && _activeIndex < FilteredOptions.Count
        ? GetOptionId(FilteredOptions[_activeIndex])
        : null;
    private BzsSelectOption<TValue>? SelectedOption => Options.FirstOrDefault(option => IsSelected(option.Value));
    private string SelectedText => SelectedOption?.Label
        ?? (string.IsNullOrWhiteSpace(PlaceholderOption) ? Localize("SelectPlaceholder") : PlaceholderOption.Trim());
    private string EffectiveAccessibleName => !string.IsNullOrWhiteSpace(Label) ? Label.Trim() : SelectedText;
    private IReadOnlyList<BzsSelectOption<TValue>> FilteredOptions => string.IsNullOrWhiteSpace(_searchText)
        ? Options
        : Options.Where(MatchesSearch).ToArray();

    private IReadOnlyDictionary<string, object> TriggerAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-input bzs-select__trigger", supportsReadOnly: false),
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
                && !HasAdditionalAccessibleName(attributes))
            {
                attributes["aria-label"] = EffectiveAccessibleName;
            }

            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> NativeInputAttributes =>
        BuildInputAttributes("bzs-input bzs-select__native", supportsReadOnly: false);

    private IReadOnlyDictionary<string, object> ConstraintInputAttributes => new Dictionary<string, object>
    {
        ["id"] = ConstraintId,
        ["class"] = "bzs-select__constraint",
        ["required"] = "required",
        ["tabindex"] = "-1",
        ["aria-hidden"] = "true",
        ["data-bzs-select-constraint"] = "true",
    };

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value) => Options
        .FirstOrDefault(option => EqualityComparer<TValue>.Default.Equals(option.Value, value))
        ?.ValueText;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrEmpty(value) && default(TValue) is null)
        {
            result = default!;
            validationErrorMessage = null;
            return true;
        }

        var option = Options.FirstOrDefault(candidate =>
            string.Equals(candidate.ValueText, value, StringComparison.Ordinal));
        if (option is not null)
        {
            result = option.Value;
            validationErrorMessage = null;
            return true;
        }

        result = default!;
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

    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

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
        "bzs-select__option",
        selected ? "bzs-select__option--selected" : null,
        index == _activeIndex ? "bzs-select__option--active" : null,
        disabled ? "bzs-select__option--disabled" : null,
    }.Where(static value => value is not null));

    private static bool HasAdditionalAccessibleName(IReadOnlyDictionary<string, object> attributes) =>
        attributes.ContainsKey("aria-label") || attributes.ContainsKey("aria-labelledby");

    private void Activate(int index)
    {
        if (index >= 0 && index < FilteredOptions.Count && !FilteredOptions[index].Disabled)
        {
            _activeIndex = index;
        }
    }

    private async Task ToggleAsync()
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        if (_isOpen)
        {
            await CloseAsync(false);
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        _isOpen = true;
        _searchText = string.Empty;
        _activeIndex = FindInitialActiveIndex();
        _positionPending = true;
        _focusSearchPending = SearchEnabled;
    }

    private async Task CloseAsync(bool restoreFocus)
    {
        if (!_isOpen)
        {
            return;
        }

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

    private async Task SelectAsync(BzsSelectOption<TValue> option)
    {
        if (Disabled || ReadOnly || option.Disabled)
        {
            return;
        }

        CurrentValueAsString = option.ValueText;
        await CloseAsync(true);
    }

    private void OnSearchInput(ChangeEventArgs args)
    {
        _searchText = args.Value?.ToString() ?? string.Empty;
        _activeIndex = FindFirstEnabledIndex();
    }

    private void OnNativeChanged(ChangeEventArgs args)
    {
        if (!Disabled && !ReadOnly)
        {
            CurrentValueAsString = args.Value?.ToString();
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

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
                await SelectActiveAsync();
                break;
            case " " when !_isOpen:
                Open();
                break;
            case "Escape" when _isOpen:
                await CloseAsync(true);
                break;
        }
    }

    private async Task SelectActiveAsync()
    {
        var options = FilteredOptions;
        if (_activeIndex >= 0 && _activeIndex < options.Count)
        {
            await SelectAsync(options[_activeIndex]);
        }
    }

    private void MoveActive(int delta)
    {
        var options = FilteredOptions;
        if (options.Count == 0)
        {
            _activeIndex = -1;
            return;
        }

        for (var offset = 1; offset <= options.Count; offset++)
        {
            var candidate = (_activeIndex + delta * offset + options.Count) % options.Count;
            if (!options[candidate].Disabled)
            {
                _activeIndex = candidate;
                return;
            }
        }
    }

    private int FindInitialActiveIndex()
    {
        var selected = FilteredOptions.ToList().FindIndex(option => IsSelected(option.Value) && !option.Disabled);
        return selected >= 0 ? selected : FindFirstEnabledIndex();
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
        if (Options is null)
        {
            throw new InvalidOperationException("BzsSelect requires an Options collection.");
        }

        var duplicate = Options.GroupBy(static option => option.ValueText, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"BzsSelect option ValueText '{duplicate.Key}' must be unique.");
        }

        if (Options.GroupBy(static option => option.Value, EqualityComparer<TValue>.Default)
            .Any(static group => group.Count() > 1))
        {
            throw new InvalidOperationException("BzsSelect option values must be unique.");
        }
    }

    /// <summary>Closes the option panel after an outside pointer interaction.</summary>
    [JSInvokable]
    public Task CloseFromBrowserAsync() => InvokeAsync(() =>
    {
        if (!_isOpen)
        {
            return;
        }

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
