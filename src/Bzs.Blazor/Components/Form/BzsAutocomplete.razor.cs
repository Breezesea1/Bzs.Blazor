using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Renders an asynchronous editable combobox integrated with EditContext.</summary>
/// <typeparam name="TValue">The selected value type.</typeparam>
public sealed partial class BzsAutocomplete<TValue> : BzsInputBase<TValue>
{
    private const int ImmediateInteropAttemptLimit = 2;
    private readonly string _instanceId = $"bzs-autocomplete-{Guid.NewGuid():N}";
    private ElementReference _rootElement;
    private DotNetObjectReference<BzsAutocomplete<TValue>>? _dotNetReference;
    private BzsAnchoredOverlayInterop? _interop;
    private BzsAutocompleteInterop? _keyboardInterop;
    private BzsAutocompleteRequestCoordinator<TValue>? _requestCoordinator;
    private IBzsAutocompleteProvider<TValue>? _coordinatorProvider;
    private IReadOnlyList<BzsAutocompleteOption<TValue>> _suggestions = [];
    private BzsAutocompleteOption<TValue>? _selectedOption;
    private Exception? _providerError;
    private string _query = string.Empty;
    private string _committedQuery = string.Empty;
    private int _activeIndex = -1;
    private bool _isOpen;
    private bool _loading;
    private bool _parametersInitialized;
    private TValue? _lastParameterValue;
    private bool _interopInitialized;
    private int _initializationAttemptCount;
    private bool _keyboardInteropInitialized;
    private int _keyboardInitializationAttemptCount;
    private bool _overlaySynchronizationPending = true;
    private int _overlaySynchronizationVersion;
    private int _overlaySynchronizationAttemptCount;
    private bool _restoreFocusPending;
    private bool _disposed;

    /// <summary>Gets or sets the asynchronous suggestion provider.</summary>
    [Parameter, EditorRequired]
    public IBzsAutocompleteProvider<TValue>? Provider { get; set; }

    /// <summary>Gets or sets the delay applied before a query is sent to the provider.</summary>
    [Parameter]
    public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets or sets the minimum query length required before suggestions are requested.</summary>
    [Parameter]
    public int MinimumQueryLength { get; set; } = 1;

    /// <summary>Gets or sets the template used to render a suggestion.</summary>
    [Parameter]
    public RenderFragment<BzsAutocompleteOption<TValue>>? ResultTemplate { get; set; }

    /// <summary>Gets or sets the text displayed while suggestions are loading.</summary>
    [Parameter]
    public string? LoadingText { get; set; }

    /// <summary>Gets or sets the text displayed when a query has no suggestions.</summary>
    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>Gets or sets the text displayed after a provider failure.</summary>
    [Parameter]
    public string? ErrorText { get; set; }

    /// <summary>Gets or sets the retry command text.</summary>
    [Parameter]
    public string? RetryText { get; set; }

    /// <summary>Gets or sets the clear command accessible text.</summary>
    [Parameter]
    public string? ClearText { get; set; }

    /// <summary>Gets or sets the validation message used when strict text does not match a suggestion.</summary>
    [Parameter]
    public string? SelectionValidationMessage { get; set; }

    /// <summary>Gets or sets the callback raised when the suggestion provider fails.</summary>
    [Parameter]
    public EventCallback<Exception> ProviderFailed { get; set; }

    private string ListboxId => $"{InputId}-listbox";
    private string? ActiveOptionId => _isOpen && _activeIndex >= 0 && _activeIndex < _suggestions.Count
        ? GetOptionId(_activeIndex)
        : null;
    private string EffectiveLoadingText => Normalize(LoadingText) ?? Localize("AutocompleteLoadingText");
    private string EffectiveEmptyText => Normalize(EmptyText) ?? Localize("AutocompleteEmptyText");
    private string EffectiveErrorText => Normalize(ErrorText) ?? Localize("AutocompleteErrorText");
    private string EffectiveRetryText => Normalize(RetryText) ?? Localize("AutocompleteRetryText");
    private string EffectiveClearText => Normalize(ClearText) ?? Localize("AutocompleteClearText");
    private string FormValueText => _selectedOption?.ValueText ?? FormatValueForForm(Value);

