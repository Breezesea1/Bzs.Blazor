namespace Bzs.Blazor;

/// <summary>
/// Renders a semantic application navigation menu composed from <see cref="BzsNavItem" /> components.
/// </summary>
public sealed partial class BzsNavMenu : BzsComponentBase
{
    /// <summary>Gets or sets the accessible name of the navigation landmark.</summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets the navigation items.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-nav-menu"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-nav-menu"] = "true",
            };

            if (!string.IsNullOrWhiteSpace(AccessibleName))
            {
                attributes["aria-label"] = AccessibleName.Trim();
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ChildContent is null)
        {
            throw new InvalidOperationException("BzsNavMenu requires ChildContent.");
        }
    }
}
