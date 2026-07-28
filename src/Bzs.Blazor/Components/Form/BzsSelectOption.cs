using System.Globalization;

namespace Bzs.Blazor;

/// <summary>Describes one strongly typed option rendered by <see cref="BzsSelect{TValue}" />.</summary>
public sealed record BzsSelectOption<TValue>
{
    private string? _description;

    /// <summary>Initializes a strongly typed select option.</summary>
    public BzsSelectOption(TValue value, string label, bool disabled = false, string? valueText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Value = value;
        Label = label;
        Disabled = disabled;
        ValueText = valueText ?? FormatValue(value);
    }

    /// <summary>Gets the strongly typed option value.</summary>
    public TValue Value { get; }
    /// <summary>Gets the visible option label.</summary>
    public string Label { get; }
    /// <summary>Gets whether this option is disabled.</summary>
    public bool Disabled { get; }
    /// <summary>Gets the stable native option value used for form posts.</summary>
    public string ValueText { get; }
    /// <summary>Gets optional supporting text shown below the option label.</summary>
    public string? Description
    {
        get => _description;
        init => _description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    /// <summary>Gets the optional decorative icon shown with the option.</summary>
    public BzsIconData? Icon { get; init; }

    private static string FormatValue(TValue value) => value switch
    {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        string text => text,
        _ when typeof(TValue).IsEnum => value.ToString() ?? string.Empty,
        _ => throw new ArgumentException(
            $"BzsSelectOption<{typeof(TValue).Name}> requires an explicit valueText for values that do not support invariant formatting."),
    };
}
