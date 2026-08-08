using System.Globalization;

namespace Bzs.Blazor;

internal static class BzsDateValueAdapter<TValue>
{
    private const string NativeDateFormat = "yyyy-MM-dd";
    private static readonly Type ValueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    internal static bool IsSupported => ValueType == typeof(DateOnly)
        || ValueType == typeof(DateTime)
        || ValueType == typeof(DateTimeOffset);

    internal static bool IsNullable => Nullable.GetUnderlyingType(typeof(TValue)) is not null;

    internal static bool TryGetDate(TValue? value, out DateOnly date)
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

    internal static bool TryParse(
        string? value,
        string dateFormat,
        CultureInfo dateCulture,
        CultureInfo? fallbackCulture,
        TValue? currentValue,
        out TValue result)
    {
        var styles = DateTimeStyles.AllowWhiteSpaces;
        if (DateOnly.TryParseExact(value, NativeDateFormat, CultureInfo.InvariantCulture, styles, out var date)
            || DateOnly.TryParseExact(value, dateFormat, dateCulture, styles, out date)
            || DateOnly.TryParse(value, dateCulture, styles, out date)
            || (fallbackCulture is not null
                && (DateOnly.TryParse(value, fallbackCulture, styles, out date)
                    || DateOnly.TryParse(value, CultureInfo.InvariantCulture, styles, out date))))
        {
            result = CreateValue(date, currentValue);
            return true;
        }

        result = default!;
        return false;
    }

    internal static TValue CreateValue(DateOnly date, TValue? currentValue = default)
    {
        if (ValueType == typeof(DateOnly))
        {
            return (TValue)(object)date;
        }

        if (ValueType == typeof(DateTime))
        {
            return (TValue)(object)date.ToDateTime(TimeOnly.MinValue);
        }

        return (TValue)(object)CreateDateTimeOffset(date, currentValue);
    }

    private static DateTimeOffset CreateDateTimeOffset(DateOnly date, TValue? currentValue)
    {
        var localDateTime = date.ToDateTime(TimeOnly.MinValue);
        var offset = currentValue is DateTimeOffset current ? current.Offset : TimeSpan.Zero;
        var utcTicks = localDateTime.Ticks - offset.Ticks;
        if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks)
        {
            offset = TimeSpan.Zero;
        }
        return new DateTimeOffset(localDateTime, offset);
    }
}
