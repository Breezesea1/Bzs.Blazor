using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a native text input integrated with EditContext.</summary>
public partial class BzsTextInput : BzsInputBase<string?>
{
    private IReadOnlyDictionary<string, object> InputAttributes => BuildInputAttributes("bzs-input bzs-text-input", "text");

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out string? result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    private void OnChanged(ChangeEventArgs args)
    {
        if (!Disabled && !ReadOnly)
        {
            CurrentValueAsString = args.Value?.ToString();
        }
    }
}
