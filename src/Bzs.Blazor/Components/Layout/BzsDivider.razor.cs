namespace Bzs.Blazor;

/// <summary>
/// Separates adjacent content along a horizontal or vertical axis.
/// </summary>
public partial class BzsDivider : BzsComponentBase
{
    /// <summary>
    /// Gets or sets whether the divider is vertical.
    /// </summary>
    [Parameter]
    public bool Vertical { get; set; }

    /// <summary>
    /// Gets or sets whether the divider stretches within a flex layout.
    /// </summary>
    [Parameter]
    public bool FlexItem { get; set; }

    /// <summary>
    /// Gets or sets whether the divider uses a quieter visual treatment.
    /// </summary>
    [Parameter]
    public bool Subtle { get; set; }

    /// <summary>
    /// Gets or sets whether the divider is positioned against its containing block.
    /// </summary>
    [Parameter]
    public bool Absolute { get; set; }

    /// <summary>
    /// Gets or sets the divider inset.
    /// </summary>
    [Parameter]
    public BzsDividerInset Inset { get; set; }

    private string OrientationName => Vertical ? "vertical" : "horizontal";

    private string InsetName => Inset switch
    {
        BzsDividerInset.None => "none",
        BzsDividerInset.Start => "start",
        BzsDividerInset.Both => "both",
        _ => throw new ArgumentOutOfRangeException(nameof(Inset), Inset, "The divider inset is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var flexClass = FlexItem ? "bzs-divider--flex-item" : null;
            var subtleClass = Subtle ? "bzs-divider--subtle" : null;
            var absoluteClass = Absolute ? "bzs-divider--absolute" : null;
            var attributes = new Dictionary<string, object>(
                BuildAttributes(
                    $"bzs-divider bzs-divider--{OrientationName} bzs-divider--inset-{InsetName} " +
                    $"{flexClass} {subtleClass} {absoluteClass}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["aria-orientation"] = OrientationName,
                ["data-bzs-divider"] = OrientationName,
                ["role"] = "separator",
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Inset))
        {
            throw new ArgumentOutOfRangeException(nameof(Inset), Inset, "The divider inset is not supported.");
        }
    }
}
