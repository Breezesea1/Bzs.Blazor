namespace Bzs.Blazor;

/// <summary>
/// Renders a compact identity avatar with initials or icon fallback content and an optional visible name.
/// </summary>
public sealed partial class BzsAvatar : BzsComponentBase
{
    private static readonly BzsIconData DefaultIcon = new(
        "M20 21a8 8 0 0 0-16 0M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8");

    /// <summary>
    /// Gets or sets the optional image URL. Initials or icon content remains as a stable fallback.
    /// </summary>
    [Parameter]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the initials rendered when no image is available.
    /// </summary>
    [Parameter]
    public string? Initials { get; set; }

    /// <summary>
    /// Gets or sets the optional visible identity name rendered beside the avatar.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the icon rendered when no initials are available.
    /// </summary>
    [Parameter]
    public BzsIconData? Icon { get; set; }

    /// <summary>
    /// Gets or sets the avatar size.
    /// </summary>
    [Parameter]
    public BzsAvatarSize Size { get; set; } = BzsAvatarSize.Medium;

    /// <summary>
    /// Gets or sets the avatar shape.
    /// </summary>
    [Parameter]
    public BzsAvatarShape Shape { get; set; } = BzsAvatarShape.Circle;

    /// <summary>
    /// Gets or sets the accessible identity name. When omitted, <see cref="Name" /> supplies the accessible
    /// identity text; an avatar without either name is decorative.
    /// </summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>
    /// Gets or sets optional trailing action content, such as an account menu or sign-out command.
    /// </summary>
    [Parameter]
    public RenderFragment? ActionContent { get; set; }

    private bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    private bool HasName => !string.IsNullOrWhiteSpace(Name);

    private bool HasAction => ActionContent is not null;

    private bool HasExplicitAccessibleName => !string.IsNullOrWhiteSpace(AccessibleName)
        || HasAdditionalAccessibleName();

    private string NormalizedName => Name?.Trim() ?? string.Empty;

    private string SizeName => Size switch
    {
        BzsAvatarSize.Small => "small",
        BzsAvatarSize.Medium => "medium",
        BzsAvatarSize.Large => "large",
        _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, "The avatar size is not supported."),
    };

    private string ShapeName => Shape switch
    {
        BzsAvatarShape.Circle => "circle",
        BzsAvatarShape.Rounded => "rounded",
        BzsAvatarShape.Square => "square",
        _ => throw new ArgumentOutOfRangeException(nameof(Shape), Shape, "The avatar shape is not supported."),
    };

    private RenderFragment FallbackContent => builder =>
    {
        if (!string.IsNullOrWhiteSpace(Initials))
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "bzs-avatar__initials");
            builder.AddContent(2, Initials.Trim());
            builder.CloseElement();
            return;
        }

        builder.OpenComponent<BzsIcon>(3);
        builder.AddAttribute(4, nameof(BzsIcon.Icon), Icon ?? DefaultIcon);
        builder.AddAttribute(5, nameof(BzsIcon.Class), "bzs-avatar__icon");
        builder.CloseComponent();
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-avatar bzs-avatar--{SizeName} bzs-avatar--{ShapeName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-avatar-size"] = SizeName,
                ["data-bzs-avatar-shape"] = ShapeName,
            };

            if (HasName)
            {
                attributes["data-bzs-avatar-has-name"] = "true";
                attributes["data-bzs-avatar-composite"] = "true";
                attributes.Remove("aria-hidden");

                if (!string.IsNullOrWhiteSpace(AccessibleName))
                {
                    attributes["role"] = "group";
                    attributes["aria-label"] = AccessibleName.Trim();
                }
                else if (HasAdditionalAccessibleName())
                {
                    attributes["role"] = "group";
                }
                else
                {
                    attributes.Remove("role");
                    attributes.Remove("aria-label");
                    attributes.Remove("aria-labelledby");
                }
            }
            else if (HasAction)
            {
                attributes["data-bzs-avatar-composite"] = "true";
                attributes.Remove("aria-hidden");

                if (!string.IsNullOrWhiteSpace(AccessibleName))
                {
                    attributes["role"] = "group";
                    attributes["aria-label"] = AccessibleName.Trim();
                }
                else if (HasAdditionalAccessibleName())
                {
                    attributes["role"] = "group";
                }
                else
                {
                    attributes.Remove("role");
                    attributes.Remove("aria-label");
                    attributes.Remove("aria-labelledby");
                }
            }
            else if (!string.IsNullOrWhiteSpace(AccessibleName))
            {
                attributes.Remove("aria-hidden");
                attributes["role"] = "img";
                attributes["aria-label"] = AccessibleName.Trim();
            }
            else if (HasAdditionalAccessibleName())
            {
                attributes.Remove("aria-hidden");
                attributes["role"] = "img";
            }
            else
            {
                attributes.Remove("role");
                attributes.Remove("aria-label");
                attributes.Remove("aria-labelledby");
                attributes["aria-hidden"] = "true";
            }

            if (HasAction)
            {
                attributes["data-bzs-avatar-has-action"] = "true";
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "The avatar size is not supported.");
        }

        if (!Enum.IsDefined(Shape))
        {
            throw new ArgumentOutOfRangeException(nameof(Shape), Shape, "The avatar shape is not supported.");
        }
    }

    private bool HasAdditionalAccessibleName() => AdditionalAttributes?.Any(attribute =>
        (attribute.Key.Equals("aria-label", StringComparison.OrdinalIgnoreCase)
            || attribute.Key.Equals("aria-labelledby", StringComparison.OrdinalIgnoreCase))
        && !string.IsNullOrWhiteSpace(attribute.Value?.ToString())) == true;
}

/// <summary>
/// Selects the fixed dimensions of a <see cref="BzsAvatar" />.
/// </summary>
public enum BzsAvatarSize
{
    /// <summary>Uses the compact avatar dimensions.</summary>
    Small,

    /// <summary>Uses the default avatar dimensions.</summary>
    Medium,

    /// <summary>Uses the large avatar dimensions.</summary>
    Large,
}

/// <summary>
/// Selects the visual shape of a <see cref="BzsAvatar" />.
/// </summary>
public enum BzsAvatarShape
{
    /// <summary>Uses a circular avatar.</summary>
    Circle,

    /// <summary>Uses the theme container radius.</summary>
    Rounded,

    /// <summary>Uses square corners.</summary>
    Square,
}
