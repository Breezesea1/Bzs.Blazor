namespace Bzs.Blazor;

/// <summary>
/// Renders a semantic container with a token-driven depth treatment.
/// </summary>
public partial class BzsSurface : BzsComponentBase
{
    /// <summary>
    /// Gets or sets the semantic depth treatment applied to the surface.
    /// </summary>
    [Parameter]
    public BzsSurfaceLevel Level { get; set; } = BzsSurfaceLevel.Base;

    /// <summary>
    /// Gets or sets the content rendered inside the surface.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string LevelName => Level switch
    {
        BzsSurfaceLevel.Base => "base",
        BzsSurfaceLevel.Raised => "raised",
        BzsSurfaceLevel.Inset => "inset",
        BzsSurfaceLevel.Overlay => "overlay",
        _ => throw new ArgumentOutOfRangeException(nameof(Level), Level, "The surface level is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-surface bzs-surface--{LevelName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-surface"] = LevelName,
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Level))
        {
            throw new ArgumentOutOfRangeException(nameof(Level), Level, "The surface level is not supported.");
        }
    }
}
