using System.Globalization;

namespace Bzs.Blazor;

internal static class BzsDateCalendarMath
{
    private const int CalendarDayCount = 42;

    internal static CultureInfo CreateGregorianCulture(CultureInfo culture)
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

    internal static bool IsDateAllowed(DateOnly date, DateOnly min, DateOnly max) =>
        date >= min && date <= max;

    internal static bool MonthIntersectsRange(int year, int month, DateOnly min, DateOnly max)
    {
        var first = new DateOnly(year, month, 1);
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return last >= min && first <= max;
    }

    internal static IReadOnlyList<int> GetAvailableMonths(int year, DateOnly min, DateOnly max) =>
        Enumerable.Range(1, 12)
            .Where(month => MonthIntersectsRange(year, month, min, max))
            .ToArray();

    internal static IReadOnlyList<int> GetAvailableYears(int viewYear, DateOnly min, DateOnly max)
    {
        var centerYear = Math.Clamp(viewYear, min.Year, max.Year);
        var firstYear = Math.Max(min.Year, centerYear - 50);
        var lastYear = Math.Min(max.Year, centerYear + 50);
        return Enumerable.Range(firstYear, lastYear - firstYear + 1).ToArray();
    }

    internal static IReadOnlyList<BzsDateCalendarDay> CreateCalendarGrid(
        DateOnly viewMonth,
        DayOfWeek firstDayOfWeek,
        DateOnly today,
        DateOnly? selectedDate,
        DateOnly focusedDate,
        DateOnly min,
        DateOnly max)
    {
        var normalizedViewMonth = FirstOfMonth(viewMonth);
        var offset = ((int)normalizedViewMonth.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        var startDayNumber = normalizedViewMonth.DayNumber - offset;
        var days = new BzsDateCalendarDay[CalendarDayCount];

        for (var index = 0; index < days.Length; index++)
        {
            var dayNumber = startDayNumber + index;
            if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
            {
                days[index] = new(null, false, false, false, false, true);
                continue;
            }

            var date = DateOnly.FromDayNumber(dayNumber);
            days[index] = new(
                date,
                date.Month == normalizedViewMonth.Month,
                date == today,
                selectedDate == date,
                date == focusedDate,
                !IsDateAllowed(date, min, max));
        }

        return days;
    }

    internal static DateOnly ClampDate(DateOnly date, DateOnly min, DateOnly max) => date < min
        ? min
        : date > max
            ? max
            : date;

    internal static DateOnly ClampMonth(DateOnly month, DateOnly min, DateOnly max)
    {
        var normalizedMonth = FirstOfMonth(month);
        var firstAllowedMonth = FirstOfMonth(min);
        var lastAllowedMonth = FirstOfMonth(max);
        return normalizedMonth < firstAllowedMonth
            ? firstAllowedMonth
            : normalizedMonth > lastAllowedMonth
                ? lastAllowedMonth
                : normalizedMonth;
    }

    internal static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    internal static DateOnly AddMonths(DateOnly month, int offset)
    {
        var monthIndex = Math.Clamp((long)month.Year * 12 + month.Month - 1 + offset, 12L, 120_000L - 1);
        return new DateOnly((int)(monthIndex / 12), (int)(monthIndex % 12) + 1, 1);
    }

    internal static BzsDateCalendarState MoveFocusedDate(
        DateOnly focusedDate,
        int days,
        DateOnly min,
        DateOnly max)
    {
        var dayNumber = Math.Clamp((long)focusedDate.DayNumber + days, min.DayNumber, max.DayNumber);
        var nextFocusedDate = DateOnly.FromDayNumber((int)dayNumber);
        return new(FirstOfMonth(nextFocusedDate), nextFocusedDate);
    }

    internal static BzsDateCalendarState MoveFocusedDateByMonth(
        DateOnly focusedDate,
        int months,
        DateOnly min,
        DateOnly max)
    {
        var targetMonth = AddMonths(FirstOfMonth(focusedDate), months);
        var targetDate = WithDay(targetMonth, focusedDate.Day);
        var nextFocusedDate = ClampDate(targetDate, min, max);
        return new(FirstOfMonth(nextFocusedDate), nextFocusedDate);
    }

    internal static BzsDateCalendarState ShiftViewMonth(
        DateOnly viewMonth,
        DateOnly focusedDate,
        int months,
        DateOnly min,
        DateOnly max) => SetViewMonth(AddMonths(viewMonth, months), focusedDate, min, max);

    internal static BzsDateCalendarState SetViewMonth(
        DateOnly viewMonth,
        DateOnly focusedDate,
        DateOnly min,
        DateOnly max)
    {
        var nextViewMonth = ClampMonth(viewMonth, min, max);
        var nextFocusedDate = ClampDate(WithDay(nextViewMonth, focusedDate.Day), min, max);
        return new(nextViewMonth, nextFocusedDate);
    }

    private static DateOnly WithDay(DateOnly month, int day) => new(
        month.Year,
        month.Month,
        Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)));
}

internal sealed record BzsDateCalendarState(DateOnly ViewMonth, DateOnly FocusedDate);

internal sealed record BzsDateCalendarDay(
    DateOnly? Date,
    bool IsInViewMonth,
    bool IsToday,
    bool IsSelected,
    bool IsFocused,
    bool IsDisabled = false);
