using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class CatalogShell : ComponentBase
{
    [Parameter, EditorRequired]
    public RenderFragment NavigationContent { get; set; } = default!;

    [Parameter, EditorRequired]
    public string Status { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public RenderFragment HeaderTools { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment Content { get; set; } = default!;
}
