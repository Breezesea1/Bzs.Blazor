namespace Bzs.Blazor;

/// <summary>
/// Renders the primary content region coordinated by a <see cref="BzsAppShell" />.
/// </summary>
public partial class BzsMainContent : BzsComponentBase
{
    /// <summary>
    /// Gets or sets whether the component renders a main landmark. Disable this for nested demonstrations.
    /// </summary>
    [Parameter]
    public bool Landmark { get; set; } = true;

    /// <summary>
    /// Gets or sets the content rendered in the main region.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-main-content"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-main-content"] = Landmark ? "landmark" : "container",
            };

            return attributes;
        }
    }
}
