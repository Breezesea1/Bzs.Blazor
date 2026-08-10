namespace Bzs.Blazor;

/// <summary>
/// Renders a decorative placeholder for content that is still loading.
/// </summary>
/// <remarks>
/// The skeleton is hidden from assistive technology. Consumers should expose
/// the loading state on the content region with <c>aria-busy</c> and an
/// accessible status message when the state is not otherwise communicated.
/// </remarks>
public sealed partial class BzsSkeleton : BzsComponentBase
{
    /// <summary>
    /// Gets or sets the placeholder shape.
    /// </summary>
    [Parameter]
    public BzsSkeletonShape Shape { get; set; } = BzsSkeletonShape.Text;

    /// <summary>
    /// Gets or sets the preset placeholder size.
    /// </summary>
    [Parameter]
    public BzsSkeletonSize Size { get; set; } = BzsSkeletonSize.Medium;

    /// <summary>
    /// Gets or sets whether the placeholder uses a loading animation.
    /// </summary>
    [Parameter]
    public bool Animated { get; set; } = true;

    private string ShapeName => Shape switch
    {
        BzsSkeletonShape.Text => "text",
        BzsSkeletonShape.Rectangle => "rectangle",
        BzsSkeletonShape.Circle => "circle",
        _ => throw new ArgumentOutOfRangeException(nameof(Shape), Shape, "The skeleton shape is not supported."),
    };

    private string SizeName => Size switch
    {
        BzsSkeletonSize.Small => "small",
        BzsSkeletonSize.Medium => "medium",
        BzsSkeletonSize.Large => "large",
        _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, "The skeleton size is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-skeleton bzs-skeleton--{ShapeName} bzs-skeleton--{SizeName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["aria-hidden"] = "true",
                ["data-bzs-skeleton-shape"] = ShapeName,
                ["data-bzs-skeleton-size"] = SizeName,
                ["data-bzs-skeleton-animated"] = Animated ? "true" : "false",
            };

            attributes.Remove("role");
            attributes.Remove("aria-label");
            attributes.Remove("aria-labelledby");
            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Shape))
        {
            throw new ArgumentOutOfRangeException(nameof(Shape), Shape, "The skeleton shape is not supported.");
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "The skeleton size is not supported.");
        }
    }
}

/// <summary>
/// Selects the visual shape of a <see cref="BzsSkeleton" />.
/// </summary>
public enum BzsSkeletonShape
{
    /// <summary>Represents a line of text.</summary>
    Text,

    /// <summary>Represents a rectangular content region.</summary>
    Rectangle,

    /// <summary>Represents circular content such as an avatar.</summary>
    Circle,
}

/// <summary>
/// Selects a preset size for a <see cref="BzsSkeleton" />.
/// </summary>
public enum BzsSkeletonSize
{
    /// <summary>Uses the compact placeholder size.</summary>
    Small,

    /// <summary>Uses the default placeholder size.</summary>
    Medium,

    /// <summary>Uses the large placeholder size.</summary>
    Large,
}
