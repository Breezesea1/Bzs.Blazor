using System.Collections.ObjectModel;

namespace Bzs.Blazor;

/// <summary>
/// Provides the common identity, class, style, and unmatched-attribute contract
/// used by Bzs.Blazor components.
/// </summary>
/// <remarks>
/// This base class standardizes component parameters. It is not a supported
/// inheritance extension contract for consumer components.
/// </remarks>
public abstract class BzsComponentBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the element identifier for the component root.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets CSS classes appended to the component's built-in root classes.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the consumer-provided root style escape hatch.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// Gets or sets attributes that are not matched by another component parameter.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Creates root attributes while preserving additional attributes and merging
    /// class and style values with the component defaults.
    /// </summary>
    /// <param name="componentClass">The built-in CSS classes for the component root.</param>
    /// <param name="componentStyle">The built-in root style, if a component requires one.</param>
    /// <returns>An immutable attribute map for attribute splatting.</returns>
    protected IReadOnlyDictionary<string, object> BuildAttributes(
        string? componentClass = null,
        string? componentStyle = null)
    {
        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        string? additionalId = null;
        string? additionalClass = null;
        string? additionalStyle = null;

        if (AdditionalAttributes is not null)
        {
            foreach (var attribute in AdditionalAttributes)
            {
                if (attribute.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    additionalId = AttributeValueToString(attribute.Value);
                }
                else if (attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase))
                {
                    additionalClass = AttributeValueToString(attribute.Value);
                }
                else if (attribute.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
                {
                    additionalStyle = AttributeValueToString(attribute.Value);
                }
                else
                {
                    attributes[attribute.Key] = attribute.Value;
                }
            }
        }

        var id = FirstNonEmpty(Id, additionalId);
        if (id is not null)
        {
            attributes["id"] = id;
        }

        var cssClass = JoinValues(componentClass, additionalClass, Class);
        if (cssClass is not null)
        {
            attributes["class"] = cssClass;
        }

        var style = JoinStyles(componentStyle, additionalStyle, Style);
        if (style is not null)
        {
            attributes["style"] = style;
        }

        return new ReadOnlyDictionary<string, object>(attributes);
    }

    private static string? AttributeValueToString(object? value) => value?.ToString();

    private static string? FirstNonEmpty(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    private static string? JoinValues(params string?[] values)
    {
        var nonEmptyValues = values.Where(static value => !string.IsNullOrWhiteSpace(value));
        var result = string.Join(" ", nonEmptyValues);

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? JoinStyles(params string?[] values)
    {
        var nonEmptyValues = values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim().TrimEnd(';'));
        var result = string.Join("; ", nonEmptyValues);

        return string.IsNullOrWhiteSpace(result) ? null : $"{result};";
    }
}
