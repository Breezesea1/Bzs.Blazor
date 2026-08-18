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

    /// <summary>Gets the package icon.</summary>
    public static BzsIconData Package { get; } = new(
        "m7.5 4.27 9 5.15M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16ZM3.3 7 12 12l8.7-5M12 22V12");

    /// <summary>Gets the search icon.</summary>
    public static BzsIconData Search { get; } = new(
        "m21 21-4.35-4.35M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16");

    /// <summary>Gets the filter icon.</summary>
    public static BzsIconData Filter { get; } = new(
        "M22 3H2l8 9.46V19l4 2v-8.54L22 3Z");

    /// <summary>Gets the refresh icon.</summary>
    public static BzsIconData Refresh { get; } = new(
        "M20 11a8.1 8.1 0 0 0-15.5-2M4 4v5h5M4 13a8.1 8.1 0 0 0 15.5 2M20 20v-5h-5");

    /// <summary>Gets the eye icon.</summary>
    public static BzsIconData Eye { get; } = new(
        "M2.062 12.348a1 1 0 0 1 0-.696C3.54 7.51 7.773 4.5 12 4.5c4.227 0 8.46 3.01 9.938 7.152a1 1 0 0 1 0 .696C20.46 16.49 16.227 19.5 12 19.5c-4.227 0-8.46-3.01-9.938-7.152ZM12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7");

    /// <summary>Gets the eye-off icon.</summary>
    public static BzsIconData EyeOff { get; } = new(
        "m2 2 20 20M6.71 6.71C4.93 7.89 3.57 9.57 2.62 11.62a1 1 0 0 0 0 .76C4.1 15.49 7.98 18.5 12 18.5c1.35 0 2.66-.34 3.84-.96M10.73 10.73a2 2 0 0 0 2.54 2.54M9.88 4.24A10.94 10.94 0 0 1 12 4.03c4.02 0 7.9 3.01 9.38 7.15a1 1 0 0 1 0 .76 10.97 10.97 0 0 1-1.67 2.68");

    /// <summary>Gets the loading indicator icon.</summary>
    public static BzsIconData Loader { get; } = new(
        "M12 2v4M16.2 7.8l2.9-2.9M18 12h4M16.2 16.2l2.9 2.9M12 18v4M7.8 16.2l-2.9 2.9M6 12H2M7.8 7.8 4.9 4.9");
}
