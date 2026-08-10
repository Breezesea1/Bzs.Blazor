namespace Bzs.Blazor.Demo.Client;

/// <summary>
/// Lucide-style icon geometry used by the demo shell navigation. These glyphs
/// are demo-local so the runtime library icon set stays minimal.
/// </summary>
public static class DemoNavIcons
{
    public static BzsIconData Overview { get; } = new("M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z");

    public static BzsIconData ThemeFoundation { get; } = new("M12 2.7 17.66 8.36a8 8 0 1 1-11.31 0z");

    public static BzsIconData Foundation { get; } = new(
        "m12.83 2.18a2 2 0 0 0-1.66 0L2.6 6.08a1 1 0 0 0 0 1.83l8.58 3.91a2 2 0 0 0 1.66 0l8.58-3.9a1 1 0 0 0 0-1.83z"
        + "M22 17.65l-9.17 4.16a2 2 0 0 1-1.66 0L2 17.65M22 12.65l-9.17 4.16a2 2 0 0 1-1.66 0L2 12.65");

    public static BzsIconData Forms { get; } = new(
        "M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"
        + "M9 2h6a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1H9a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z"
        + "M12 11h4M12 16h4M8 11h.01M8 16h.01");

    public static BzsIconData Feedback { get; } = new("M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z");

    public static BzsIconData Tabs { get; } = new("M19 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2zM3 9h18");

    public static BzsIconData Overlays { get; } = new(
        "M10 8h10a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H10a2 2 0 0 1-2-2V10a2 2 0 0 1 2-2z"
        + "M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2");

    public static BzsIconData Layout { get; } = new("M3 3h7v9H3zM14 3h7v5h-7zM14 12h7v9h-7zM3 16h7v5H3z");

    public static BzsIconData StaticRender { get; } = new(
        "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"
        + "M14 2v6h6M16 13H8M16 17H8M10 9H8");

    public static BzsIconData ServerRender { get; } = new(
        "M4 2h16a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2z"
        + "M4 14h16a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-4a2 2 0 0 1 2-2zM6 6h.01M6 18h.01");

    public static BzsIconData WebAssemblyRender { get; } = new(
        "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z"
        + "M2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z");

    public static BzsIconData AutoRender { get; } = new("M13 2 3 14h9l-1 8 10-12h-9l1-8z");

    public static BzsIconData Announcements { get; } = new(
        "M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M13.73 21a2 2 0 0 1-3.46 0");

    public static BzsIconData LogOut { get; } = new("M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9");
}
