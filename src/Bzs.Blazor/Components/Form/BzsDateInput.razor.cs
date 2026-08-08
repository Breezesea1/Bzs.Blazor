using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.ExceptionServices;
using Bzs.Blazor.Localization;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>
/// Renders a culture-aware date input with a progressively enhanced calendar.
/// Calendar dates use the Gregorian calendar localized by the current culture.
/// DateTimeOffset values preserve an existing offset and use UTC when no value exists.
/// </summary>
public sealed partial class BzsDateInput<TValue> : BzsInputBase<TValue>
{
    private const string NativeDateFormat = "yyyy-MM-dd";
    private const int ImmediateOpenSyncAttemptLimit = 2;
    private const int PeriodTypeaheadResetMilliseconds = 2_000;
    private static readonly DateOnly[] DateFormatValidationDates =
    [
        new(2000, 2, 29),
        new(2099, 11, 23),
    ];
    private static readonly ResourceManager DatePickerResources = new(typeof(BzsBlazorResources));

    /// <summary>
    /// Gets or sets the culture used for date formatting and date-picker-owned text.
    /// When omitted, the component follows the current culture and UI culture.
    /// </summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Gets or sets the culture-aware date format shown by the interactive text input.
    /// The format must preserve the year, month, and day.
    /// </summary>
    [Parameter] public string? DateFormat { get; set; }

    /// <summary>Gets or sets the earliest date that can be entered or selected.</summary>
    [Parameter] public DateOnly? Min { get; set; }

    /// <summary>Gets or sets the latest date that can be entered or selected.</summary>
    [Parameter] public DateOnly? Max { get; set; }

    /// <summary>Gets or sets whether the calendar shows an action that clears nullable values.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>Gets or sets whether the calendar shows an action that selects today.</summary>
    [Parameter] public bool ShowToday { get; set; } = true;

    private readonly string _instanceId = $"bzs-date-picker-{Guid.NewGuid():N}";
    private readonly CancellationTokenSource _interopLifetimeCancellation = new();
    private ElementReference _rootReference;
    private ElementReference _inputReference;
    private ElementReference _periodMenuReference;
    private DotNetObjectReference<BzsDateInput<TValue>>? _dotNetReference;
    private BzsDateInputInterop? _interop;
    private Task<BzsDateInputInitialization>? _interopInitializationTask;
    private bool _disposed;
    private bool _isInteractive;
    private bool _interopInitialized;
    private bool _interopInitializationPending;
    private bool _renderAfterInitialization;
    private bool _isOpen;
    private bool _openRequested;
    private bool _openSyncPending;
    private int _openSyncVersion;
    private int _openSyncAttemptCount;
    private bool _focusCalendarOnOpen;
    private bool _restoreInputFocusOnClose;
    private bool _focusDayPending;
    private bool _scrollPeriodMenuPending;
    private double? _pointerX;
    private double? _pointerY;
    private DatePeriodMenu? _openPeriodMenu;
    private int _activeMonth;
    private int _activeYear;
    private string _periodTypeahead = string.Empty;
    private long _periodTypeaheadTimestamp;
    private CultureInfo? _dateCultureSource;
    private CultureInfo? _dateCulture;
    private DateOnly _today = DateOnly.FromDateTime(DateTime.Today);
    private DateOnly _viewMonth = BzsDateCalendarMath.FirstOfMonth(DateOnly.FromDateTime(DateTime.Today));
    private DateOnly _focusedDate = DateOnly.FromDateTime(DateTime.Today);

