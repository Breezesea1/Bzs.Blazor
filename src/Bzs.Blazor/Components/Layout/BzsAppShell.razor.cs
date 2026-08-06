namespace Bzs.Blazor;

/// <summary>
/// Coordinates an app bar, navigation drawer, and main content as one application frame.
/// </summary>
/// <remarks>
/// Set <c>--bzs-app-shell-min-block-size</c> and <c>--bzs-navigation-drawer-width</c>
/// in a stylesheet to customize the frame without requiring inline styles.
/// </remarks>
public sealed partial class BzsAppShell : BzsComponentBase
{
    /// <summary>
    /// Gets or sets the navigation drawer, app bar, and main content composition.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-app-shell"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-app-shell"] = "true",
            };

            return attributes;
        }
    }
}
