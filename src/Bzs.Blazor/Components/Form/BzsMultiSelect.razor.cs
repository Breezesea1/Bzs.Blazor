using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Renders a searchable, strongly typed multi-select integrated with EditContext.</summary>
public sealed partial class BzsMultiSelect<TValue> : BzsInputBase<IReadOnlyList<TValue>>
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

    private const int ImmediateInteropAttemptLimit = 2;
    private readonly string _instanceId = $"bzs-multi-select-{Guid.NewGuid():N}";
    private readonly BzsOptionListState<BzsSelectOption<TValue>> _optionList;
    private string _searchText = string.Empty;
    private ElementReference _rootReference;
    private BzsAnchoredOverlaySession? _overlaySession;
    private BzsSelectInterop? _interop;
    private bool _isInteractive;
    private bool _interopInitialized;
    private int _interopInitializationAttemptCount;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="BzsMultiSelect{TValue}"/> class.</summary>
    public BzsMultiSelect() => _optionList = new BzsOptionListState<BzsSelectOption<TValue>>(
        static option => option.Disabled,
        static (left, right) => EqualityComparer<TValue>.Default.Equals(left.Value, right.Value));

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
    private string? ActiveOptionId => _optionList.ActiveOption is { } active ? GetOptionId(active) : null;
    private IReadOnlyList<TValue> SelectedValues => CurrentValue ?? [];
    private HashSet<TValue> SelectedSet => SelectedValues.ToHashSet(EqualityComparer<TValue>.Default);
    private IReadOnlyList<BzsSelectOption<TValue>> SelectedOptions => Options.Where(option => IsSelected(option.Value)).ToArray();
    private IReadOnlyList<BzsSelectOption<TValue>> FilteredOptions => _optionList.Options;
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
                ["data-bzs-anchor"] = "true",
                ["aria-haspopup"] = "listbox",
                ["aria-expanded"] = _optionList.IsOpen ? "true" : "false",
                ["aria-controls"] = ListboxId,
                ["aria-autocomplete"] = SearchEnabled ? "list" : "none",
            };

            attributes.Remove("name");
            attributes.Remove("placeholder");
            attributes.Remove("required");
            if (ActiveOptionId is { } activeOptionId)
            {
                attributes["aria-activedescendant"] = activeOptionId;
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
        PublishOptionSnapshot();
        UpdateOverlayState();
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

        if (_disposed)
        {
            return;
        }

        await GetOverlaySession().AfterRenderAsync(_rootReference);
        if (_disposed || _interopInitialized)
        {
            return;
        }

        _interop ??= new BzsSelectInterop(JsRuntime, LoggerFactory);
        _interopInitializationAttemptCount++;
        _interopInitialized = await _interop.InitializeAsync(_instanceId, _rootReference);
        if (!_disposed
            && !_interopInitialized
            && _interopInitializationAttemptCount < ImmediateInteropAttemptLimit)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

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
        index == _optionList.ActiveIndex ? "bzs-multi-select__option--active" : null,
        disabled ? "bzs-multi-select__option--disabled" : null,
    }.Where(static value => value is not null));

    private void Activate(int index) => _optionList.Activate(index);

    private void ToggleAsync()
    {
        if (Disabled || ReadOnly) return;
        if (_optionList.IsOpen)
        {
            _ = GetOverlaySession().RequestCloseAsync(false);
            return;
        }

        OpenOptionList();
    }

    private void ToggleOptionAsync(BzsSelectOption<TValue> option)
    {
        if (Disabled || ReadOnly || option.Disabled) return;

        var selected = SelectedSet;
        if (!selected.Add(option.Value)) selected.Remove(option.Value);
        SetSelection(selected);
    }

    private void SelectVisibleAsync()
    {
        var selected = SelectedSet;
        foreach (var option in FilteredOptions.Where(static option => !option.Disabled)) selected.Add(option.Value);
        SetSelection(selected);
    }

    private void InvertVisibleAsync()
    {
        var selected = SelectedSet;
        foreach (var option in FilteredOptions.Where(static option => !option.Disabled))
        {
            if (!selected.Add(option.Value)) selected.Remove(option.Value);
        }
        SetSelection(selected);
    }

    private void ClearAsync()
    {
        var selected = SelectedSet;
        foreach (var option in FilteredOptions.Where(static option => !option.Disabled))
        {
            selected.Remove(option.Value);
        }
        SetSelection(selected);
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
        PublishOptionSnapshot();
    }

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (Disabled || ReadOnly) return;

        if (!_optionList.IsOpen && args.Key is "ArrowDown" or "ArrowUp" or " ")
        {
            OpenOptionList();
            return;
        }

        var action = _optionList.HandleKey(args.Key, commitOnSpaceWhenOpen: !SearchEnabled);
        switch (action)
        {
            case BzsOptionListAction.CommitActive when _optionList.ActiveOption is { } active:
                ToggleOptionAsync(active);
                break;
            case BzsOptionListAction.CloseRequested:
                _ = GetOverlaySession().RequestCloseAsync(true);
                break;
        }
    }

    private BzsAnchoredOverlaySession GetOverlaySession() =>
        _overlaySession ??= new BzsAnchoredOverlaySession(
            JsRuntime,
            HandleOverlayCloseRequestedAsync,
            ImmediateInteropAttemptLimit,
            LoggerFactory);

    private void UpdateOverlayState() =>
        GetOverlaySession().SetDesiredState(new BzsAnchoredOverlayState(
            _optionList.IsOpen,
            BzsPopoverPlacement.BottomStart,
            CloseOnOutsideInteraction: true,
            CloseOnEscape: true));

    private Task HandleOverlayCloseRequestedAsync() => InvokeAsync(() =>
    {
        if (!_optionList.IsOpen) return;

        _optionList.Close();
        _searchText = string.Empty;
        PublishOptionSnapshot();
        UpdateOverlayState();
        StateHasChanged();
    });

    private void OpenOptionList()
    {
        _searchText = string.Empty;
        PublishOptionSnapshot();
        _optionList.Open();
        UpdateOverlayState();
    }

    private void PublishOptionSnapshot() => _optionList.SetOptions(
        BzsSelectNavigation.Filter(Options, _searchText));

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

    /// <summary>Closes the option panel after a browser-owned outside or Escape interaction.</summary>
    public Task CloseFromBrowserAsync(bool restoreFocus = false)
    {
        if (_disposed || !_optionList.IsOpen)
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
        try
        {
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

            if (_interop is not null)
            {
                try
                {
                    await _interop.DisposeInstanceAsync(_instanceId);
                }
                catch (Exception exception)
                {
                    disposalException ??= exception;
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

            if (disposalException is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disposalException).Throw();
            }
        }
        finally
        {
            ((IDisposable)this).Dispose();
        }
    }
}