    private string PanelId => $"{InputId}-calendar";
    private string MonthListboxId => $"{InputId}-month-options";
    private string YearListboxId => $"{InputId}-year-options";
    private string? ActiveMonthOptionId => _openPeriodMenu == DatePeriodMenu.Month
        ? GetMonthOptionId(_activeMonth)
        : null;
    private string? ActiveYearOptionId => _openPeriodMenu == DatePeriodMenu.Year
        ? GetYearOptionId(_activeYear)
        : null;
    private string? ExplicitCultureName => Culture?.Name;
    private string? ExplicitCultureDirection => Culture is null
        ? null
        : Culture.TextInfo.IsRightToLeft ? "rtl" : "ltr";
    private string EffectiveDateFormat => string.IsNullOrWhiteSpace(DateFormat) ? "d" : DateFormat.Trim();
    private CultureInfo DateCulture
    {
        get
        {
            var source = Culture ?? CultureInfo.CurrentCulture;
            if (!ReferenceEquals(_dateCultureSource, source))
            {
                _dateCultureSource = source;
                _dateCulture = BzsDateCalendarMath.CreateGregorianCulture(source);
            }
            return _dateCulture!;
        }
    }
    private DateOnly Today => _today;
    private DateOnly FirstAllowedDate => Min ?? DateOnly.MinValue;
    private DateOnly LastAllowedDate => Max ?? DateOnly.MaxValue;
    private DayOfWeek FirstDayOfWeek => DateCulture.DateTimeFormat.FirstDayOfWeek;
    private string? NativeValueAsString => BzsDateValueAdapter<TValue>.TryGetDate(CurrentValue, out var date)
        ? FormatNativeDate(date)
        : null;
    private string ViewMonthAccessibleLabel => _viewMonth.ToString("Y", DateCulture);
    private bool CanNavigatePreviousMonth => _viewMonth > BzsDateCalendarMath.FirstOfMonth(FirstAllowedDate);
    private bool CanNavigateNextMonth => _viewMonth < BzsDateCalendarMath.FirstOfMonth(LastAllowedDate);
    private string IsMonthMenuOpen => _openPeriodMenu == DatePeriodMenu.Month ? "true" : "false";
    private string IsYearMenuOpen => _openPeriodMenu == DatePeriodMenu.Year ? "true" : "false";

