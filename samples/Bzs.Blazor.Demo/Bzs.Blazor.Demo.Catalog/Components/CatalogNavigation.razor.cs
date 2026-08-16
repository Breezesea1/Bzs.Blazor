using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class CatalogNavigation : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public bool IncludesServerRenderModes { get; set; }

    private string RouteUrl(string route) => DemoCulture.PreserveCulture(
        new Uri(Navigation.Uri),
        new Uri(Navigation.BaseUri),
        route);
}
