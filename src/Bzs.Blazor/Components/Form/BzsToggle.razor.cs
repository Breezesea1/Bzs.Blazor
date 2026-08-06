using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a compact switch backed by a native checkbox and integrated with EditContext.</summary>
public sealed partial class BzsToggle : BzsInputBase<bool>
{
    /// <summary>Gets or sets optional text displayed inside the switch when enabled.</summary>
    [Parameter]
    public string? OnText { get; set; }

    /// <summary>Gets or sets optional text displayed inside the switch when disabled.</summary>
    [Parameter]
    public string? OffText { get; set; }

    private string? CurrentText
    {
        get
        {
            var text = CurrentValue ? OnText : OffText;
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
    }

    private IReadOnlyDictionary<string, object> InputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-toggle__input", "checkbox", supportsReadOnly: false),
                StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "switch",
                ["aria-checked"] = CurrentValue ? "true" : "false",
            };

            return attributes;
        }
    }

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