    private IReadOnlyDictionary<string, object> NativeInputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-input bzs-date-input", "date"),
                StringComparer.OrdinalIgnoreCase);
            AddRangeAttributes(attributes);
            AddCultureAttributes(attributes);
            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> TextInputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-input bzs-date-picker__input", "text"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "combobox",
                ["aria-haspopup"] = "dialog",
                ["aria-expanded"] = _isOpen ? "true" : "false",
                ["aria-controls"] = PanelId,
                ["autocomplete"] = "off",
            };
            AddCultureAttributes(attributes);
            return attributes;
        }
    }

    private IReadOnlyList<int> AvailableMonths => BzsDateCalendarMath.GetAvailableMonths(
        _viewMonth.Year,
        FirstAllowedDate,
        LastAllowedDate);

    private IReadOnlyList<int> AvailableYears => BzsDateCalendarMath.GetAvailableYears(
        _viewMonth.Year,
        FirstAllowedDate,
        LastAllowedDate);

    private IReadOnlyList<CalendarWeekday> Weekdays
    {
        get
        {
            var format = DateCulture.DateTimeFormat;
            return Enumerable.Range(0, 7)
                .Select(offset => ((int)FirstDayOfWeek + offset) % 7)
                .Select(day => new CalendarWeekday(format.ShortestDayNames[day], format.DayNames[day]))
                .ToArray();
        }
    }

    private IReadOnlyList<BzsDateCalendarDay> CalendarDays
    {
        get
        {
            var selectedDate = BzsDateValueAdapter<TValue>.TryGetDate(CurrentValue, out var selected)
                ? selected
                : (DateOnly?)null;
            return BzsDateCalendarMath.CreateCalendarGrid(
                _viewMonth,
                FirstDayOfWeek,
                Today,
                selectedDate,
                _focusedDate,
                FirstAllowedDate,
                LastAllowedDate);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ValidateParameters();

        if (Disabled || ReadOnly)
        {
            if (_isOpen || _openRequested)
            {
                SetClosedState();
            }
            return;
        }

        if (!_isOpen)
        {
            SynchronizeCalendarWithValue();
        }
        else
        {
            _focusedDate = BzsDateCalendarMath.ClampDate(_focusedDate, FirstAllowedDate, LastAllowedDate);
            _viewMonth = BzsDateCalendarMath.ClampMonth(_viewMonth, FirstAllowedDate, LastAllowedDate);
            SynchronizeOpenPeriodMenu();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (!_isInteractive)
        {
            _isInteractive = true;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (!_interopInitialized)
        {
            if (_interopInitializationPending)
            {
                _renderAfterInitialization = true;
                return;
            }

            _interop ??= new BzsDateInputInterop(JsRuntime, LoggerFactory);
            _dotNetReference ??= DotNetObjectReference.Create(this);
            _interopInitializationPending = true;
            BzsDateInputInitialization initialization;
            try
            {
                _interopInitializationTask = _interop.InitializeAsync(
                    _instanceId,
                    _rootReference,
                    _dotNetReference,
                    _interopLifetimeCancellation.Token).AsTask();
                initialization = await _interopInitializationTask;
            }
            finally
            {
                _interopInitializationPending = false;
            }

            if (_disposed)
            {
                return;
            }

            _interopInitialized = initialization.Initialized;
            if (!_interopInitialized)
            {
                if (_renderAfterInitialization)
                {
                    _renderAfterInitialization = false;
                    await InvokeAsync(StateHasChanged);
                }
                return;
            }

            _renderAfterInitialization = false;
            if (initialization.BrowserToday is { } browserToday)
            {
                _today = browserToday;
            }

            if (_openRequested)
            {
                ActivateOpenRequest();
                await InvokeAsync(StateHasChanged);
                return;
            }
        }

        if (_openSyncPending && _interop is not null)
        {
            var synchronized = await TrySynchronizeOpenStateAsync();
            if (_disposed)
            {
                return;
            }

            if (!synchronized
                && _openSyncPending
                && _openSyncAttemptCount < ImmediateOpenSyncAttemptLimit)
            {
                await InvokeAsync(StateHasChanged);
                return;
            }
        }

        if (_focusDayPending && _interop is not null)
        {
            _focusDayPending = false;
            await _interop.FocusActiveDayAsync(_instanceId);
        }

        if (_scrollPeriodMenuPending && _openPeriodMenu is not null && _interop is not null)
        {
            _scrollPeriodMenuPending = false;
            await _interop.ScrollActivePeriodOptionAsync(_periodMenuReference);
        }
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value)
    {
        if (!BzsDateValueAdapter<TValue>.TryGetDate(value, out var date))
        {
            return null;
        }

        return date.ToString(EffectiveDateFormat, DateCulture);
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value) && BzsDateValueAdapter<TValue>.IsNullable)
        {
            result = default!;
            validationErrorMessage = null;
            return true;
        }

        if (TryParseDateValue(value, out result)
            && BzsDateValueAdapter<TValue>.TryGetDate(result, out var date))
        {
            if (IsDateAllowed(date))
            {
                validationErrorMessage = null;
                return true;
            }

            validationErrorMessage = FormatDateValidationError("FormValidationDateRange");
            return false;
        }

        validationErrorMessage = FormatDateValidationError("FormValidationDate");
        return false;
    }

    private void ValidateParameters()
    {
        if (!BzsDateValueAdapter<TValue>.IsSupported)
        {
            throw new InvalidOperationException(
                $"{nameof(BzsDateInput<TValue>)} supports DateOnly, DateTime, DateTimeOffset, and their nullable forms.");
        }

        if (Min > Max)
        {
            throw new InvalidOperationException($"{nameof(BzsDateInput<TValue>)} requires Min to be earlier than or equal to Max.");
        }

        if (Clearable && !BzsDateValueAdapter<TValue>.IsNullable)
        {
            throw new InvalidOperationException(
                $"{nameof(BzsDateInput<TValue>)} requires a nullable TValue when {nameof(Clearable)} is true.");
        }

        ValidateDateFormat();
    }

    private void ValidateDateFormat()
    {
        try
        {
            foreach (var date in DateFormatValidationDates)
            {
                var formatted = date.ToString(EffectiveDateFormat, DateCulture);
                if (!DateOnly.TryParseExact(
                        formatted,
                        EffectiveDateFormat,
                        DateCulture,
                        DateTimeStyles.AllowWhiteSpaces,
                        out var parsed)
                    || parsed != date)
                {
                    throw new InvalidOperationException(
                        $"{nameof(BzsDateInput<TValue>)} requires {nameof(DateFormat)} to preserve the year, month, and day. "
                        + $"The value '{DateFormat}' is incomplete or ambiguous.");
                }
            }
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"{nameof(BzsDateInput<TValue>)} requires {nameof(DateFormat)} to be a valid date format. "
                + $"The value '{DateFormat}' is invalid.",
                exception);
        }
    }

    private void AddRangeAttributes(IDictionary<string, object> attributes)
    {
        if (Min is { } min)
        {
            attributes["min"] = FormatNativeDate(min);
        }
        if (Max is { } max)
        {
            attributes["max"] = FormatNativeDate(max);
        }
    }

    private void AddCultureAttributes(IDictionary<string, object> attributes)
    {
        if (Culture is null)
        {
            return;
        }

        attributes["lang"] = Culture.Name;
        attributes["dir"] = Culture.TextInfo.IsRightToLeft ? "rtl" : "ltr";
    }

    private void SynchronizeCalendarWithValue()
    {
        var reference = BzsDateValueAdapter<TValue>.TryGetDate(CurrentValue, out var selected) ? selected : Today;
        _focusedDate = BzsDateCalendarMath.ClampDate(reference, FirstAllowedDate, LastAllowedDate);
        _viewMonth = BzsDateCalendarMath.FirstOfMonth(_focusedDate);
    }

    private void SynchronizeOpenPeriodMenu()
    {
        if (_openPeriodMenu is not { } menu)
        {
            return;
        }

        if (!GetPeriodOptions(menu).Contains(GetActivePeriodOption(menu)))
        {
            SetActivePeriodOption(menu, GetViewPeriodOption(menu));
        }
        _scrollPeriodMenuPending = true;
    }

    private void OnNativeChanged(ChangeEventArgs args)
    {
        if (!Disabled && !ReadOnly)
        {
            CurrentValueAsString = args.Value?.ToString();
        }
    }

    private void OnTextChanged(ChangeEventArgs args)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        var text = args.Value?.ToString();
        CurrentValueAsString = text;
        if (TryParseDateValue(text, out var value)
            && BzsDateValueAdapter<TValue>.TryGetDate(value, out var date)
            && IsDateAllowed(date))
        {
            _focusedDate = date;
            _viewMonth = BzsDateCalendarMath.FirstOfMonth(date);
        }
    }

    private void OpenAtPointer(MouseEventArgs args) => Open(args.ClientX, args.ClientY, false);

    private void Open(double? pointerX, double? pointerY, bool focusCalendar)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        _pointerX = pointerX;
        _pointerY = pointerY;
        _focusCalendarOnOpen = focusCalendar;
        _openRequested = true;
        if (_interopInitialized)
        {
            ActivateOpenRequest();
        }
    }

    private void ActivateOpenRequest()
    {
        if (!_isOpen)
        {
            SynchronizeCalendarWithValue();
        }
        _isOpen = true;
        _openRequested = false;
        RequestOpenSynchronization();
    }

    private async Task CloseAsync(bool restoreFocus)
    {
        if (!_isOpen && !_openRequested)
        {
            return;
        }

        var wasOpen = _isOpen;
        SetClosedState(wasOpen && restoreFocus);
        if (wasOpen && _interop is not null)
        {
            await TrySynchronizeOpenStateAsync();
        }
    }

    private void SetClosedState(bool restoreInputFocus = false)
    {
        _isOpen = false;
        _openRequested = false;
        _pointerX = null;
        _pointerY = null;
        _focusCalendarOnOpen = false;
        _restoreInputFocusOnClose = restoreInputFocus;
        _focusDayPending = false;
        _scrollPeriodMenuPending = false;
        _openPeriodMenu = null;
        RequestOpenSynchronization();
    }

    private void RequestOpenSynchronization()
    {
        _openSyncVersion++;
        _openSyncAttemptCount = 0;
        _openSyncPending = true;
    }

    private async Task<bool> TrySynchronizeOpenStateAsync()
    {
        if (_interop is null)
        {
            return false;
        }

        var version = _openSyncVersion;
        var open = _isOpen;
        var focusCalendar = open && _focusCalendarOnOpen;
        var focusTarget = !open && _restoreInputFocusOnClose ? _inputReference : (ElementReference?)null;
        _openSyncAttemptCount++;
        var synchronized = await _interop.SetOpenAsync(
            _instanceId,
            open,
            _pointerX,
            _pointerY,
            focusCalendar,
            focusTarget);

        if (!synchronized || _disposed || version != _openSyncVersion)
        {
            return false;
        }

        _openSyncPending = false;
        _focusCalendarOnOpen = false;
        _restoreInputFocusOnClose = false;
        return true;
    }

    private async Task HandleInputKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        if (args.Key == "ArrowDown")
        {
            if (_isOpen)
            {
                _focusDayPending = true;
            }
            else
            {
                Open(null, null, true);
            }
        }
        else if (args.Key == "Escape" && (_isOpen || _openRequested))
        {
            await CloseAsync(true);
        }
    }

    private async Task HandleCalendarKeyDownAsync(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "ArrowLeft":
                MoveFocusedDate(-1);
                break;
            case "ArrowRight":
                MoveFocusedDate(1);
                break;
            case "ArrowUp":
                MoveFocusedDate(-7);
                break;
            case "ArrowDown":
                MoveFocusedDate(7);
                break;
            case "Home":
                MoveFocusedDate(-(((int)_focusedDate.DayOfWeek - (int)FirstDayOfWeek + 7) % 7));
                break;
            case "End":
                MoveFocusedDate(6 - (((int)_focusedDate.DayOfWeek - (int)FirstDayOfWeek + 7) % 7));
                break;
            case "PageUp":
                MoveFocusedDateByMonth(args.ShiftKey ? -12 : -1);
                break;
            case "PageDown":
                MoveFocusedDateByMonth(args.ShiftKey ? 12 : 1);
                break;
            case "Enter":
            case " ":
                await SelectDateAsync(_focusedDate);
                break;
        }
    }

    private Task HandleDialogKeyDownAsync(KeyboardEventArgs args) =>
        args.Key == "Escape" ? CloseAsync(true) : Task.CompletedTask;

    private void ActivatePeriodMenu(DatePeriodMenu menu, MouseEventArgs args)
    {
        if (_openPeriodMenu == menu && args.Detail == 0)
        {
            SelectActivePeriodOption(menu);
            return;
        }

        if (_openPeriodMenu == menu)
        {
            _openPeriodMenu = null;
            return;
        }

        OpenPeriodMenu(menu);
    }

    private async Task HandlePeriodKeyDownAsync(DatePeriodMenu menu, KeyboardEventArgs args)
    {
        var isOpen = _openPeriodMenu == menu;
        switch (args.Key)
        {
            case "ArrowDown":
                ResetPeriodTypeahead();
                if (!isOpen)
                {
                    OpenPeriodMenu(menu);
                }
                else
                {
                    MoveActivePeriodOption(menu, 1);
                }
                break;
            case "ArrowUp":
                ResetPeriodTypeahead();
                if (!isOpen)
                {
                    OpenPeriodMenu(menu);
                }
                else
                {
                    MoveActivePeriodOption(menu, -1);
                }
                break;
            case "Home" when isOpen:
                ResetPeriodTypeahead();
                SetActivePeriodBoundary(menu, first: true);
                break;
            case "End" when isOpen:
                ResetPeriodTypeahead();
                SetActivePeriodBoundary(menu, first: false);
                break;
            case "PageUp" when isOpen:
                ResetPeriodTypeahead();
                MoveActivePeriodOption(menu, -GetPeriodPageSize(menu));
                break;
            case "PageDown" when isOpen:
                ResetPeriodTypeahead();
                MoveActivePeriodOption(menu, GetPeriodPageSize(menu));
                break;
            case "Enter":
            case " ":
                if (isOpen)
                {
                    SelectActivePeriodOption(menu);
                }
                else
                {
                    OpenPeriodMenu(menu);
                }
                break;
            case "Escape":
                if (isOpen)
                {
                    _openPeriodMenu = null;
                }
                else
                {
                    await CloseAsync(true);
                }
                break;
            case "Tab":
                if (isOpen)
                {
                    SelectActivePeriodOption(menu);
                }
                break;
            default:
                if (isOpen && IsPeriodTypeaheadKey(args))
                {
                    ActivatePeriodOptionByTypeahead(menu, args.Key);
                }
                break;
        }
    }

    private void OpenPeriodMenu(DatePeriodMenu menu)
    {
        _openPeriodMenu = menu;
        _activeMonth = _viewMonth.Month;
        _activeYear = _viewMonth.Year;
        ResetPeriodTypeahead();
        _scrollPeriodMenuPending = true;
    }

    private void MoveActivePeriodOption(DatePeriodMenu menu, int offset)
    {
        var options = GetPeriodOptions(menu);
        var index = GetPeriodOptionIndex(options, GetActivePeriodOption(menu));
        var next = options[Math.Clamp(index + offset, 0, options.Count - 1)];
        SetActivePeriodOption(menu, next);
        _scrollPeriodMenuPending = true;
    }

    private void SetActivePeriodBoundary(DatePeriodMenu menu, bool first)
    {
        var options = GetPeriodOptions(menu);
        var value = first ? options[0] : options[^1];
        SetActivePeriodOption(menu, value);
        _scrollPeriodMenuPending = true;
    }

    private void SelectActivePeriodOption(DatePeriodMenu menu)
    {
        var active = GetActivePeriodOption(menu);
        if (menu == DatePeriodMenu.Month)
        {
            SelectMonth(active);
        }
        else
        {
            SelectYear(active);
        }
    }

    private void ActivatePeriodOptionByTypeahead(DatePeriodMenu menu, string key)
    {
        var now = Environment.TickCount64;
        if (now - _periodTypeaheadTimestamp > PeriodTypeaheadResetMilliseconds)
        {
            _periodTypeahead = string.Empty;
        }

        var compareInfo = DateCulture.CompareInfo;
        var compareOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        var repeatedSingleCharacter = menu == DatePeriodMenu.Month
            && _periodTypeahead.Length == 1
            && compareInfo.Compare(_periodTypeahead, key, compareOptions) == 0;
        _periodTypeahead = repeatedSingleCharacter ? key : _periodTypeahead + key;
        _periodTypeaheadTimestamp = now;

        if (!TryActivatePeriodOptionByPrefix(menu, _periodTypeahead))
        {
            _periodTypeahead = key;
            TryActivatePeriodOptionByPrefix(menu, key);
        }
    }

    private bool TryActivatePeriodOptionByPrefix(DatePeriodMenu menu, string prefix)
    {
        var options = GetPeriodOptions(menu);
        var activeIndex = GetPeriodOptionIndex(options, GetActivePeriodOption(menu));
        var startIndex = prefix.Length == 1 ? activeIndex + 1 : 0;
        var compareInfo = DateCulture.CompareInfo;
        var compareOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        for (var offset = 0; offset < options.Count; offset++)
        {
            var index = (startIndex + offset) % options.Count;
            var option = options[index];
            if (!compareInfo.IsPrefix(GetPeriodOptionText(menu, option), prefix, compareOptions))
            {
                continue;
            }

            SetActivePeriodOption(menu, option);
            _scrollPeriodMenuPending = true;
            return true;
        }

        return false;
    }

    private IReadOnlyList<int> GetPeriodOptions(DatePeriodMenu menu) =>
        menu == DatePeriodMenu.Month ? AvailableMonths : AvailableYears;

    private int GetActivePeriodOption(DatePeriodMenu menu) =>
        menu == DatePeriodMenu.Month ? _activeMonth : _activeYear;

    private int GetViewPeriodOption(DatePeriodMenu menu) =>
        menu == DatePeriodMenu.Month ? _viewMonth.Month : _viewMonth.Year;

    private void SetActivePeriodOption(DatePeriodMenu menu, int value)
    {
        if (menu == DatePeriodMenu.Month)
        {
            _activeMonth = value;
        }
        else
        {
            _activeYear = value;
        }
    }

    private string GetPeriodOptionText(DatePeriodMenu menu, int value) =>
        menu == DatePeriodMenu.Month ? GetMonthName(value) : value.ToString(DateCulture);

    private static int GetPeriodPageSize(DatePeriodMenu menu) =>
        menu == DatePeriodMenu.Month ? 3 : 10;

    private static int GetPeriodOptionIndex(IReadOnlyList<int> options, int active)
    {
        for (var index = 0; index < options.Count; index++)
        {
            if (options[index] == active)
            {
                return index;
            }
        }

        return 0;
    }

    private static bool IsPeriodTypeaheadKey(KeyboardEventArgs args) =>
        args.Key.Length == 1
        && !char.IsControl(args.Key[0])
        && !args.AltKey
        && !args.CtrlKey
        && !args.MetaKey;

    private void ResetPeriodTypeahead()
    {
        _periodTypeahead = string.Empty;
        _periodTypeaheadTimestamp = 0;
    }

    private void MoveFocusedDate(int days)
    {
        ApplyCalendarState(BzsDateCalendarMath.MoveFocusedDate(
            _focusedDate,
            days,
            FirstAllowedDate,
            LastAllowedDate));
        _focusDayPending = true;
    }

    private void MoveFocusedDateByMonth(int months)
    {
        ApplyCalendarState(BzsDateCalendarMath.MoveFocusedDateByMonth(
            _focusedDate,
            months,
            FirstAllowedDate,
            LastAllowedDate));
        _focusDayPending = true;
    }

    private void ShiftViewMonth(int months)
    {
        _openPeriodMenu = null;
        ApplyCalendarState(BzsDateCalendarMath.ShiftViewMonth(
            _viewMonth,
            _focusedDate,
            months,
            FirstAllowedDate,
            LastAllowedDate));
    }

    private void SelectMonth(int month) => SetViewMonth(_viewMonth.Year, month);

    private void SelectYear(int year) => SetViewMonth(year, _viewMonth.Month);

    private void ActivateMonth(int month) => _activeMonth = month;

    private void ActivateYear(int year) => _activeYear = year;

    private void SetViewMonth(int year, int month)
    {
        _openPeriodMenu = null;
        ApplyCalendarState(BzsDateCalendarMath.SetViewMonth(
            new DateOnly(year, month, 1),
            _focusedDate,
            FirstAllowedDate,
            LastAllowedDate));
    }

    private void ApplyCalendarState(BzsDateCalendarState state)
    {
        _viewMonth = state.ViewMonth;
        _focusedDate = state.FocusedDate;
    }

    private void FocusDate(DateOnly date) => _focusedDate = date;

    private async Task SelectDateAsync(DateOnly date)
    {
        if (!IsDateAllowed(date))
        {
            return;
        }

        CurrentValueAsString = FormatValueAsString(BzsDateValueAdapter<TValue>.CreateValue(date, CurrentValue));
        _focusedDate = date;
        _viewMonth = BzsDateCalendarMath.FirstOfMonth(date);
        await CloseAsync(true);
    }

    private Task SelectTodayAsync() => SelectDateAsync(Today);

    private async Task ClearAsync()
    {
        CurrentValueAsString = string.Empty;
        _focusedDate = BzsDateCalendarMath.ClampDate(Today, FirstAllowedDate, LastAllowedDate);
        _viewMonth = BzsDateCalendarMath.FirstOfMonth(_focusedDate);
        await CloseAsync(true);
    }

    private bool TryParseDateValue(string? value, out TValue result) => BzsDateValueAdapter<TValue>.TryParse(
        value,
        EffectiveDateFormat,
        DateCulture,
        Culture is null ? CultureInfo.CurrentCulture : null,
        CurrentValue,
        out result);

    private string GetMonthName(int month) => DateCulture.DateTimeFormat.GetMonthName(month);

    private string GetMonthOptionId(int month) => $"{MonthListboxId}-{month}";

    private string GetYearOptionId(int year) => $"{YearListboxId}-{year}";

    private static string GetPeriodOptionClass(bool selected, bool active) => string.Join(" ", new[]
    {
        "bzs-date-picker__period-option",
        selected ? "bzs-date-picker__period-option--selected" : null,
        active ? "bzs-date-picker__period-option--active" : null,
    }.Where(static value => value is not null));

    private string FormatDateValidationError(string resourceKey) =>
        Culture is null
            ? FormatValidationError(resourceKey)
            : LocalizeDatePicker(resourceKey, DisplayName ?? FieldIdentifier.FieldName);

    private string LocalizeDatePicker(string resourceKey, params object[] arguments)
    {
        if (Culture is null)
        {
            return Localize(resourceKey, arguments);
        }

        var value = DatePickerResources.GetString(resourceKey, Culture) ?? resourceKey;
        return arguments.Length == 0 ? value : string.Format(Culture, value, arguments);
    }

    private string FormatAccessibleDate(DateOnly date) => date.ToString("D", DateCulture);

    private static string FormatNativeDate(DateOnly date) => date.ToString(NativeDateFormat, CultureInfo.InvariantCulture);

    private string GetDayClass(BzsDateCalendarDay day) => string.Join(" ", new[]
    {
        "bzs-date-picker__day",
        day.IsInViewMonth ? null : "bzs-date-picker__day--outside",
        day.IsToday ? "bzs-date-picker__day--today" : null,
        day.IsSelected ? "bzs-date-picker__day--selected" : null,
    }.Where(static value => value is not null));

    private bool IsDateAllowed(DateOnly date) => BzsDateCalendarMath.IsDateAllowed(
        date,
        FirstAllowedDate,
        LastAllowedDate);

    /// <summary>Closes the calendar after an outside pointer interaction.</summary>
    [JSInvokable]
    public Task CloseFromBrowserAsync() => InvokeAsync(() =>
    {
        if (!_isOpen)
        {
            return;
        }

        SetClosedState();
        _openSyncPending = false;
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
        _interopLifetimeCancellation.Cancel();
        try
        {
            _openRequested = false;
            _isOpen = false;
            if (_interopInitializationTask is not null)
            {
                try
                {
                    await _interopInitializationTask;
                }
                catch
                {
                    // The render lifecycle owns reporting initialization failures.
                }
            }

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
            _dotNetReference?.Dispose();
            _dotNetReference = null;

            if (disposalException is not null)
            {
                ExceptionDispatchInfo.Capture(disposalException).Throw();
            }
        }
        finally
        {
            _interopLifetimeCancellation.Dispose();
            ((IDisposable)this).Dispose();
        }
    }

    private sealed record CalendarWeekday(string ShortName, string FullName);

    private enum DatePeriodMenu
    {
        Month,
        Year,
    }

}
