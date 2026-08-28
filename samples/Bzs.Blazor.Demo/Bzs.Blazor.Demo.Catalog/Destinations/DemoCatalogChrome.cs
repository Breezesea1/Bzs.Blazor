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
        return Copy(destination).Name();
    }

    internal static DemoCatalogEntry Describe(
        DemoCatalogDestination destination,
        bool includesServerRenderModes)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var copy = Copy(destination);
        return new(
            destination,
            copy.Name(),
            includesServerRenderModes || copy.StandaloneDescription is null
                ? copy.Description?.Invoke()
                : copy.StandaloneDescription());
    }

    // One row per destination so a new destination cannot land with a name but no description, or
    // with either one silently missing. Copy is resolved per access because the accessors follow
    // the active culture.
    private static DemoDestinationCopy Copy(DemoCatalogDestination destination) => destination.Id switch
    {
        "overview" => new(() => DemoText.Chrome.Overview),
        "theme-foundation" => new(
            () => DemoText.Chrome.ThemeFoundation,
            () => DemoText.Landing.GroupThemeFoundationDescription),
        "foundation" => new(
            () => DemoText.Chrome.FoundationComponents,
            () => DemoText.Landing.GroupFoundationDescription),
        "forms" => new(
            () => DemoText.Chrome.Forms,
            () => DemoText.Landing.GroupFormsDescription),
        "productivity" => new(
            () => DemoText.Chrome.Productivity,
            () => DemoText.Landing.GroupProductivityDescription),
        "feedback" => new(
            () => DemoText.Chrome.Feedback,
            () => DemoText.Landing.GroupFeedbackDescription),
        "tabs" => new(
            () => DemoText.Chrome.Tabs,
            () => DemoText.Landing.GroupTabsDescription),
        "overlays" => new(
            () => DemoText.Chrome.Overlays,
            () => DemoText.Landing.GroupOverlaysDescription),
        "layout" => new(
            () => DemoText.Chrome.Layout,
            () => DemoText.Landing.GroupLayoutDescription),
        "navigation-drawer" => new(
            () => DemoText.Chrome.NavigationDrawer,
            () => DemoText.Landing.GroupNavigationDrawerDescription),
        "releases" => new(() => DemoText.Chrome.Releases),
        "static-ssr" => new(
            () => DemoText.Chrome.StaticSsr,
            () => DemoText.Landing.StaticSsrDescription),
        "interactive-server" => new(
            () => DemoText.Chrome.InteractiveServer,
            () => DemoText.Landing.InteractiveServerDescription),
        "interactive-webassembly" => new(
            () => DemoText.Chrome.InteractiveWebAssembly,
            () => DemoText.Landing.InteractiveWebAssemblyDescription,
            () => DemoText.Landing.StandaloneRuntimeDescription),
        "interactive-auto" => new(
            () => DemoText.Chrome.InteractiveAuto,
            () => DemoText.Landing.InteractiveAutoDescription),
        _ => throw new ArgumentOutOfRangeException(
            nameof(destination),
            destination.Id,
            "The destination has no Demo Catalog Chrome copy."),
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

/// <summary>
/// The bilingual copy one destination contributes to Demo Catalog Chrome. A destination without a
/// description contributes only a name; <paramref name="StandaloneDescription"/> is supplied only
/// where a host without the full render modes needs different wording.
/// </summary>
internal sealed record DemoDestinationCopy(
    Func<string> Name,
    Func<string>? Description = null,
    Func<string>? StandaloneDescription = null);
