using Bzs.Blazor;
using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client;

public sealed class DemoCatalogDestination
{
    private readonly Func<string>? _description;
    private readonly Func<string> _name;

    internal DemoCatalogDestination(
        string id,
        string route,
        Func<string> name,
        BzsIconData icon,
        Func<string>? description = null,
        DemoCatalogHostCapabilities availability = DemoCatalogHostCapabilities.SharedCatalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(name);

        Id = id;
        Route = route ?? throw new ArgumentNullException(nameof(route));
        _name = name;
        Icon = icon;
        _description = description;
        Availability = availability;
    }

    public string Id { get; }

    public string Route { get; }

    public string Name => _name();

    public BzsIconData Icon { get; }

    public string? Description => _description?.Invoke();

    internal DemoCatalogHostCapabilities Availability { get; }

    internal bool IsAvailable(DemoCatalogHostCapabilities capabilities) =>
        (Availability & capabilities) != 0;
}

public static class DemoCatalogDestinations
{
    public static DemoCatalogDestination Overview { get; } = new(
        "overview",
        "./",
        () => DemoText.Chrome.Overview,
        DemoNavIcons.Overview);

    public static DemoCatalogDestination ThemeFoundation { get; } = new(
        "theme-foundation",
        "theme-foundation",
        () => DemoText.Chrome.ThemeFoundation,
        DemoNavIcons.ThemeFoundation,
        () => DemoText.Landing.GroupThemeFoundationDescription);

    public static DemoCatalogDestination Foundation { get; } = new(
        "foundation",
        "foundation",
        () => DemoText.Chrome.FoundationComponents,
        DemoNavIcons.Foundation,
        () => DemoText.Landing.GroupFoundationDescription);

    public static DemoCatalogDestination Forms { get; } = new(
        "forms",
        "forms",
        () => DemoText.Chrome.Forms,
        DemoNavIcons.Forms,
        () => DemoText.Landing.GroupFormsDescription);

    public static DemoCatalogDestination Productivity { get; } = new(
        "productivity",
        "productivity",
        () => DemoText.Chrome.Productivity,
        DemoNavIcons.Foundation,
        () => DemoText.Landing.GroupProductivityDescription);

    public static DemoCatalogDestination Feedback { get; } = new(
        "feedback",
        "feedback",
        () => DemoText.Chrome.Feedback,
        DemoNavIcons.Feedback,
        () => DemoText.Landing.GroupFeedbackDescription);

    public static DemoCatalogDestination Tabs { get; } = new(
        "tabs",
        "tabs",
        () => DemoText.Chrome.Tabs,
        DemoNavIcons.Tabs,
        () => DemoText.Landing.GroupTabsDescription);

    public static DemoCatalogDestination Overlays { get; } = new(
        "overlays",
        "overlays",
        () => DemoText.Chrome.Overlays,
        DemoNavIcons.Overlays,
        () => DemoText.Landing.GroupOverlaysDescription);

    public static DemoCatalogDestination Layout { get; } = new(
        "layout",
        "layout",
        () => DemoText.Chrome.Layout,
        DemoNavIcons.Layout,
        () => DemoText.Landing.GroupLayoutDescription);

    public static DemoCatalogDestination NavigationDrawer { get; } = new(
        "navigation-drawer",
        "navigation-drawer",
        () => DemoText.Chrome.NavigationDrawer,
        DemoNavIcons.NavigationDrawer,
        () => DemoText.Landing.GroupNavigationDrawerDescription);

    public static DemoCatalogDestination Releases { get; } = new(
        "releases",
        "releases",
        () => DemoText.Chrome.Releases,
        DemoNavIcons.Announcements);

    public static DemoCatalogDestination StaticSsr { get; } = new(
        "static-ssr",
        "render-modes/static",
        () => DemoText.Chrome.StaticSsr,
        DemoNavIcons.StaticRender,
        availability: DemoCatalogHostCapabilities.FullRenderModes);

    public static DemoCatalogDestination InteractiveServer { get; } = new(
        "interactive-server",
        "render-modes/server",
        () => DemoText.Chrome.InteractiveServer,
        DemoNavIcons.ServerRender,
        availability: DemoCatalogHostCapabilities.FullRenderModes);

    public static DemoCatalogDestination InteractiveWebAssembly { get; } = new(
        "interactive-webassembly",
        "render-modes/webassembly",
        () => DemoText.Chrome.InteractiveWebAssembly,
        DemoNavIcons.WebAssemblyRender,
        availability: DemoCatalogHostCapabilities.FullRenderModes
            | DemoCatalogHostCapabilities.StandaloneRuntime);

