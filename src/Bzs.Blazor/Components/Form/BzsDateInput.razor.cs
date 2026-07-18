using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a generic native date input integrated with EditContext.</summary>
public partial class BzsDateInput<TValue> : BzsInputBase<TValue>
    where TValue : struct, IParsable<TValue>, IFormattable
{
    private const string DateFormat = "yyyy-MM-dd";
    private IReadOnlyDictionary<string, object> InputAttributes => BuildInputAttributes("bzs-input bzs-date-input", "date");

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue value) => value switch
    {
        DateOnly date => date.ToString(DateFormat, CultureInfo.InvariantCulture),
        DateTime date => date.ToString(DateFormat, CultureInfo.InvariantCulture),
        DateTimeOffset date => date.ToString(DateFormat, CultureInfo.InvariantCulture),
        _ => value.ToString(null, CultureInfo.InvariantCulture),
    };

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (TValue.TryParse(value, CultureInfo.InvariantCulture, out result)
            || TValue.TryParse(value, CultureInfo.CurrentCulture, out result))
        {
            validationErrorMessage = null;
            return true;
        }

        validationErrorMessage = FormatValidationError("FormValidationDate");
        return false;
    }

    private void OnChanged(ChangeEventArgs args)
    {
        if (!Disabled && !ReadOnly)
        {
            CurrentValueAsString = args.Value?.ToString();
        }
    }
}
