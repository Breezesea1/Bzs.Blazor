namespace Bzs.Blazor;

/// <summary>
/// Centers content and constrains its responsive maximum inline size.
/// </summary>
public partial class BzsContainer : BzsComponentBase
{
    /// <summary>
    /// Gets or sets whether the container follows stepped viewport breakpoints.
    /// </summary>
    [Parameter]
    public bool Fixed { get; set; }

    /// <summary>
    /// Gets or sets whether responsive inline gutters are included.
    /// </summary>
    [Parameter]
    public bool Gutters { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum inline size used when <see cref="Fixed"/> is false.
    /// </summary>
    [Parameter]
    public BzsContainerMaxWidth MaxWidth { get; set; } = BzsContainerMaxWidth.Large;

    /// <summary>
    /// Gets or sets the content rendered inside the container.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string WidthName => MaxWidth switch
    {
        BzsContainerMaxWidth.ExtraSmall => "extra-small",
        BzsContainerMaxWidth.Small => "small",
        BzsContainerMaxWidth.Medium => "medium",
        BzsContainerMaxWidth.Large => "large",
        BzsContainerMaxWidth.ExtraLarge => "extra-large",
        BzsContainerMaxWidth.ExtraExtraLarge => "extra-extra-large",
        BzsContainerMaxWidth.None => "none",
        _ => throw new ArgumentOutOfRangeException(nameof(MaxWidth), MaxWidth, "The container maximum width is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var widthClass = Fixed ? "bzs-container--fixed" : $"bzs-container--max-{WidthName}";
            var gutterClass = Gutters ? null : "bzs-container--no-gutters";
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-container {widthClass} {gutterClass}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-container"] = Fixed ? "fixed" : WidthName,
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(MaxWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(MaxWidth), MaxWidth, "The container maximum width is not supported.");
        }
    }
}
