using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a strongly typed segmented radio group integrated with EditContext.</summary>
/// <typeparam name="TValue">The strongly typed option value.</typeparam>
public partial class BzsRadioGroup<TValue> : BzsInputBase<TValue>
{
    /// <summary>Gets or sets the read-only option collection.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<BzsSelectOption<TValue>> Options { get; set; } = [];

    private string FieldLabelId => $"{InputId}-label";
    private int FirstEnabledOptionIndex => Options.ToList().FindIndex(option => !IsOptionDisabled(option));
    private int SelectedEnabledOptionIndex => Options.ToList().FindIndex(option =>
        IsSelected(option.Value) && !IsOptionDisabled(option));
    private int LabelTargetOptionIndex => SelectedEnabledOptionIndex >= 0
        ? SelectedEnabledOptionIndex
        : FirstEnabledOptionIndex;
    private string LabelTargetId => LabelTargetOptionIndex >= 0
        ? GetOptionId(LabelTargetOptionIndex)
        : string.Empty;

    private IReadOnlyDictionary<string, object> GroupAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-radio-group", supportsReadOnly: false),
                StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "radiogroup",
            };

            attributes.Remove("name");
            attributes.Remove("disabled");
            attributes.Remove("required");
            if (!HasAdditionalAccessibleName() && !string.IsNullOrWhiteSpace(Label))
            {
                attributes["aria-labelledby"] = FieldLabelId;
            }

            if (Disabled || ReadOnly)
            {
                attributes["aria-disabled"] = "true";
            }
            else
            {
                attributes.Remove("aria-disabled");
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value) => Options
        .FirstOrDefault(option => EqualityComparer<TValue>.Default.Equals(option.Value, value))
        ?.ValueText;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrEmpty(value) && default(TValue) is null)
        {
            result = default!;
            validationErrorMessage = null;
            return true;
        }

        var option = Options.FirstOrDefault(candidate =>
            string.Equals(candidate.ValueText, value, StringComparison.Ordinal));
        if (option is not null)
        {
            result = option.Value;
            validationErrorMessage = null;
            return true;
        }

        result = default!;
        validationErrorMessage = FormatValidationError("FormValidationSelection");
        return false;
    }

    private string GetOptionId(int index) => $"{InputId}-option-{index}";

    private bool IsSelected(TValue value) =>
        EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    private bool IsOptionDisabled(BzsSelectOption<TValue> option) =>
        Disabled || ReadOnly || option.Disabled;

    private bool IsOptionRequired(int index, BzsSelectOption<TValue> option) =>
        Required && index == FirstEnabledOptionIndex && !IsOptionDisabled(option);

    private bool HasAdditionalAccessibleName() => AdditionalAttributes?.Any(attribute =>
        (attribute.Key.Equals("aria-label", StringComparison.OrdinalIgnoreCase)
            || attribute.Key.Equals("aria-labelledby", StringComparison.OrdinalIgnoreCase))
        && !string.IsNullOrWhiteSpace(attribute.Value?.ToString())) == true;

    private void OnChanged(BzsSelectOption<TValue> option, ChangeEventArgs args)
    {
        if (!Disabled && !ReadOnly && !option.Disabled)
        {
            CurrentValueAsString = args.Value?.ToString();
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Options is null)
        {
            throw new InvalidOperationException("BzsRadioGroup requires an Options collection.");
        }

        var duplicate = Options
            .GroupBy(static option => option.ValueText, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"BzsRadioGroup option ValueText '{duplicate.Key}' must be unique.");
        }

        var duplicateValue = Options
            .GroupBy(static option => option.Value, EqualityComparer<TValue>.Default)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateValue is not null)
        {
            throw new InvalidOperationException("BzsRadioGroup option values must be unique.");
        }
    }
}
