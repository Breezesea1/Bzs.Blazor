namespace Bzs.Blazor;

/// <summary>
/// Renders the primary command and context bar within a <see cref="BzsAppShell" />.
/// </summary>
public partial class BzsAppBar : BzsComponentBase
{
    /// <summary>
    /// Gets or sets whether the app bar remains pinned to the block-start edge while its shell scrolls.
    /// </summary>
    [Parameter]
    public bool Fixed { get; set; } = true;

    /// <summary>
    /// Gets or sets whether compact app bar dimensions are used.
    /// </summary>
    [Parameter]
    public bool Dense { get; set; }

    /// <summary>
    /// Gets or sets whether standard inline gutters are included.
    /// </summary>
    [Parameter]
    public bool Gutters { get; set; } = true;

    /// <summary>
    /// Gets or sets the semantic color treatment of the app bar.
    /// </summary>
    [Parameter]
    public BzsAppBarColor Color { get; set; } = BzsAppBarColor.Surface;

    /// <summary>
    /// Gets or sets the semantic depth treatment of the app bar.
    /// </summary>
    [Parameter]
    public BzsSurfaceLevel Level { get; set; } = BzsSurfaceLevel.Raised;

    /// <summary>
    /// Gets or sets the content rendered inside the app bar.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string ColorName => Color switch
    {
        BzsAppBarColor.Surface => "surface",
        BzsAppBarColor.Primary => "primary",
        BzsAppBarColor.Info => "info",
        BzsAppBarColor.Success => "success",
        BzsAppBarColor.Warning => "warning",
        BzsAppBarColor.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(Color), Color, "The app bar color is not supported."),
    };

    private string LevelName => Level switch
    {
        BzsSurfaceLevel.Base => "base",
        BzsSurfaceLevel.Raised => "raised",
        BzsSurfaceLevel.Inset => "inset",
        BzsSurfaceLevel.Overlay => "overlay",
        _ => throw new ArgumentOutOfRangeException(nameof(Level), Level, "The app bar surface level is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var denseClass = Dense ? "bzs-app-bar--dense" : null;
            var fixedClass = Fixed ? "bzs-app-bar--fixed" : null;
            var gutterClass = Gutters ? null : "bzs-app-bar--no-gutters";
            var attributes = new Dictionary<string, object>(
                BuildAttributes(
                    $"bzs-app-bar bzs-app-bar--{ColorName} bzs-app-bar--level-{LevelName} " +
                    $"{denseClass} {fixedClass} {gutterClass}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-app-bar"] = ColorName,
                ["data-bzs-app-bar-density"] = Dense ? "dense" : "regular",
                ["data-bzs-surface"] = LevelName,
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Color))
        {
            throw new ArgumentOutOfRangeException(nameof(Color), Color, "The app bar color is not supported.");
        }

        if (!Enum.IsDefined(Level))
        {
            throw new ArgumentOutOfRangeException(nameof(Level), Level, "The app bar surface level is not supported.");
        }
    }
}
