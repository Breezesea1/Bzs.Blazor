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
    private readonly BzsListboxState<TValue> _listbox;
    private ElementReference _rootReference;
    private ElementReference _triggerReference;
    private ElementReference _searchReference;
    private DotNetObjectReference<BzsSelect<TValue>>? _dotNetReference;
    private BzsSelectInterop? _interop;
    private bool _isInteractive;
    private bool _interopInitialized;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="BzsSelect{TValue}"/> class.</summary>
    public BzsSelect() => _listbox = new BzsListboxState<TValue>(
        () => Options,
        () => SearchEnabled,
        () => CurrentValue);

    private string ListboxId => $"{InputId}-listbox";
    private string SearchId => $"{InputId}-search";
    private string ConstraintId => $"{InputId}-constraint";
    private string EffectiveSearchPlaceholder => string.IsNullOrWhiteSpace(SearchPlaceholder)
        ? Localize("SelectSearchPlaceholder")
        : SearchPlaceholder.Trim();
    private string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText)
        ? Localize("SelectNoMatches")
        : EmptyText.Trim();
    private string? ActiveOptionId => _listbox.ActiveOption is { } active ? GetOptionId(active) : null;
    private BzsSelectOption<TValue>? SelectedOption => Options.FirstOrDefault(option => IsSelected(option.Value));
    private string SelectedText => SelectedOption?.Label
        ?? (string.IsNullOrWhiteSpace(PlaceholderOption) ? Localize("SelectPlaceholder") : PlaceholderOption.Trim());
    private string EffectiveAccessibleName => !string.IsNullOrWhiteSpace(Label) ? Label.Trim() : SelectedText;
    private IReadOnlyList<BzsSelectOption<TValue>> FilteredOptions => _listbox.VisibleOptions;

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
                ["aria-expanded"] = _listbox.IsOpen ? "true" : "false",
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
            _interop ??= new BzsSelectInterop(JsRuntime, LoggerFactory);
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

        if (_interop is not null && _listbox.TakePositionRequest(out var focusSearch))
        {
            var focus = focusSearch ? _searchReference : (ElementReference?)null;
            await _interop.SetOpenAsync(_instanceId, _listbox.IsOpen, focus);
        }
    }

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
        index == _listbox.ActiveIndex ? "bzs-select__option--active" : null,
        disabled ? "bzs-select__option--disabled" : null,
    }.Where(static value => value is not null));

    private static bool HasAdditionalAccessibleName(IReadOnlyDictionary<string, object> attributes) =>
        attributes.ContainsKey("aria-label") || attributes.ContainsKey("aria-labelledby");

    private void Activate(int index) => _listbox.Activate(index);

    private async Task ToggleAsync()
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        if (_listbox.IsOpen)
        {
            await CloseAsync(false);
        }
        else
        {
            _listbox.Open();
        }
    }

    private async Task CloseAsync(bool restoreFocus)
    {
        if (!_listbox.IsOpen)
        {
            return;
        }

        _listbox.Close();
        if (_interop is not null)
        {
            await _interop.SetOpenAsync(_instanceId, false, restoreFocus ? _triggerReference : null);
        }
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

    private void OnSearchInput(ChangeEventArgs args) => _listbox.Search(args.Value?.ToString());

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

        switch (_listbox.HandleKey(args.Key))
        {
            case BzsListboxAction.CommitActive:
                if (_listbox.ActiveOption is { } active)
                {
                    await SelectAsync(active);
                }
                break;
            case BzsListboxAction.Closed:
                if (_interop is not null)
                {
                    await _interop.SetOpenAsync(_instanceId, false, _triggerReference);
                }
                break;
        }
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
        if (!_listbox.IsOpen)
        {
            return;
        }

        _listbox.Close();
        StateHasChanged();
    });

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

            try
            {
                _dotNetReference?.Dispose();
            }
            catch (Exception exception)
            {
                disposalException ??= exception;
            }
            _dotNetReference = null;

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
