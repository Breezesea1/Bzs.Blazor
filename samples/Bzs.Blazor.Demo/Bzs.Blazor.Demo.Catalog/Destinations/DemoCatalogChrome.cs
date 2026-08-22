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

    internal static IReadOnlyList<DemoCatalogEntry> GetComponentGroups(bool includesServerRenderModes) =>
        Present(DemoCatalogDestinations.ComponentGroups, includesServerRenderModes);

    /// <summary>Gets the visitor-facing name of a destination, which no host varies.</summary>
    internal static string GetName(DemoCatalogDestination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return destination.Id switch
        {
            "overview" => DemoText.Chrome.Overview,
            "theme-foundation" => DemoText.Chrome.ThemeFoundation,
            "foundation" => DemoText.Chrome.FoundationComponents,
            "forms" => DemoText.Chrome.Forms,
            "productivity" => DemoText.Chrome.Productivity,
            "feedback" => DemoText.Chrome.Feedback,
            "tabs" => DemoText.Chrome.Tabs,
            "overlays" => DemoText.Chrome.Overlays,
            "layout" => DemoText.Chrome.Layout,
            "navigation-drawer" => DemoText.Chrome.NavigationDrawer,
            "releases" => DemoText.Chrome.Releases,
            "static-ssr" => DemoText.Chrome.StaticSsr,
            "interactive-server" => DemoText.Chrome.InteractiveServer,
            "interactive-webassembly" => DemoText.Chrome.InteractiveWebAssembly,
            "interactive-auto" => DemoText.Chrome.InteractiveAuto,
            _ => throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Id,
                "The destination has no Demo Catalog Chrome name."),
        };
    }

    internal static DemoCatalogEntry Describe(
        DemoCatalogDestination destination,
        bool includesServerRenderModes)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return new(destination, GetName(destination), GetDescription(destination, includesServerRenderModes));
    }

    private static string? GetDescription(
        DemoCatalogDestination destination,
        bool includesServerRenderModes) =>
        destination.Id switch
        {
            "overview" or "releases" => null,
            "theme-foundation" => DemoText.Landing.GroupThemeFoundationDescription,
            "foundation" => DemoText.Landing.GroupFoundationDescription,
            "forms" => DemoText.Landing.GroupFormsDescription,
            "productivity" => DemoText.Landing.GroupProductivityDescription,
            "feedback" => DemoText.Landing.GroupFeedbackDescription,
            "tabs" => DemoText.Landing.GroupTabsDescription,
            "overlays" => DemoText.Landing.GroupOverlaysDescription,
            "layout" => DemoText.Landing.GroupLayoutDescription,
            "navigation-drawer" => DemoText.Landing.GroupNavigationDrawerDescription,
            "static-ssr" => DemoText.Landing.StaticSsrDescription,
            "interactive-server" => DemoText.Landing.InteractiveServerDescription,
            "interactive-webassembly" => includesServerRenderModes
                ? DemoText.Landing.InteractiveWebAssemblyDescription
                : DemoText.Landing.StandaloneRuntimeDescription,
            "interactive-auto" => DemoText.Landing.InteractiveAutoDescription,
            _ => throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Id,
                "The destination has no Demo Catalog Chrome description."),
        };

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
