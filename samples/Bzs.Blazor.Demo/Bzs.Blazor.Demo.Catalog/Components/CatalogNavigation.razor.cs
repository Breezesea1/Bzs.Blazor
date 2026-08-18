using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class CatalogNavigation : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public bool IncludesServerRenderModes { get; set; }

    private IReadOnlyList<DemoCatalogNavigationSection> NavigationSections =>
        DemoCatalogDestinations.GetNavigationSections(
            IncludesServerRenderModes
                ? DemoCatalogHostCapabilities.SharedCatalog | DemoCatalogHostCapabilities.FullRenderModes
                : DemoCatalogHostCapabilities.SharedCatalog | DemoCatalogHostCapabilities.StandaloneRuntime);

    private string DestinationUrl(DemoCatalogDestination destination) =>
        DemoCatalogDestinations.GetHref(Navigation, destination);
}
