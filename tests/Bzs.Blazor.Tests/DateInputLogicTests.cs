using System.Globalization;

namespace Bzs.Blazor.Tests;

public sealed class DateInputLogicTests
{
    [Fact]
    public void ValueAdaptersConvertLeapDatesForEverySupportedType()
    {
        var leapDate = new DateOnly(2024, 2, 29);
        var currentOffsetValue = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(8));

        var dateOnly = BzsDateValueAdapter<DateOnly>.CreateValue(leapDate);
        var dateTime = BzsDateValueAdapter<DateTime>.CreateValue(leapDate);
        var dateTimeOffset = BzsDateValueAdapter<DateTimeOffset>.CreateValue(leapDate, currentOffsetValue);

        Assert.Equal(leapDate, dateOnly);
        Assert.Equal(leapDate.ToDateTime(TimeOnly.MinValue), dateTime);
        Assert.Equal(leapDate.ToDateTime(TimeOnly.MinValue), dateTimeOffset.DateTime);
        Assert.Equal(currentOffsetValue.Offset, dateTimeOffset.Offset);
        Assert.True(BzsDateValueAdapter<DateTimeOffset>.TryGetDate(dateTimeOffset, out var convertedDate));
        Assert.Equal(leapDate, convertedDate);
    }

    [Fact]
    public void ValueAdaptersIdentifyNullableAndUnsupportedValues()
    {
        Assert.True(BzsDateValueAdapter<DateOnly?>.IsSupported);
        Assert.True(BzsDateValueAdapter<DateOnly?>.IsNullable);
        Assert.False(BzsDateValueAdapter<DateOnly?>.TryGetDate(null, out _));

        var nullableDate = BzsDateValueAdapter<DateOnly?>.CreateValue(new DateOnly(2024, 2, 29));
        Assert.Equal(new DateOnly(2024, 2, 29), nullableDate);

        Assert.False(BzsDateValueAdapter<string>.IsSupported);
        Assert.False(BzsDateValueAdapter<string>.IsNullable);
        Assert.False(BzsDateValueAdapter<string>.TryGetDate("2024-02-29", out _));
    }

    [Fact]
    public void ValueAdapterParsesLeapDateUsingTheSuppliedCultureAndFormat()
    {
        var culture = new CultureInfo("fr-FR");

        var parsed = BzsDateValueAdapter<DateOnly>.TryParse(
            "29/02/2024",
            "dd/MM/yyyy",
            culture,
            fallbackCulture: null,
            currentValue: default,
            out var value);

        Assert.True(parsed);
        Assert.Equal(new DateOnly(2024, 2, 29), value);
    }

    [Fact]
    public void GregorianCultureNormalizationPreservesLocalization()
    {
        var culture = new CultureInfo("ar-SA");

        var normalized = BzsDateCalendarMath.CreateGregorianCulture(culture);

        Assert.IsType<GregorianCalendar>(normalized.DateTimeFormat.Calendar);
        Assert.Equal(culture.Name, normalized.Name);
        Assert.True(normalized.IsReadOnly);
    }

    [Fact]
    public void DateRangeComparisonsIncludeBothBoundaries()
    {
        var min = new DateOnly(2024, 2, 10);
        var max = new DateOnly(2024, 3, 20);

        Assert.True(BzsDateCalendarMath.IsDateAllowed(min, min, max));
        Assert.True(BzsDateCalendarMath.IsDateAllowed(max, min, max));
        Assert.False(BzsDateCalendarMath.IsDateAllowed(min.AddDays(-1), min, max));
        Assert.False(BzsDateCalendarMath.IsDateAllowed(max.AddDays(1), min, max));
        Assert.True(BzsDateCalendarMath.MonthIntersectsRange(2024, 2, min, max));
        Assert.True(BzsDateCalendarMath.MonthIntersectsRange(2024, 3, min, max));
        Assert.False(BzsDateCalendarMath.MonthIntersectsRange(2024, 1, min, max));
        Assert.False(BzsDateCalendarMath.MonthIntersectsRange(2024, 4, min, max));
    }

    [Fact]
    public void CalendarGridAlwaysCoversSixWeeksAndMarksState()
    {
        var selected = new DateOnly(2024, 2, 29);
        var focused = new DateOnly(2024, 2, 1);
        var days = BzsDateCalendarMath.CreateCalendarGrid(
            new DateOnly(2024, 2, 15),
            DayOfWeek.Monday,
            selected,
            selected,
            focused,
            new DateOnly(2024, 2, 1),
            new DateOnly(2024, 2, 29));

        Assert.Equal(42, days.Count);
        Assert.Equal(new DateOnly(2024, 1, 29), days[0].Date);
        Assert.Equal(new DateOnly(2024, 3, 10), days[^1].Date);
        Assert.True(days.Single(day => day.Date == selected).IsSelected);
        Assert.True(days.Single(day => day.Date == selected).IsToday);
        Assert.True(days.Single(day => day.Date == focused).IsFocused);
        Assert.True(days[0].IsDisabled);
        Assert.False(days.Single(day => day.Date == selected).IsDisabled);
    }

    [Fact]
    public void CalendarGridHandlesDateOnlyBoundariesWithoutOverflow()
    {
        var firstGrid = BzsDateCalendarMath.CreateCalendarGrid(
            DateOnly.MinValue,
            DayOfWeek.Sunday,
            DateOnly.MinValue,
            DateOnly.MinValue,
            DateOnly.MinValue,
            DateOnly.MinValue,
            DateOnly.MaxValue);
        var lastGrid = BzsDateCalendarMath.CreateCalendarGrid(
            DateOnly.MaxValue,
            DayOfWeek.Monday,
            DateOnly.MaxValue,
            DateOnly.MaxValue,
            DateOnly.MaxValue,
            DateOnly.MinValue,
            DateOnly.MaxValue);

        Assert.Equal(42, firstGrid.Count);
        Assert.Null(firstGrid[0].Date);
        Assert.Equal(DateOnly.MinValue, firstGrid[1].Date);
        Assert.Equal(42, lastGrid.Count);
        Assert.Equal(DateOnly.MaxValue, lastGrid.Single(day => day.Date == DateOnly.MaxValue).Date);
        Assert.All(lastGrid.SkipWhile(day => day.Date != DateOnly.MaxValue).Skip(1), day => Assert.Null(day.Date));
    }

    [Fact]
    public void MonthNavigationClampsLeapDaysAndAllowedRange()
    {
        var unbounded = BzsDateCalendarMath.ShiftViewMonth(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31),
            1,
            DateOnly.MinValue,
            DateOnly.MaxValue);
        var followingYear = BzsDateCalendarMath.MoveFocusedDateByMonth(
            new DateOnly(2024, 2, 29),
            12,
            DateOnly.MinValue,
            DateOnly.MaxValue);
        var selectedYear = BzsDateCalendarMath.SetViewMonth(
            new DateOnly(2025, 2, 1),
            new DateOnly(2024, 2, 29),
            DateOnly.MinValue,
            DateOnly.MaxValue);
        var minimumClamped = BzsDateCalendarMath.ShiftViewMonth(
            new DateOnly(2024, 4, 1),
            new DateOnly(2024, 4, 10),
            -12,
            new DateOnly(2024, 3, 15),
            new DateOnly(2024, 5, 10));
        var maximumClamped = BzsDateCalendarMath.SetViewMonth(
            new DateOnly(2025, 12, 1),
            new DateOnly(2024, 4, 30),
            new DateOnly(2024, 3, 15),
            new DateOnly(2024, 5, 10));

        Assert.Equal(new DateOnly(2024, 2, 1), unbounded.ViewMonth);
        Assert.Equal(new DateOnly(2024, 2, 29), unbounded.FocusedDate);
        Assert.Equal(new DateOnly(2025, 2, 1), followingYear.ViewMonth);
        Assert.Equal(new DateOnly(2025, 2, 28), followingYear.FocusedDate);
        Assert.Equal(new DateOnly(2025, 2, 1), selectedYear.ViewMonth);
        Assert.Equal(new DateOnly(2025, 2, 28), selectedYear.FocusedDate);
        Assert.Equal(new DateOnly(2024, 3, 1), minimumClamped.ViewMonth);
        Assert.Equal(new DateOnly(2024, 3, 15), minimumClamped.FocusedDate);
        Assert.Equal(new DateOnly(2024, 5, 1), maximumClamped.ViewMonth);
        Assert.Equal(new DateOnly(2024, 5, 10), maximumClamped.FocusedDate);
    }
}
