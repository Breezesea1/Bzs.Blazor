namespace Bzs.Blazor;

/// <summary>
/// Renders an SVG icon from strongly typed <see cref="BzsIconData" />.
/// </summary>
public partial class BzsIcon : BzsComponentBase
{
    /// <summary>
    /// Gets or sets the icon geometry to render.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public BzsIconData? Icon { get; set; }

    /// <summary>
    /// Gets or sets the accessible name for a meaningful icon. When omitted,
    /// the icon is decorative and hidden from assistive technology.
    /// </summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    private bool IsDecorative => string.IsNullOrWhiteSpace(AccessibleName)
        && !HasAdditionalAccessibleName();

    private IReadOnlyDictionary<string, object> SvgAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(BuildAttributes("bzs-icon"), StringComparer.OrdinalIgnoreCase)
            {
                ["viewBox"] = Icon?.ViewBox ?? BzsIconData.DefaultViewBox,
                ["fill"] = "none",
                ["stroke"] = "currentColor",
                ["stroke-width"] = "2",
                ["stroke-linecap"] = "round",
                ["stroke-linejoin"] = "round",
                ["focusable"] = "false",
            };

            if (IsDecorative)
            {
                attributes.Remove("role");
                attributes.Remove("aria-label");
                attributes["aria-hidden"] = "true";
            }
            else
            {
                attributes.Remove("aria-hidden");
                attributes["role"] = "img";
                if (!string.IsNullOrWhiteSpace(AccessibleName))
                {
                    attributes["aria-label"] = AccessibleName.Trim();
                }
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Icon);
    }

    private bool HasAdditionalAccessibleName()
    {
        if (AdditionalAttributes is null)
        {
            return false;
        }

        return AdditionalAttributes.Any(attribute =>
            (attribute.Key.Equals("aria-label", StringComparison.OrdinalIgnoreCase)
                || attribute.Key.Equals("aria-labelledby", StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(attribute.Value?.ToString()));
    }
}
