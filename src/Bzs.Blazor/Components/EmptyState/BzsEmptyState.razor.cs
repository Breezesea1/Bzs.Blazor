namespace Bzs.Blazor;

/// <summary>Renders a lightweight empty state with optional supporting content and action.</summary>
public sealed partial class BzsEmptyState : BzsComponentBase
{
    /// <summary>Gets or sets the required empty-state heading.</summary>
    [Parameter, EditorRequired]
    public string? Title { get; set; }

    /// <summary>Gets or sets optional supporting text.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Gets or sets the decorative icon shown above the heading.</summary>
    [Parameter]
    public BzsIconData? Icon { get; set; }

    /// <summary>Gets or sets optional action content.</summary>
    [Parameter]
    public RenderFragment? ActionContent { get; set; }

    private BzsIconData EffectiveIcon => Icon ?? BzsIcons.Package;

    private IReadOnlyDictionary<string, object> RootAttributes =>
        BuildAttributes("bzs-empty-state");

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("BzsEmptyState requires a non-empty Title.");
        }
    }
}
