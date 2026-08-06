namespace Bzs.Blazor;

/// <summary>
/// Arranges child content along one flexible row or column.
/// </summary>
public sealed partial class BzsStack : BzsComponentBase
{
    /// <summary>
    /// Gets or sets whether children are arranged horizontally.
    /// </summary>
    [Parameter]
    public bool Row { get; set; }

    /// <summary>
    /// Gets or sets whether the visual order is reversed.
    /// </summary>
    [Parameter]
    public bool Reverse { get; set; }

    /// <summary>
    /// Gets or sets the token-driven gap between children.
    /// </summary>
    [Parameter]
    public BzsLayoutSpacing Spacing { get; set; } = BzsLayoutSpacing.Medium;

    /// <summary>
    /// Gets or sets distribution along the main axis.
    /// </summary>
    [Parameter]
    public BzsJustify Justify { get; set; } = BzsJustify.Start;

    /// <summary>
    /// Gets or sets alignment along the cross axis.
    /// </summary>
    [Parameter]
    public BzsAlignItems AlignItems { get; set; } = BzsAlignItems.Stretch;

    /// <summary>
    /// Gets or sets wrapping behavior.
    /// </summary>
    [Parameter]
    public BzsStackWrap Wrap { get; set; } = BzsStackWrap.NoWrap;

    /// <summary>
    /// Gets or sets the content rendered inside the stack.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string DirectionName => (Row, Reverse) switch
    {
        (false, false) => "column",
        (false, true) => "column-reverse",
        (true, false) => "row",
        (true, true) => "row-reverse",
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes(
                    $"bzs-stack bzs-stack--{DirectionName} " +
                    $"bzs-stack--spacing-{LayoutNames.Spacing(Spacing)} " +
                    $"bzs-stack--justify-{LayoutNames.Justify(Justify)} " +
                    $"bzs-stack--align-{LayoutNames.Align(AlignItems)} " +
                    $"bzs-stack--wrap-{LayoutNames.Wrap(Wrap)}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-stack"] = DirectionName,
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Spacing))
        {
            throw new ArgumentOutOfRangeException(nameof(Spacing), Spacing, "The layout spacing is not supported.");
        }

        if (!Enum.IsDefined(Justify))
        {
            throw new ArgumentOutOfRangeException(nameof(Justify), Justify, "The layout justification is not supported.");
        }

        if (!Enum.IsDefined(AlignItems))
        {
            throw new ArgumentOutOfRangeException(nameof(AlignItems), AlignItems, "The layout alignment is not supported.");
        }

        if (!Enum.IsDefined(Wrap))
        {
            throw new ArgumentOutOfRangeException(nameof(Wrap), Wrap, "The stack wrapping behavior is not supported.");
        }
    }
}
