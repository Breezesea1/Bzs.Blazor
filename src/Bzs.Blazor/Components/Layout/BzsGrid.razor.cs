namespace Bzs.Blazor;

/// <summary>
/// Arranges child items on a responsive twelve-column grid.
/// </summary>
public sealed partial class BzsGrid : BzsComponentBase
{
    /// <summary>
    /// Gets or sets the token-driven gap between rows and columns.
    /// </summary>
    [Parameter]
    public BzsLayoutSpacing Spacing { get; set; } = BzsLayoutSpacing.Large;

    /// <summary>
    /// Gets or sets alignment of items within each grid row.
    /// </summary>
    [Parameter]
    public BzsAlignItems AlignItems { get; set; } = BzsAlignItems.Stretch;

    /// <summary>
    /// Gets or sets the grid items.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes(
                    $"bzs-grid bzs-grid--spacing-{LayoutNames.Spacing(Spacing)} " +
                    $"bzs-grid--align-{LayoutNames.Align(AlignItems)}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-grid"] = "12",
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

        if (!Enum.IsDefined(AlignItems))
        {
            throw new ArgumentOutOfRangeException(nameof(AlignItems), AlignItems, "The layout alignment is not supported.");
        }
    }
}