    public static DemoCatalogDestination InteractiveAuto { get; } = new(
        "interactive-auto",
        "render-modes/auto",
        () => DemoText.Chrome.InteractiveAuto,
        DemoNavIcons.AutoRender,
        availability: DemoCatalogHostCapabilities.FullRenderModes);

    internal static IReadOnlyList<DemoCatalogDestination> ComponentGroupDestinations { get; } =
    [
        ThemeFoundation,
        Foundation,
        Forms,
        Productivity,
        Feedback,
        Tabs,
        Overlays,
        Layout,
        NavigationDrawer,
    ];

    private static IReadOnlyList<DemoCatalogDestination> CatalogNavigation { get; } =
    [
        Overview,
        .. ComponentGroupDestinations,
    ];

    private static IReadOnlyList<DemoCatalogRuntimeDefinition> RuntimeDefinitions { get; } =
    [
        new(StaticSsr, () => DemoText.Landing.StaticSsrDescription),
        new(InteractiveServer, () => DemoText.Landing.InteractiveServerDescription),
        new(
            InteractiveWebAssembly,
            () => DemoText.Landing.InteractiveWebAssemblyDescription,
            () => DemoText.Landing.StandaloneRuntimeDescription),
        new(InteractiveAuto, () => DemoText.Landing.InteractiveAutoDescription),
    ];

    internal static IReadOnlyList<DemoCatalogNavigationSection> GetNavigationSections(
        DemoCatalogHostCapabilities capabilities)
    {
        var runtimePresentation = GetRuntimePresentation(capabilities);
        var runtimeSection = new DemoCatalogNavigationSection(
            runtimePresentation.SectionName,
            runtimePresentation.Destinations
                .Select(item => item.Destination)
                .ToArray());

        return
        [
            new(DemoText.Chrome.CatalogSection, Available(CatalogNavigation, capabilities)),
            new(DemoText.Chrome.ProjectSection, Available([Releases], capabilities)),
            runtimeSection,
        ];
    }

    internal static DemoCatalogRuntimePresentation GetRuntimePresentation(
        DemoCatalogHostCapabilities capabilities)
    {
        var fullRenderModes = capabilities.HasFlag(DemoCatalogHostCapabilities.FullRenderModes);
        var destinations = RuntimeDefinitions
            .Where(item => item.Destination.IsAvailable(capabilities))
            .Select(item => new DemoCatalogRuntimeDestination(
                item.Destination,
                item.GetDescription(useStandaloneDescription: !fullRenderModes)))
            .ToArray();

        return new DemoCatalogRuntimePresentation(
            fullRenderModes
                ? DemoText.Chrome.RenderModesSection
                : DemoText.Chrome.RuntimeSection,
            destinations);
    }

    private static IReadOnlyList<DemoCatalogDestination> Available(
        IEnumerable<DemoCatalogDestination> destinations,
        DemoCatalogHostCapabilities capabilities) =>
        destinations.Where(destination => destination.IsAvailable(capabilities)).ToArray();

    public static string GetHref(
        NavigationManager navigation,
        DemoCatalogDestination destination,
        string? query = null,
        string? fragment = null)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(destination);

        var relativePath = destination.Route;
        if (!string.IsNullOrWhiteSpace(query))
        {
            relativePath = $"{relativePath}?{query.TrimStart('?')}";
        }

        if (!string.IsNullOrWhiteSpace(fragment))
        {
            relativePath = $"{relativePath}#{fragment.TrimStart('#')}";
        }

        return DemoCulture.PreserveCulture(
            new Uri(navigation.Uri),
            new Uri(navigation.BaseUri),
            relativePath);
    }

    private sealed record DemoCatalogRuntimeDefinition(
        DemoCatalogDestination Destination,
        Func<string> Description,
        Func<string>? StandaloneDescription = null)
    {
        internal string GetDescription(bool useStandaloneDescription) =>
            useStandaloneDescription && StandaloneDescription is not null
                ? StandaloneDescription()
                : Description();
    }
}

[Flags]
internal enum DemoCatalogHostCapabilities
{
    SharedCatalog = 1,
    FullRenderModes = 2,
    StandaloneRuntime = 4,
}

internal sealed record DemoCatalogNavigationSection(
    string Name,
    IReadOnlyList<DemoCatalogDestination> Destinations);

internal sealed record DemoCatalogRuntimeDestination(
    DemoCatalogDestination Destination,
    string Description);

internal sealed record DemoCatalogRuntimePresentation(
    string SectionName,
    IReadOnlyList<DemoCatalogRuntimeDestination> Destinations);
