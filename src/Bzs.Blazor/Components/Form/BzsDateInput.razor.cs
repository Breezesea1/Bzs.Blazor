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
public partial class BzsDateInput<TValue> : BzsInputBase<TValue>
{
    private const string NativeDateFormat = "yyyy-MM-dd";
    private const int ImmediateOpenSyncAttemptLimit = 2;
    private const int PeriodTypeaheadResetMilliseconds = 2_000;
    private static readonly DateOnly[] DateFormatValidationDates =
    [
        new(2000, 2, 29),
        new(2099, 11, 23),
    ];
    private static readonly Type ValueDateType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
    private static readonly bool IsSupportedDateType = ValueDateType == typeof(DateOnly)
        || ValueDateType == typeof(DateTime)
        || ValueDateType == typeof(DateTimeOffset);
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
    private DateOnly _viewMonth = FirstOfMonth(DateOnly.FromDateTime(DateTime.Today));
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
                _dateCulture = CreateDateCulture(source);
            }
            return _dateCulture!;
        }
    }
    private DateOnly Today => _today;
    private DateOnly FirstAllowedDate => Min ?? DateOnly.MinValue;
    private DateOnly LastAllowedDate => Max ?? DateOnly.MaxValue;
    private DayOfWeek FirstDayOfWeek => DateCulture.DateTimeFormat.FirstDayOfWeek;
    private string? NativeValueAsString => TryGetDate(CurrentValue, out var date) ? FormatNativeDate(date) : null;
    private string ViewMonthAccessibleLabel => _viewMonth.ToString("Y", DateCulture);
    private bool CanNavigatePreviousMonth => _viewMonth > FirstOfMonth(FirstAllowedDate);
    private bool CanNavigateNextMonth => _viewMonth < FirstOfMonth(LastAllowedDate);
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

    private IReadOnlyList<int> AvailableMonths => Enumerable.Range(1, 12)
        .Where(month => MonthIntersectsRange(_viewMonth.Year, month))
        .ToArray();

    private IReadOnlyList<int> AvailableYears
    {
        get
        {
            var centerYear = Math.Clamp(_viewMonth.Year, FirstAllowedDate.Year, LastAllowedDate.Year);
            var firstYear = Math.Max(FirstAllowedDate.Year, centerYear - 50);
            var lastYear = Math.Min(LastAllowedDate.Year, centerYear + 50);
            return Enumerable.Range(firstYear, lastYear - firstYear + 1).ToArray();
        }
    }

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

    private IReadOnlyList<CalendarDay> CalendarDays
    {
        get
        {
            var offset = ((int)_viewMonth.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;
            var startDayNumber = _viewMonth.DayNumber - offset;
            var selectedDate = TryGetDate(CurrentValue, out var selected) ? selected : (DateOnly?)null;
            var days = new CalendarDay[42];

            for (var index = 0; index < days.Length; index++)
            {
                var dayNumber = startDayNumber + index;
                if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
                {
                    days[index] = new CalendarDay(null, false, false, false, false, true);
                    continue;
                }

                var date = DateOnly.FromDayNumber(dayNumber);
                days[index] = new CalendarDay(
                    date,
                    date.Month == _viewMonth.Month,
                    date == Today,
                    selectedDate == date,
                    date == _focusedDate,
                    !IsDateAllowed(date));
            }

            return days;
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
            _focusedDate = ClampToAllowedRange(_focusedDate);
            _viewMonth = ClampMonthToAllowedRange(_viewMonth);
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

            _interop ??= new BzsDateInputInterop(JsRuntime);
            _dotNetReference ??= DotNetObjectReference.Create(this);
            _interopInitializationPending = true;
            BzsDateInputInitialization initialization;
            try
            {
                _interopInitializationTask = _interop.InitializeAsync(
                    _instanceId,
                    _rootReference,
                    _dotNetReference).AsTask();
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
        if (!TryGetDate(value, out var date))
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
        if (string.IsNullOrWhiteSpace(value) && Nullable.GetUnderlyingType(typeof(TValue)) is not null)
        {
            result = default!;
            validationErrorMessage = null;
            return true;
        }

        if (TryParseDateValue(value, out result)
            && TryGetDate(result, out var date))
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
        if (!IsSupportedDateType)
        {
            throw new InvalidOperationException(
                $"{nameof(BzsDateInput<TValue>)} supports DateOnly, DateTime, DateTimeOffset, and their nullable forms.");
        }

        if (Min > Max)
        {
            throw new InvalidOperationException($"{nameof(BzsDateInput<TValue>)} requires Min to be earlier than or equal to Max.");
        }

        if (Clearable && Nullable.GetUnderlyingType(typeof(TValue)) is null)
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
        var reference = TryGetDate(CurrentValue, out var selected) ? selected : Today;
        _focusedDate = ClampToAllowedRange(reference);
        _viewMonth = FirstOfMonth(_focusedDate);
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
        if (TryParseDateValue(text, out var value) && TryGetDate(value, out var date) && IsDateAllowed(date))
        {
            _focusedDate = date;
            _viewMonth = FirstOfMonth(date);
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
        var dayNumber = Math.Clamp(
            (long)_focusedDate.DayNumber + days,
            FirstAllowedDate.DayNumber,
            LastAllowedDate.DayNumber);
        _focusedDate = DateOnly.FromDayNumber((int)dayNumber);
        _viewMonth = FirstOfMonth(_focusedDate);
        _focusDayPending = true;
    }

    private void MoveFocusedDateByMonth(int months)
    {
        var targetMonth = Math.Clamp(
            (long)_focusedDate.Year * 12 + _focusedDate.Month - 1 + months,
            12L,
            120_000L - 1);
        var year = (int)(targetMonth / 12);
        var month = (int)(targetMonth % 12) + 1;
        var day = Math.Min(_focusedDate.Day, DateTime.DaysInMonth(year, month));
        _focusedDate = ClampToAllowedRange(new DateOnly(year, month, day));
        _viewMonth = FirstOfMonth(_focusedDate);
        _focusDayPending = true;
    }

    private void ShiftViewMonth(int months)
    {
        _openPeriodMenu = null;
        var target = AddMonths(_viewMonth, months);
        _viewMonth = ClampMonthToAllowedRange(target);
        _focusedDate = ClampToAllowedRange(new DateOnly(
            _viewMonth.Year,
            _viewMonth.Month,
            Math.Min(_focusedDate.Day, DateTime.DaysInMonth(_viewMonth.Year, _viewMonth.Month))));
    }

    private void SelectMonth(int month) => SetViewMonth(_viewMonth.Year, month);

    private void SelectYear(int year) => SetViewMonth(year, _viewMonth.Month);

    private void ActivateMonth(int month) => _activeMonth = month;

    private void ActivateYear(int year) => _activeYear = year;

    private void SetViewMonth(int year, int month)
    {
        _openPeriodMenu = null;
        _viewMonth = ClampMonthToAllowedRange(new DateOnly(year, month, 1));
        _focusedDate = ClampToAllowedRange(new DateOnly(
            _viewMonth.Year,
            _viewMonth.Month,
            Math.Min(_focusedDate.Day, DateTime.DaysInMonth(_viewMonth.Year, _viewMonth.Month))));
    }

    private void FocusDate(DateOnly date) => _focusedDate = date;

    private async Task SelectDateAsync(DateOnly date)
    {
        if (!IsDateAllowed(date))
        {
            return;
        }

        CurrentValueAsString = FormatValueAsString(CreateValue(date));
        _focusedDate = date;
        _viewMonth = FirstOfMonth(date);
        await CloseAsync(true);
    }

    private Task SelectTodayAsync() => SelectDateAsync(Today);

    private async Task ClearAsync()
    {
        CurrentValueAsString = string.Empty;
        _focusedDate = ClampToAllowedRange(Today);
        _viewMonth = FirstOfMonth(_focusedDate);
        await CloseAsync(true);
    }

    private bool TryParseDateValue(string? value, out TValue result)
    {
        var culture = DateCulture;
        var styles = DateTimeStyles.AllowWhiteSpaces;
        if (DateOnly.TryParseExact(value, NativeDateFormat, CultureInfo.InvariantCulture, styles, out var date)
            || DateOnly.TryParseExact(value, EffectiveDateFormat, culture, styles, out date)
            || DateOnly.TryParse(value, culture, styles, out date)
            || (Culture is null
                && (DateOnly.TryParse(value, CultureInfo.CurrentCulture, styles, out date)
                    || DateOnly.TryParse(value, CultureInfo.InvariantCulture, styles, out date))))
        {
            result = CreateValue(date);
            return true;
        }

        result = default!;
        return false;
    }

    private static bool TryGetDate(TValue? value, out DateOnly date)
    {
        switch (value)
        {
            case DateOnly dateOnly:
                date = dateOnly;
                return true;
            case DateTime dateTime:
                date = DateOnly.FromDateTime(dateTime);
                return true;
            case DateTimeOffset dateTimeOffset:
                date = DateOnly.FromDateTime(dateTimeOffset.DateTime);
                return true;
            default:
                date = default;
                return false;
        }
    }

    private TValue CreateValue(DateOnly date)
    {
        object value = ValueDateType == typeof(DateOnly)
            ? date
            : ValueDateType == typeof(DateTime)
                ? date.ToDateTime(TimeOnly.MinValue)
                : CreateDateTimeOffset(date);
        return (TValue)value;
    }

    private DateTimeOffset CreateDateTimeOffset(DateOnly date)
    {
        var localDateTime = date.ToDateTime(TimeOnly.MinValue);
        var offset = CurrentValue is DateTimeOffset current ? current.Offset : TimeSpan.Zero;
        var utcTicks = localDateTime.Ticks - offset.Ticks;
        if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks)
        {
            offset = TimeSpan.Zero;
        }
        return new DateTimeOffset(localDateTime, offset);
    }

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

    private static CultureInfo CreateDateCulture(CultureInfo culture)
    {
        if (culture.DateTimeFormat.Calendar is GregorianCalendar)
        {
            return culture;
        }

        var gregorianCalendar = culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
        if (gregorianCalendar is null)
        {
            return CultureInfo.InvariantCulture;
        }

        var localizedGregorianCulture = (CultureInfo)culture.Clone();
        localizedGregorianCulture.DateTimeFormat.Calendar = gregorianCalendar;
        return CultureInfo.ReadOnly(localizedGregorianCulture);
    }

    private string GetDayClass(CalendarDay day) => string.Join(" ", new[]
    {
        "bzs-date-picker__day",
        day.IsInViewMonth ? null : "bzs-date-picker__day--outside",
        day.IsToday ? "bzs-date-picker__day--today" : null,
        day.IsSelected ? "bzs-date-picker__day--selected" : null,
    }.Where(static value => value is not null));

    private bool IsDateAllowed(DateOnly date) => date >= FirstAllowedDate && date <= LastAllowedDate;

    private bool MonthIntersectsRange(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return last >= FirstAllowedDate && first <= LastAllowedDate;
    }

    private DateOnly ClampToAllowedRange(DateOnly date) => date < FirstAllowedDate
        ? FirstAllowedDate
        : date > LastAllowedDate
            ? LastAllowedDate
            : date;

    private DateOnly ClampMonthToAllowedRange(DateOnly month) => month < FirstOfMonth(FirstAllowedDate)
        ? FirstOfMonth(FirstAllowedDate)
        : month > FirstOfMonth(LastAllowedDate)
            ? FirstOfMonth(LastAllowedDate)
            : month;

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private static DateOnly AddMonths(DateOnly month, int offset)
    {
        var monthIndex = Math.Clamp((long)month.Year * 12 + month.Month - 1 + offset, 12L, 120_000L - 1);
        return new DateOnly((int)(monthIndex / 12), (int)(monthIndex % 12) + 1, 1);
    }

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
            ((IDisposable)this).Dispose();
        }
    }

    private sealed record CalendarWeekday(string ShortName, string FullName);

    private enum DatePeriodMenu
    {
        Month,
        Year,
    }

    private sealed record CalendarDay(
        DateOnly? Date,
        bool IsInViewMonth,
        bool IsToday,
        bool IsSelected,
        bool IsFocused,
        bool IsDisabled = false);
}
