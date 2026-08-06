using Bzs.Blazor.Localization;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor;

/// <summary>Provides the shared native Blazor form contract for Bzs inputs.</summary>
public abstract class BzsInputBase<TValue> : InputBase<TValue>
{
    internal BzsInputBase()
    {
    }

    private readonly string _fallbackId = $"bzs-input-{Guid.NewGuid():N}";

    [Inject]
    private IStringLocalizer<BzsBlazorResources> Localizer { get; set; } = default!;

    /// <summary>Gets or sets the input element identifier.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Gets or sets CSS classes appended to the input element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets the input element style escape hatch.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Gets or sets the visible field label.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Gets or sets supporting field text.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Gets or sets whether the field is presented as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets whether the input is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the input is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets the native form name. InputBase supplies a stable default.</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>Gets or sets placeholder text where the native element supports it.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets the effective input identifier used by field relationships.</summary>
    protected string InputId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Id))
            {
                return Id.Trim();
            }

            return string.IsNullOrWhiteSpace(NameAttributeValue)
                ? _fallbackId
                : $"bzs-input-{NameAttributeValue}";
        }
    }

    /// <summary>Gets the effective native form name.</summary>
    protected string InputName => string.IsNullOrWhiteSpace(Name)
        ? NameAttributeValue ?? InputId
        : Name.Trim();

    /// <summary>Gets the description element identifier.</summary>
    protected string DescriptionId => $"{InputId}-description";

    /// <summary>Gets the validation element identifier.</summary>
    protected string ErrorId => $"{InputId}-error";

    /// <summary>Gets the first current validation message for this field.</summary>
    protected string? FieldError => EditContext?
        .GetValidationMessages(FieldIdentifier)
        .FirstOrDefault();

    /// <summary>Formats a localized validation error for the current field.</summary>
    protected string FormatValidationError(string resourceKey) =>
        Localize(resourceKey, DisplayName ?? FieldIdentifier.FieldName);

    /// <summary>Gets a localized library-owned string for a derived input.</summary>
    protected string Localize(string resourceKey, params object[] arguments) =>
        Localizer[resourceKey, arguments].Value;

    /// <summary>Builds attributes for the native form element.</summary>
    protected IReadOnlyDictionary<string, object> BuildInputAttributes(
        string componentClass,
        string? type = null,
        bool supportsReadOnly = true)
    {
        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        string? additionalClass = null;
        string? additionalStyle = null;
        string? additionalDescribedBy = null;

        if (AdditionalAttributes is not null)
        {
            foreach (var attribute in AdditionalAttributes)
            {
                if (attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase))
                {
                    additionalClass = attribute.Value?.ToString();
                }
                else if (attribute.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
                {
                    additionalStyle = attribute.Value?.ToString();
                }
                else if (attribute.Key.Equals("aria-describedby", StringComparison.OrdinalIgnoreCase))
                {
                    additionalDescribedBy = attribute.Value?.ToString();
                }
                else if (!ReservedAttributes.Contains(attribute.Key) && attribute.Value is not null)
                {
                    attributes[attribute.Key] = attribute.Value;
                }
            }
        }

        attributes["id"] = InputId;
        attributes["name"] = InputName;
        attributes["class"] = Join(componentClass, CssClass, additionalClass, Class);
        var style = JoinStyles(additionalStyle, Style);
        if (style is not null)
        {
            attributes["style"] = style;
        }

        if (type is not null)
        {
            attributes["type"] = type;
        }

        if (!string.IsNullOrWhiteSpace(Placeholder))
        {
            attributes["placeholder"] = Placeholder.Trim();
        }

        if (Disabled)
        {
            attributes["disabled"] = "disabled";
        }

        if (ReadOnly)
        {
            if (supportsReadOnly)
            {
                attributes["readonly"] = "readonly";
            }
            else
            {
                attributes["disabled"] = "disabled";
            }
            attributes["aria-readonly"] = "true";
        }

        if (Required)
        {
            attributes["required"] = "required";
            attributes["aria-required"] = "true";
        }

        var describedBy = Join(
            additionalDescribedBy,
            string.IsNullOrWhiteSpace(Description) ? null : DescriptionId,
            FieldError is null ? null : ErrorId);
        if (!string.IsNullOrWhiteSpace(describedBy))
        {
            attributes["aria-describedby"] = describedBy;
        }

        if (FieldError is not null)
        {
            attributes["aria-invalid"] = "true";
        }

        return attributes;
    }

    private static readonly HashSet<string> ReservedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "name", "class", "style", "type", "value", "checked", "disabled", "readonly",
        "required", "placeholder", "onchange", "aria-describedby", "aria-invalid", "aria-required", "aria-readonly",
    };

    private static string Join(params string?[] values) =>
        string.Join(" ", values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value!.Trim()));

    private static string? JoinStyles(params string?[] values)
    {
        var result = string.Join("; ", values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim().TrimEnd(';')));
        return result.Length == 0 ? null : $"{result};";
    }
}
