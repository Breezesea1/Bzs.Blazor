namespace Bzs.Blazor.Demo.Client;

/// <summary>
/// The visitor-facing presentation of Demo Catalog Destinations: their bilingual names,
/// descriptions, and section grouping for the destinations a given host offers.
/// </summary>
internal static class DemoCatalogChrome
{
    internal static IReadOnlyList<DemoCatalogSection> GetSections(bool includesServerRenderModes) =>
    [
        new(DemoText.Chrome.CatalogSection, Present(DemoCatalogDestinations.Catalog, includesServerRenderModes)),
        new(DemoText.Chrome.ProjectSection, Present(DemoCatalogDestinations.Project, includesServerRenderModes)),
        GetRuntimeSection(includesServerRenderModes),
    ];

    internal static DemoCatalogSection GetRuntimeSection(bool includesServerRenderModes) =>
        new(
            includesServerRenderModes
                ? DemoText.Chrome.RenderModesSection
                : DemoText.Chrome.RuntimeSection,
            Present(DemoCatalogDestinations.Runtimes, includesServerRenderModes));

    internal static IReadOnlyList<DemoCatalogEntry> GetComponentGroups() =>
        Present(DemoCatalogDestinations.ComponentGroups, includesServerRenderModes: true);

    internal static DemoCatalogEntry Describe(
        DemoCatalogDestination destination,
        bool includesServerRenderModes = true)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return destination.Id switch
        {
            "overview" => new(destination, DemoText.Chrome.Overview, null),
            "theme-foundation" => new(
                destination,
                DemoText.Chrome.ThemeFoundation,
                DemoText.Landing.GroupThemeFoundationDescription),
            "foundation" => new(
                destination,
                DemoText.Chrome.FoundationComponents,
                DemoText.Landing.GroupFoundationDescription),
            "forms" => new(
                destination,
                DemoText.Chrome.Forms,
                DemoText.Landing.GroupFormsDescription),
            "productivity" => new(
                destination,
                DemoText.Chrome.Productivity,
                DemoText.Landing.GroupProductivityDescription),
            "feedback" => new(
                destination,
                DemoText.Chrome.Feedback,
                DemoText.Landing.GroupFeedbackDescription),
            "tabs" => new(
                destination,
                DemoText.Chrome.Tabs,
                DemoText.Landing.GroupTabsDescription),
            "overlays" => new(
                destination,
                DemoText.Chrome.Overlays,
                DemoText.Landing.GroupOverlaysDescription),
            "layout" => new(
                destination,
                DemoText.Chrome.Layout,
                DemoText.Landing.GroupLayoutDescription),
            "navigation-drawer" => new(
                destination,
                DemoText.Chrome.NavigationDrawer,
                DemoText.Landing.GroupNavigationDrawerDescription),
            "releases" => new(destination, DemoText.Chrome.Releases, null),
            "static-ssr" => new(
                destination,
                DemoText.Chrome.StaticSsr,
                DemoText.Landing.StaticSsrDescription),
            "interactive-server" => new(
                destination,
                DemoText.Chrome.InteractiveServer,
                DemoText.Landing.InteractiveServerDescription),
            "interactive-webassembly" => new(
                destination,
                DemoText.Chrome.InteractiveWebAssembly,
                includesServerRenderModes
                    ? DemoText.Landing.InteractiveWebAssemblyDescription
                    : DemoText.Landing.StandaloneRuntimeDescription),
            "interactive-auto" => new(
                destination,
                DemoText.Chrome.InteractiveAuto,
                DemoText.Landing.InteractiveAutoDescription),
            _ => throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Id,
                "The destination has no Demo Catalog Chrome copy."),
        };
    }

    private static IReadOnlyList<DemoCatalogEntry> Present(
        IEnumerable<DemoCatalogDestination> destinations,
        bool includesServerRenderModes)
    {
        var capabilities = DemoCatalogHostCapabilities.SharedCatalog
            | (includesServerRenderModes
                ? DemoCatalogHostCapabilities.FullRenderModes
                : DemoCatalogHostCapabilities.StandaloneRuntime);

        return destinations
            .Where(destination => destination.IsAvailable(capabilities))
            .Select(destination => Describe(destination, includesServerRenderModes))
            .ToArray();
    }
}

internal sealed record DemoCatalogEntry(
    DemoCatalogDestination Destination,
    string Name,
    string? Description);

internal sealed record DemoCatalogSection(
    string Name,
    IReadOnlyList<DemoCatalogEntry> Destinations);
