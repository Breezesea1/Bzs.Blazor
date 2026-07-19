namespace Bzs.Blazor;

/// <summary>
/// Provides the curated Lucide icon geometry embedded by Bzs.Blazor.
/// </summary>
/// <remarks>
/// Selected geometry is derived from Lucide Icons under the ISC license. See
/// <c>LICENSE</c> in the package for the full Lucide and Feather attribution.
/// </remarks>
public static class BzsIcons
{
    /// <summary>Gets the close icon.</summary>
    public static BzsIconData Close { get; } = new("M18 6 6 18M6 6l12 12");

    /// <summary>Gets the check icon.</summary>
    public static BzsIconData Check { get; } = new("M20 6 9 17l-5-5");

    /// <summary>Gets the left chevron icon.</summary>
    public static BzsIconData ChevronLeft { get; } = new("m15 18-6-6 6-6");

    /// <summary>Gets the right chevron icon.</summary>
    public static BzsIconData ChevronRight { get; } = new("m9 18 6-6-6-6");

    /// <summary>Gets the upward chevron icon.</summary>
    public static BzsIconData ChevronUp { get; } = new("m18 15-6-6-6 6");

    /// <summary>Gets the downward chevron icon.</summary>
    public static BzsIconData ChevronDown { get; } = new("m6 9 6 6 6-6");

    /// <summary>Gets the calendar icon.</summary>
    public static BzsIconData Calendar { get; } = new(
        "M8 2v4M16 2v4M3 10h18M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2");

    /// <summary>Gets the information icon.</summary>
    public static BzsIconData Info { get; } = new(
        "M12 16v-4M12 8h.01M3 12a9 9 0 1 0 18 0 9 9 0 0 0-18 0");

    /// <summary>Gets the warning icon.</summary>
    public static BzsIconData Warning { get; } = new(
        "M21.73 18 13.73 4a2 2 0 0 0-3.46 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3M12 9v4M12 17h.01");

    /// <summary>Gets the error icon.</summary>
    public static BzsIconData Error { get; } = new(
        "M15 9l-6 6M9 9l6 6M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0");

    /// <summary>Gets the success icon.</summary>
    public static BzsIconData Success { get; } = new(
        "M22 11.08V12a10 10 0 1 1-5.93-9.14M9 11l3 3L22 4");

    /// <summary>Gets the menu icon.</summary>
    public static BzsIconData Menu { get; } = new("M4 12h16M4 6h16M4 18h16");

    /// <summary>Gets the loading indicator icon.</summary>
    public static BzsIconData Loader { get; } = new(
        "M12 2v4M16.2 7.8l2.9-2.9M18 12h4M16.2 16.2l2.9 2.9M12 18v4M7.8 16.2l-2.9 2.9M6 12H2M7.8 7.8 4.9 4.9");
}
