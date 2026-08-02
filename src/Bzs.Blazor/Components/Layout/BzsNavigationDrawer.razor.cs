namespace Bzs.Blazor;

/// <summary>
/// Renders controlled application navigation that can be docked or overlaid by CSS.
/// </summary>
public partial class BzsNavigationDrawer : BzsComponentBase
{
    /// <summary>
    /// Gets or sets whether the navigation drawer is open.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>
    /// Gets or sets the callback used to request an open-state change.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    /// Gets or sets how the drawer participates in the application frame.
    /// </summary>
    [Parameter]
    public BzsNavigationDrawerVariant Variant { get; set; } = BzsNavigationDrawerVariant.Responsive;

    /// <summary>
    /// Gets or sets the logical edge where the navigation drawer is anchored.
    /// </summary>
    [Parameter]
    public BzsNavigationDrawerPosition Position { get; set; } = BzsNavigationDrawerPosition.Start;

    /// <summary>
    /// Gets or sets whether selecting the overlay backdrop requests that the drawer close.
    /// </summary>
    [Parameter]
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>
    /// Gets or sets the accessible name of the navigation landmark.
    /// </summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>
    /// Gets or sets the navigation content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string VariantName => Variant switch
    {
        BzsNavigationDrawerVariant.Persistent => "persistent",
        BzsNavigationDrawerVariant.Temporary => "temporary",
        BzsNavigationDrawerVariant.Responsive => "responsive",
        _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "The navigation drawer variant is not supported."),
    };

    private string PositionName => Position switch
    {
        BzsNavigationDrawerPosition.Start => "start",
        BzsNavigationDrawerPosition.End => "end",
        _ => throw new ArgumentOutOfRangeException(nameof(Position), Position, "The navigation drawer position is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes(
                    $"bzs-navigation-drawer bzs-navigation-drawer--{VariantName} " +
                    $"bzs-navigation-drawer--{PositionName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-navigation-drawer"] = VariantName,
                ["data-bzs-navigation-drawer-variant"] = VariantName,
                ["data-bzs-navigation-drawer-position"] = PositionName,
                ["data-bzs-open"] = Open ? "true" : "false",
            };

            if (!string.IsNullOrWhiteSpace(AccessibleName))
            {
                attributes["aria-label"] = AccessibleName.Trim();
            }

            if (!Open)
            {
                attributes["aria-hidden"] = "true";
                attributes["inert"] = string.Empty;
            }
            else
            {
                attributes.Remove("aria-hidden");
                attributes.Remove("inert");
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "The navigation drawer variant is not supported.");
        }

        if (!Enum.IsDefined(Position))
        {
            throw new ArgumentOutOfRangeException(nameof(Position), Position, "The navigation drawer position is not supported.");
        }
    }

    private async Task HandleBackdropClickAsync()
    {
        if (!Open || !CloseOnBackdropClick)
        {
            return;
        }

        await OpenChanged.InvokeAsync(false);
    }
}
