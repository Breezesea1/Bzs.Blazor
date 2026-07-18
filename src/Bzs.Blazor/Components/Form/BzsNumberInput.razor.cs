using System.Globalization;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a generic native number input integrated with EditContext.</summary>
public partial class BzsNumberInput<TValue> : BzsInputBase<TValue>
    where TValue : struct, INumber<TValue>
{
    /// <summary>Gets or sets the native number step.</summary>
    [Parameter] public string Step { get; set; } = "any";

    private IReadOnlyDictionary<string, object> InputAttributes => BuildInputAttributes("bzs-input bzs-number-input", "number");

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue value) =>
        value.ToString(null, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (TValue.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
            || TValue.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
        {
            validationErrorMessage = null;
            return true;
        }

        validationErrorMessage = FormatValidationError("FormValidationNumber");
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
