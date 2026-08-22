using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class CatalogNavigation : ComponentBase
{
    [Inject]
    private DemoDestinationLinks Links { get; set; } = default!;

    [Parameter]
    public bool IncludesServerRenderModes { get; set; }

    private IReadOnlyList<DemoCatalogSection> NavigationSections =>
        DemoCatalogChrome.GetSections(IncludesServerRenderModes);
}
