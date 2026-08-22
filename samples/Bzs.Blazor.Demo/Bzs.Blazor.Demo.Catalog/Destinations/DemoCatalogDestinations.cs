using Bzs.Blazor;

namespace Bzs.Blazor.Demo.Client;

/// <summary>
/// A stable visitor-reachable location in the Demo Catalog. Its identity does not depend on the
/// active culture or on the host a visitor arrived through.
/// </summary>
public sealed class DemoCatalogDestination
{
    internal DemoCatalogDestination(
        string id,
        string route,
        BzsIconData icon,
        DemoCatalogHostCapabilities availability = DemoCatalogHostCapabilities.SharedCatalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;
        Route = route ?? throw new ArgumentNullException(nameof(route));
        Icon = icon;
        Availability = availability;
    }

    public string Id { get; }

    public string Route { get; }

    public BzsIconData Icon { get; }

    internal DemoCatalogHostCapabilities Availability { get; }

    internal bool IsAvailable(DemoCatalogHostCapabilities capabilities) =>
        (Availability & capabilities) != 0;
}

public static class DemoCatalogDestinations
{
    public static DemoCatalogDestination Overview { get; } = new(
        "overview",
        "./",
        DemoNavIcons.Overview);

    public static DemoCatalogDestination ThemeFoundation { get; } = new(
        "theme-foundation",
        "theme-foundation",
        DemoNavIcons.ThemeFoundation);

    public static DemoCatalogDestination Foundation { get; } = new(
        "foundation",
        "foundation",
        DemoNavIcons.Foundation);

    public static DemoCatalogDestination Forms { get; } = new(
        "forms",
        "forms",
        DemoNavIcons.Forms);

    public static DemoCatalogDestination Productivity { get; } = new(
        "productivity",
        "productivity",
        DemoNavIcons.Foundation);

    public static DemoCatalogDestination Feedback { get; } = new(
        "feedback",
        "feedback",
        DemoNavIcons.Feedback);

    public static DemoCatalogDestination Tabs { get; } = new(
        "tabs",
        "tabs",
        DemoNavIcons.Tabs);

    public static DemoCatalogDestination Overlays { get; } = new(
        "overlays",
        "overlays",
        DemoNavIcons.Overlays);

    public static DemoCatalogDestination Layout { get; } = new(
        "layout",
        "layout",
        DemoNavIcons.Layout);

    public static DemoCatalogDestination NavigationDrawer { get; } = new(
        "navigation-drawer",
        "navigation-drawer",
        DemoNavIcons.NavigationDrawer);

    public static DemoCatalogDestination Releases { get; } = new(
        "releases",
        "releases",
        DemoNavIcons.Announcements);

    public static DemoCatalogDestination StaticSsr { get; } = new(
        "static-ssr",
        "render-modes/static",
        DemoNavIcons.StaticRender,
        DemoCatalogHostCapabilities.FullRenderModes);

    public static DemoCatalogDestination InteractiveServer { get; } = new(
        "interactive-server",
        "render-modes/server",
        DemoNavIcons.ServerRender,
        DemoCatalogHostCapabilities.FullRenderModes);

    public static DemoCatalogDestination InteractiveWebAssembly { get; } = new(
        "interactive-webassembly",
        "render-modes/webassembly",
        DemoNavIcons.WebAssemblyRender,
        DemoCatalogHostCapabilities.FullRenderModes
            | DemoCatalogHostCapabilities.StandaloneRuntime);

    public static DemoCatalogDestination InteractiveAuto { get; } = new(
        "interactive-auto",
        "render-modes/auto",
        DemoNavIcons.AutoRender,
        DemoCatalogHostCapabilities.FullRenderModes);

    internal static IReadOnlyList<DemoCatalogDestination> ComponentGroups { get; } =
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

    internal static IReadOnlyList<DemoCatalogDestination> Catalog { get; } =
    [
        Overview,
        .. ComponentGroups,
    ];

    internal static IReadOnlyList<DemoCatalogDestination> Project { get; } = [Releases];

    internal static IReadOnlyList<DemoCatalogDestination> Runtimes { get; } =
    [
        StaticSsr,
        InteractiveServer,
        InteractiveWebAssembly,
        InteractiveAuto,
    ];
}

[Flags]
internal enum DemoCatalogHostCapabilities
{
    SharedCatalog = 1,
    FullRenderModes = 2,
    StandaloneRuntime = 4,
}
