using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a native checkbox integrated with EditContext.</summary>
public partial class BzsCheckbox : BzsInputBase<bool>
{
    private IReadOnlyDictionary<string, object> InputAttributes =>
        BuildInputAttributes("bzs-input bzs-checkbox", "checkbox", supportsReadOnly: false);

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out bool result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (bool.TryParse(value, out result))
        {
            validationErrorMessage = null;
            return true;
        }

        validationErrorMessage = FormatValidationError("FormValidationBoolean");
        return false;
    }

    private void OnChanged(ChangeEventArgs args)
    {
        if (!Disabled && !ReadOnly && args.Value is bool value)
        {
            CurrentValue = value;
        }
    }
}