    private IReadOnlyDictionary<string, object> InputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-input bzs-autocomplete__input", "text"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "combobox",
                ["aria-autocomplete"] = "list",
                ["aria-haspopup"] = "listbox",
                ["aria-controls"] = ListboxId,
                ["aria-expanded"] = _isOpen ? "true" : "false",
                ["autocomplete"] = "off",
                ["spellcheck"] = "false",
                ["data-bzs-anchor"] = "true",
            };
            attributes.Remove("name");

            if (ActiveOptionId is { } activeOptionId)
            {
                attributes["aria-activedescendant"] = activeOptionId;
            }
            else
            {
                attributes.Remove("aria-activedescendant");
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value)
    {
        if (_selectedOption is not null
            && EqualityComparer<TValue>.Default.Equals(_selectedOption.Value, value))
        {
            return _selectedOption.Label;
        }

        return value switch
        {
            null => null,
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrEmpty(value) && default(TValue) is null)
        {
            _lastParameterValue = default;
            result = default!;
            validationErrorMessage = null;
            return true;
        }

        var option = _suggestions.FirstOrDefault(candidate => !candidate.Disabled
            && (string.Equals(candidate.Label, value, StringComparison.Ordinal)
                || string.Equals(candidate.ValueText, value, StringComparison.Ordinal)));
        if (option is not null)
        {
            _selectedOption = option;
            _committedQuery = option.Label;
            _lastParameterValue = option.Value;
            result = option.Value;
            validationErrorMessage = null;
            return true;
        }

        result = default!;
        validationErrorMessage = FormatSelectionValidationMessage();
        return false;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Provider is null)
        {
            throw new InvalidOperationException("BzsAutocomplete requires a Provider.");
        }

        if (DebounceDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(DebounceDelay), DebounceDelay, "DebounceDelay cannot be negative.");
        }

        if (MinimumQueryLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumQueryLength),
                MinimumQueryLength,
                "MinimumQueryLength cannot be negative.");
        }

        if (!ReferenceEquals(_coordinatorProvider, Provider))
        {
            _requestCoordinator?.Dispose();
            _requestCoordinator = new BzsAutocompleteRequestCoordinator<TValue>(Provider);
            _coordinatorProvider = Provider;
            ResetProviderState();
            SetOpen(false);
        }

        if (!_parametersInitialized
            || !EqualityComparer<TValue>.Default.Equals(_lastParameterValue, Value))
        {
            _selectedOption = _suggestions.FirstOrDefault(option =>
                EqualityComparer<TValue>.Default.Equals(option.Value, Value));
            _query = FormatValueAsString(Value) ?? string.Empty;
            _committedQuery = _query;
            _lastParameterValue = Value;
            _parametersInitialized = true;
        }

        if (Disabled || ReadOnly || _query.Length < MinimumQueryLength)
        {
            _requestCoordinator?.Cancel();
            ResetProviderState();
            SetOpen(false);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (!_interopInitialized)
        {
            _interop ??= new BzsAnchoredOverlayInterop(JS, LoggerFactory);
            _dotNetReference ??= DotNetObjectReference.Create(this);
            _initializationAttemptCount++;
            _interopInitialized = await _interop.InitializeAsync(_instanceId, _rootElement, _dotNetReference);
            if (_disposed || !_interopInitialized)
            {
                if (!_disposed && _initializationAttemptCount < ImmediateInteropAttemptLimit)
                {
                    await InvokeAsync(StateHasChanged);
                }
                return;
            }

            _initializationAttemptCount = 0;
            RequestOverlaySynchronization();
        }

        if (_overlaySynchronizationPending && _interop is not null)
        {
            var version = _overlaySynchronizationVersion;
            _overlaySynchronizationAttemptCount++;
            var synchronized = await _interop.SetOpenAsync(
                _instanceId,
                _isOpen,
                "bottom-start",
                closeOnOutsideInteraction: true,
                closeOnEscape: true,
                restoreFocus: !_isOpen && _restoreFocusPending);
            if (_disposed)
            {
                return;
            }

            if (!synchronized || version != _overlaySynchronizationVersion)
            {
                if (_overlaySynchronizationPending
                    && _overlaySynchronizationAttemptCount < ImmediateInteropAttemptLimit)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
            else
            {
                _overlaySynchronizationPending = false;
                _restoreFocusPending = false;
            }
        }

        if (!_keyboardInteropInitialized)
        {
            _keyboardInterop ??= new BzsAutocompleteInterop(JS, LoggerFactory);
            _keyboardInitializationAttemptCount++;
            _keyboardInteropInitialized = await _keyboardInterop.InitializeAsync(_instanceId, _rootElement);
            if (_disposed || !_keyboardInteropInitialized)
            {
                if (!_disposed && _keyboardInitializationAttemptCount < ImmediateInteropAttemptLimit)
                {
                    await InvokeAsync(StateHasChanged);
                }
                return;
            }

            _keyboardInitializationAttemptCount = 0;
        }
    }

    private async Task OnInputAsync(ChangeEventArgs args)
    {
        if (Disabled || ReadOnly || _disposed)
        {
            return;
        }

        var query = args.Value?.ToString() ?? string.Empty;
        var queryChanged = !string.Equals(_query, query, StringComparison.Ordinal);
        _query = query;
        _selectedOption = null;
        if (queryChanged && !string.Equals(_query, _committedQuery, StringComparison.Ordinal))
        {
            _lastParameterValue = default;
            CurrentValue = default!;
        }

        if (_query.Length < MinimumQueryLength)
        {
            _requestCoordinator?.Cancel();
            ResetProviderState();
            SetOpen(false);
            return;
        }

        await LoadSuggestionsAsync(bypassDebounce: false);
    }

    private async Task LoadSuggestionsAsync(bool bypassDebounce)
    {
        if (_requestCoordinator is null || _disposed)
        {
            return;
        }

        _loading = true;
        _providerError = null;
        _suggestions = [];
        _activeIndex = -1;
        SetOpen(true);
        var request = _requestCoordinator.QueryAsync(_query, DebounceDelay, bypassDebounce);
        await InvokeAsync(StateHasChanged);
        var result = await request;
        if (_disposed || !result.IsCurrent)
        {
            return;
        }

        _loading = false;
        _providerError = result.Error;
        _suggestions = result.Suggestions;
        _activeIndex = FindFirstEnabledIndex();
        await InvokeAsync(StateHasChanged);

        if (result.Error is not null)
        {
            await ProviderFailed.InvokeAsync(result.Error);
        }
    }

    private Task RetryAsync() => Disabled || ReadOnly || _disposed
        ? Task.CompletedTask
        : LoadSuggestionsAsync(bypassDebounce: true);

    private Task ClearAsync()
    {
        if (Disabled || ReadOnly || _disposed)
        {
            return Task.CompletedTask;
        }

        _requestCoordinator?.Cancel();
        _query = string.Empty;
        _committedQuery = string.Empty;
        _selectedOption = null;
        ResetProviderState();
        SetOpen(false);
        _lastParameterValue = default;
        CurrentValue = default!;
        return Task.CompletedTask;
    }

    private Task SelectAsync(BzsAutocompleteOption<TValue> option)
    {
        if (Disabled || ReadOnly || option.Disabled || _disposed)
        {
            return Task.CompletedTask;
        }

        _requestCoordinator?.Cancel();
        _selectedOption = option;
        _query = option.Label;
        _committedQuery = option.Label;
        _lastParameterValue = option.Value;
        CurrentValue = option.Value;
        SetOpen(false);
        return Task.CompletedTask;
    }

    private Task OnBlurAsync()
    {
        if (!Disabled && !ReadOnly)
        {
            CommitQuery();
        }

        return Task.CompletedTask;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled || ReadOnly || _disposed)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
                if (!_isOpen && _query.Length >= MinimumQueryLength)
                {
                    await LoadSuggestionsAsync(bypassDebounce: true);
                }
                else
                {
                    MoveActive(1);
                }
                break;
            case "ArrowUp":
                MoveActive(-1);
                break;
            case "Home" when _isOpen:
                _activeIndex = FindFirstEnabledIndex();
                break;
            case "End" when _isOpen:
                _activeIndex = FindLastEnabledIndex();
                break;
            case "Enter":
                if (_isOpen && _activeIndex >= 0 && _activeIndex < _suggestions.Count)
                {
                    await SelectAsync(_suggestions[_activeIndex]);
                }
                else
                {
                    CommitQuery();
                }
                break;
            case "Tab":
                CommitQuery();
                Close(restoreFocus: false, cancelRequest: true);
                break;
            case "Escape" when _isOpen:
                Close(restoreFocus: true, cancelRequest: true);
                break;
        }
    }

    private void CommitQuery()
    {
        if (string.Equals(_committedQuery, _query, StringComparison.Ordinal))
        {
            return;
        }

        CurrentValueAsString = _query;
    }

    private bool IsSelected(BzsAutocompleteOption<TValue> option) =>
        EqualityComparer<TValue>.Default.Equals(CurrentValue, option.Value);

    private string GetOptionId(int index) => $"{InputId}-option-{index}";

    private string GetOptionClass(int index, BzsAutocompleteOption<TValue> option) => string.Join(" ", new[]
    {
        "bzs-autocomplete__option",
        index == _activeIndex ? "bzs-autocomplete__option--active" : null,
        IsSelected(option) ? "bzs-autocomplete__option--selected" : null,
        option.Disabled ? "bzs-autocomplete__option--disabled" : null,
    }.Where(static value => value is not null));

    private void Activate(int index)
    {
        if (index >= 0 && index < _suggestions.Count && !_suggestions[index].Disabled)
        {
            _activeIndex = index;
        }
    }

    private int FindFirstEnabledIndex()
    {
        for (var index = 0; index < _suggestions.Count; index++)
        {
            if (!_suggestions[index].Disabled)
            {
                return index;
            }
        }

        return -1;
    }

    private int FindLastEnabledIndex()
    {
        for (var index = _suggestions.Count - 1; index >= 0; index--)
        {
            if (!_suggestions[index].Disabled)
            {
                return index;
            }
        }

        return -1;
    }

    private void MoveActive(int delta)
    {
        if (!_isOpen || _suggestions.Count == 0)
        {
            return;
        }

        for (var offset = 1; offset <= _suggestions.Count; offset++)
        {
            var candidate = (_activeIndex + (delta * offset)) % _suggestions.Count;
            if (candidate < 0)
            {
                candidate += _suggestions.Count;
            }

            if (!_suggestions[candidate].Disabled)
            {
                _activeIndex = candidate;
                return;
            }
        }
    }

    private void ResetProviderState()
    {
        _loading = false;
        _providerError = null;
        _suggestions = [];
        _activeIndex = -1;
    }

    private void SetOpen(bool open)
    {
        if (_isOpen == open)
        {
            return;
        }

        _isOpen = open;
        if (open)
        {
            _restoreFocusPending = false;
        }
        RequestOverlaySynchronization();
    }

    private void Close(bool restoreFocus, bool cancelRequest)
    {
        if (cancelRequest)
        {
            _requestCoordinator?.Cancel();
            _loading = false;
        }

        _restoreFocusPending = restoreFocus;
        SetOpen(false);
    }

    private void RequestOverlaySynchronization()
    {
        _overlaySynchronizationVersion++;
        _overlaySynchronizationAttemptCount = 0;
        _overlaySynchronizationPending = true;
    }

    private string FormatSelectionValidationMessage()
    {
        var fieldName = DisplayName ?? FieldIdentifier.FieldName;
        return string.IsNullOrWhiteSpace(SelectionValidationMessage)
            ? Localize("AutocompleteSelectionValidation", fieldName)
            : SelectionValidationMessage.Trim().Replace("{0}", fieldName, StringComparison.Ordinal);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatValueForForm(TValue? value) => value switch
    {
        null => string.Empty,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>Closes the suggestion panel after a browser-owned outside or Escape interaction.</summary>
    [JSInvokable]
    public Task CloseFromBrowserAsync(bool restoreFocus = false)
    {
        if (_disposed || !_isOpen)
        {
            return Task.CompletedTask;
        }

        return InvokeAsync(() =>
        {
            if (_disposed || !_isOpen)
            {
                return;
            }

            Close(restoreFocus, cancelRequest: true);
            StateHasChanged();
        });
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _requestCoordinator?.Dispose();
        _requestCoordinator = null;

        Exception? disposalException = null;
        try
        {
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

            if (_keyboardInterop is not null)
            {
                try
                {
                    await _keyboardInterop.DisposeInstanceAsync(_instanceId);
                }
                catch (Exception exception)
                {
                    disposalException ??= exception;
                }

                try
                {
                    await _keyboardInterop.DisposeAsync();
                }
                catch (Exception exception)
                {
                    disposalException ??= exception;
                }
            }
        }
        finally
        {
            try
            {
                _dotNetReference?.Dispose();
            }
            catch (Exception exception)
            {
                disposalException ??= exception;
            }

            _dotNetReference = null;
            ((IDisposable)this).Dispose();
        }

        if (disposalException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disposalException).Throw();
        }
    }
}
