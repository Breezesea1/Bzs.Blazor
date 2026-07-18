using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a strongly typed native select integrated with EditContext.</summary>
public partial class BzsSelect<TValue> : BzsInputBase<TValue>
{
    /// <summary>Gets or sets the read-only option collection.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<BzsSelectOption<TValue>> Options { get; set; } = [];

    /// <summary>Gets or sets the optional empty option label.</summary>
    [Parameter] public string? PlaceholderOption { get; set; }

    private IReadOnlyDictionary<string, object> InputAttributes =>
        BuildInputAttributes("bzs-input bzs-select", supportsReadOnly: false);

    private bool IsEmptyValue => CurrentValue is null || string.IsNullOrEmpty(CurrentValueAsString);

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

    private string FormatOptionValue(TValue value) => Options
        .First(option => EqualityComparer<TValue>.Default.Equals(option.Value, value))
        .ValueText;

    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    private void OnChanged(ChangeEventArgs args)
    {
        if (!Disabled && !ReadOnly)
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
            throw new InvalidOperationException("BzsSelect requires an Options collection.");
        }

        var duplicate = Options
            .GroupBy(static option => option.ValueText, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"BzsSelect option ValueText '{duplicate.Key}' must be unique.");
        }

        var duplicateValue = Options
            .GroupBy(static option => option.Value, EqualityComparer<TValue>.Default)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateValue is not null)
        {
            throw new InvalidOperationException("BzsSelect option values must be unique.");
        }
    }
}
