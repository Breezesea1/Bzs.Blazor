using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>
/// Renders compact display content with optional controlled selection and removal commands.
/// </summary>
public sealed partial class BzsChip : BzsComponentBase
{
    /// <summary>
    /// Gets or sets whether the chip exposes a selection command.
    /// </summary>
    [Parameter]
    public bool Selectable { get; set; }

    /// <summary>
    /// Gets or sets the controlled selected state.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when a selectable chip requests a selected-state change.
    /// </summary>
    [Parameter]
    public EventCallback<bool> SelectedChanged { get; set; }

    /// <summary>
    /// Gets or sets whether the chip exposes a separate removal command.
    /// </summary>
    [Parameter]
    public bool Removable { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when removal is requested.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> RemoveRequested { get; set; }

    /// <summary>
    /// Gets or sets the accessible name of the removal command.
    /// </summary>
    [Parameter]
    public string? RemoveAccessibleName { get; set; }

    /// <summary>
    /// Gets or sets whether selection and removal commands are unavailable.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the optional decorative icon before the chip content.
    /// </summary>
    [Parameter]
    public BzsIconData? StartIcon { get; set; }

    /// <summary>
    /// Gets or sets the optional decorative icon after the chip content.
    /// </summary>
    [Parameter]
    public BzsIconData? EndIcon { get; set; }

    /// <summary>
    /// Gets or sets the visible chip content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-chip"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-chip-selected"] = Selected ? "true" : "false",
            };

            if (Disabled)
            {
                attributes["aria-disabled"] = "true";
            }
            else
            {
                attributes.Remove("aria-disabled");
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ChildContent is null)
        {
            throw new InvalidOperationException("BzsChip requires ChildContent.");
        }

        if (Selected && !Selectable)
        {
            throw new InvalidOperationException("BzsChip cannot be selected when Selectable is false.");
        }

        if (Removable && string.IsNullOrWhiteSpace(RemoveAccessibleName))
        {
            throw new InvalidOperationException("BzsChip requires RemoveAccessibleName when Removable is true.");
        }
    }

    private async Task RequestSelectionChangeAsync()
    {
        if (Disabled || !Selectable)
        {
            return;
        }

        await SelectedChanged.InvokeAsync(!Selected);
    }

    private async Task RequestRemovalAsync(MouseEventArgs eventArgs)
    {
        if (Disabled || !Removable)
        {
            return;
        }

        await RemoveRequested.InvokeAsync(eventArgs);
    }
}
