using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a native multiline text input integrated with EditContext.</summary>
public partial class BzsTextArea : BzsInputBase<string?>
{
    /// <summary>Gets or sets the visible row count.</summary>
    [Parameter] public int Rows { get; set; } = 4;

    private IReadOnlyDictionary<string, object> InputAttributes => BuildInputAttributes("bzs-input bzs-textarea");

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Rows), Rows, "Rows must be greater than zero.");
        }
    }

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
