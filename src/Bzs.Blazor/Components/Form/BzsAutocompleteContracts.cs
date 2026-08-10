using System.Globalization;

namespace Bzs.Blazor;

/// <summary>Provides query-driven suggestions for <see cref="BzsAutocomplete{TValue}" />.</summary>
/// <typeparam name="TValue">The suggestion value type.</typeparam>
public interface IBzsAutocompleteProvider<TValue>
{
    /// <summary>Gets suggestions for the supplied query.</summary>
    /// <param name="query">The current user query.</param>
    /// <param name="cancellationToken">A token canceled when the query is superseded or the component is disposed.</param>
    /// <returns>The suggestions to display, in provider-defined order.</returns>
    ValueTask<IReadOnlyList<BzsAutocompleteOption<TValue>>> GetSuggestionsAsync(
        string query,
        CancellationToken cancellationToken);
}

/// <summary>Describes one strongly typed autocomplete suggestion.</summary>
/// <typeparam name="TValue">The suggestion value type.</typeparam>
public sealed record BzsAutocompleteOption<TValue>
{
    private string? _description;

    /// <summary>Initializes an autocomplete suggestion.</summary>
    /// <param name="value">The strongly typed value applied when selected.</param>
    /// <param name="label">The visible text used to identify the suggestion.</param>
    /// <param name="disabled">Whether the suggestion cannot be selected.</param>
    /// <param name="valueText">The stable form value used for strict text parsing.</param>
    public BzsAutocompleteOption(TValue value, string label, bool disabled = false, string? valueText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Value = value;
        Label = label.Trim();
        Disabled = disabled;
        ValueText = valueText ?? FormatValue(value);
    }

    /// <summary>Gets the strongly typed suggestion value.</summary>
    public TValue Value { get; }

    /// <summary>Gets the visible suggestion label.</summary>
    public string Label { get; }

    /// <summary>Gets whether the suggestion is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Gets the stable form value used for strict text parsing.</summary>
    public string ValueText { get; }

    /// <summary>Gets optional supporting text shown below the label.</summary>
    public string? Description
    {
        get => _description;
        init => _description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Gets the optional decorative icon shown with the suggestion.</summary>
    public BzsIconData? Icon { get; init; }

    private static string FormatValue(TValue value) => value switch
    {
        null => string.Empty,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ when typeof(TValue).IsEnum => value.ToString() ?? string.Empty,
        _ => throw new ArgumentException(
            $"BzsAutocompleteOption<{typeof(TValue).Name}> requires an explicit valueText for values that do not support invariant formatting."),
    };
}
