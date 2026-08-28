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
    private ElementReference _periodMenuReference;
    private BzsAnchoredOverlaySession? _overlaySession;
    private BzsDatePeriodMenuState? _periodMenuState;
    private BzsDateInputInterop? _interop;
    private Task<BzsDateInputInitialization>? _interopInitializationTask;
    private bool _disposed;
    private bool _isInteractive;
    private bool _interopInitialized;
    private bool _interopInitializationPending;
    private bool _renderAfterInitialization;
    private bool _isOpen;
    private bool _openRequested;
    private bool _focusDayPending;
    private double? _pointerX;
    private double? _pointerY;
    private CultureInfo? _dateCultureSource;
    private CultureInfo? _dateCulture;
    private DateOnly _today = DateOnly.FromDateTime(DateTime.Today);
    private DateOnly _viewMonth = BzsDateCalendarMath.FirstOfMonth(DateOnly.FromDateTime(DateTime.Today));
    private DateOnly _focusedDate = DateOnly.FromDateTime(DateTime.Today);

    private string PanelId => $"{InputId}-calendar";
    private string MonthListboxId => $"{InputId}-month-options";
    private string YearListboxId => $"{InputId}-year-options";
    private string? ActiveMonthOptionId => PeriodMenu.OpenMenu == BzsDatePeriodMenu.Month
        ? GetMonthOptionId(PeriodMenu.ActiveMonth)
        : null;
    private string? ActiveYearOptionId => PeriodMenu.OpenMenu == BzsDatePeriodMenu.Year
        ? GetYearOptionId(PeriodMenu.ActiveYear)
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
    private string IsMonthMenuOpen => PeriodMenu.OpenMenu == BzsDatePeriodMenu.Month ? "true" : "false";
    private string IsYearMenuOpen => PeriodMenu.OpenMenu == BzsDatePeriodMenu.Year ? "true" : "false";

    private BzsDatePeriodMenuState PeriodMenu => _periodMenuState ??= new BzsDatePeriodMenuState(
        menu => GetPeriodOptions(menu),
        (menu, option) => GetPeriodOptionText(menu, option),
        () => DateCulture.CompareInfo);

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
            UpdateOverlayState();
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

        UpdateOverlayState();
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

        await GetOverlaySession().AfterRenderAsync(_rootReference);
        if (_disposed)
        {
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
            _interopInitializationPending = true;
            BzsDateInputInitialization initialization;
            try
            {
                _interopInitializationTask = _interop.InitializeAsync(
                    _instanceId,
                    _rootReference,
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

            if (initialization.BrowserToday is not null)
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

        if (PeriodMenu.OpenMenu is not null && _interop is not null && PeriodMenu.ConsumeScrollPending())
        {
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

    private void SynchronizeOpenPeriodMenu() => PeriodMenu.SynchronizeWithOptions(_viewMonth);

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

    // Opening waits for the browser's current date so "today" is never rendered from the server
    // clock. The request is replayed once initialization reports back.
    private void Open(double? pointerX, double? pointerY, bool focusCalendar)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        _pointerX = pointerX;
        _pointerY = pointerY;
        _focusDayPending = focusCalendar;
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
        UpdateOverlayState();
    }

    private Task Close(bool restoreFocus)
    {
        if (!_isOpen && !_openRequested)
        {
            return Task.CompletedTask;
        }

        if (!_isOpen)
        {
            SetClosedState();
            return Task.CompletedTask;
        }

        // The session owns focus restoration, so it must observe the open state before it flips.
        return GetOverlaySession().RequestCloseAsync(restoreFocus);
    }

    private void SetClosedState()
    {
        _isOpen = false;
        _openRequested = false;
        _pointerX = null;
        _pointerY = null;
        _focusDayPending = false;
        PeriodMenu.Close();
    }

    private BzsAnchoredOverlaySession GetOverlaySession() =>
        _overlaySession ??= new BzsAnchoredOverlaySession(
            JsRuntime,
            HandleOverlayCloseRequestedAsync,
            ImmediateOpenSyncAttemptLimit,
            LoggerFactory);

    private void UpdateOverlayState() =>
        GetOverlaySession().SetDesiredState(new BzsAnchoredOverlayState(
            _isOpen,
            BzsPopoverPlacement.BottomStart,
            CloseOnOutsideInteraction: true,
            CloseOnEscape: true,
            _isOpen && _pointerX is { } x && _pointerY is { } y
                ? new BzsAnchoredOverlayInvocationPoint(x, y)
                : null));

    private Task HandleOverlayCloseRequestedAsync() => InvokeAsync(() =>
    {
        if (!_isOpen)
        {
            return;
        }

        SetClosedState();
        UpdateOverlayState();
        StateHasChanged();
    });

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
            await CloseFromEscapeAsync();
        }
    }

    // Escape closes the innermost open surface: a period menu first, the calendar only once no
    // period menu is left open.
    private Task CloseFromEscapeAsync()
    {
        if (PeriodMenu.OpenMenu is not null)
        {
            PeriodMenu.Close();
            return Task.CompletedTask;
        }

        return Close(true);
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
        args.Key == "Escape" ? CloseFromEscapeAsync() : Task.CompletedTask;

    private void ActivatePeriodMenu(BzsDatePeriodMenu menu, MouseEventArgs args) =>
        ApplyPeriodMenuAction(menu, PeriodMenu.Activate(menu, args.Detail, _viewMonth));

    private async Task HandlePeriodKeyDownAsync(BzsDatePeriodMenu menu, KeyboardEventArgs args)
    {
        var action = PeriodMenu.HandleKey(menu, BzsDatePeriodMenuKey.From(args), _viewMonth);
        if (action == BzsDatePeriodMenuAction.CloseSurfaceRequested)
        {
            await Close(true);
            return;
        }

        ApplyPeriodMenuAction(menu, action);
    }

    private void ApplyPeriodMenuAction(BzsDatePeriodMenu menu, BzsDatePeriodMenuAction action)
    {
        if (action == BzsDatePeriodMenuAction.CommitActive)
        {
            SetViewPeriod(menu, PeriodMenu.ActiveOption(menu));
        }
    }

    private void SetViewPeriod(BzsDatePeriodMenu menu, int option)
    {
        if (menu == BzsDatePeriodMenu.Month)
        {
            SelectMonth(option);
        }
        else
        {
            SelectYear(option);
        }
    }

    private IReadOnlyList<int> GetPeriodOptions(BzsDatePeriodMenu menu) =>
        menu == BzsDatePeriodMenu.Month ? AvailableMonths : AvailableYears;

    private string GetPeriodOptionText(BzsDatePeriodMenu menu, int value) =>
        menu == BzsDatePeriodMenu.Month ? GetMonthName(value) : value.ToString(DateCulture);

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
        PeriodMenu.Close();
        ApplyCalendarState(BzsDateCalendarMath.ShiftViewMonth(
            _viewMonth,
            _focusedDate,
            months,
            FirstAllowedDate,
            LastAllowedDate));
    }

    private void SelectMonth(int month) => SetViewMonth(_viewMonth.Year, month);

    private void SelectYear(int year) => SetViewMonth(year, _viewMonth.Month);

    private void ActivateMonth(int month) => PeriodMenu.Activate(BzsDatePeriodMenu.Month, month);

    private void ActivateYear(int year) => PeriodMenu.Activate(BzsDatePeriodMenu.Year, year);

    private void SetViewMonth(int year, int month)
    {
        PeriodMenu.Close();
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
        await Close(true);
    }

    private Task SelectTodayAsync() => SelectDateAsync(Today);

    private async Task ClearAsync()
    {
        CurrentValueAsString = string.Empty;
        _focusedDate = BzsDateCalendarMath.ClampDate(Today, FirstAllowedDate, LastAllowedDate);
        _viewMonth = BzsDateCalendarMath.FirstOfMonth(_focusedDate);
        await Close(true);
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

    /// <summary>Closes the calendar after a browser-owned outside or Escape interaction.</summary>
    public Task CloseFromBrowserAsync(bool restoreFocus = false)
    {
        if (_disposed || !_isOpen)
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
        _interopLifetimeCancellation.Cancel();
        try
        {
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
}
