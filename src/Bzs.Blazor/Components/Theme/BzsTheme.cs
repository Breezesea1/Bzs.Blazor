namespace Bzs.Blazor;

/// <summary>
/// Selects the requested built-in color scheme.
/// </summary>
public enum BzsThemeMode
{
    /// <summary>Uses the built-in light scheme.</summary>
    Light,

    /// <summary>Uses the built-in dark scheme.</summary>
    Dark,

    /// <summary>Follows the browser color-scheme preference after interactivity begins.</summary>
    System,
}

/// <summary>
/// Selects the control density applied through semantic CSS variables.
/// </summary>
public enum BzsDensity
{
    /// <summary>Uses compact, productivity-oriented dimensions.</summary>
    Compact,

    /// <summary>Uses more spacious dimensions for lower-density interfaces.</summary>
    Comfortable,
}

/// <summary>
/// Identifies the semantic elevation treatment of a component surface.
/// </summary>
public enum BzsSurfaceLevel
{
    /// <summary>A flat base surface.</summary>
    Base,

    /// <summary>A subtly raised surface.</summary>
    Raised,

    /// <summary>An inset surface.</summary>
    Inset,

    /// <summary>An overlay surface.</summary>
    Overlay,
}

/// <summary>
/// Defines semantic color values for one color scheme.
/// </summary>
public sealed record BzsThemeColors(
    string Canvas,
    string Surface,
    string SurfaceRaised,
    string SurfaceInset,
    string SurfaceOverlay,
    string Text,
    string TextMuted,
    string Border,
    string FocusRing,
    string Primary,
    string OnPrimary,
    string Success,
    string Warning,
    string Error,
    string Info,
    string DisabledSurface,
    string DisabledText);

/// <summary>
/// Defines semantic shadows and focus depth tokens.
/// </summary>
public sealed record BzsThemeDepth(
    string RaisedShadow,
    string InsetShadow,
    string OverlayShadow,
    string FocusShadow);

/// <summary>
/// Defines semantic dimensions for control and surface shape.
/// </summary>
public sealed record BzsThemeShape(
    string ControlRadius,
    string ContainerRadius,
    string OverlayRadius,
    string BorderWidth);

/// <summary>
/// Defines the semantic typography values shared by a theme.
/// </summary>
public sealed record BzsThemeTypography(
    string FontFamily,
    string FontSize,
    string SmallFontSize,
    string LineHeight,
    string FontWeightRegular,
    string FontWeightMedium,
    string FontWeightBold);

/// <summary>
/// Defines semantic motion values shared by a theme.
/// </summary>
public sealed record BzsThemeMotion(
    string FastDuration,
    string NormalDuration,
    string SlowDuration,
    string Easing);

/// <summary>
/// Defines immutable semantic tokens for both light and dark color schemes.
/// </summary>
public sealed record BzsTheme(
    BzsThemeColors LightColors,
    BzsThemeColors DarkColors,
    BzsThemeDepth LightDepth,
    BzsThemeDepth DarkDepth,
    BzsThemeShape Shape,
    BzsThemeTypography Typography,
    BzsThemeMotion Motion)
{
    /// <summary>
    /// Gets the color values for an effective light or dark mode.
    /// </summary>
    /// <param name="mode">The resolved theme mode.</param>
    /// <returns>The matching semantic color values.</returns>
    public BzsThemeColors GetColors(BzsThemeMode mode) => mode switch
    {
        BzsThemeMode.Dark => DarkColors,
        BzsThemeMode.Light or BzsThemeMode.System => LightColors,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "The theme mode is not supported."),
    };

    /// <summary>Gets the depth values for an effective light or dark mode.</summary>
    public BzsThemeDepth GetDepth(BzsThemeMode mode) => mode switch
    {
        BzsThemeMode.Dark => DarkDepth,
        BzsThemeMode.Light or BzsThemeMode.System => LightDepth,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "The theme mode is not supported."),
    };
}

/// <summary>
/// Provides the immutable built-in theme values used by the external stylesheet.
/// </summary>
public static class BzsThemes
{
    /// <summary>
    /// Gets the independently designed built-in light color scheme.
    /// </summary>
    public static BzsThemeColors Light { get; } = new(
        Canvas: "#dde2e9",
        Surface: "#dde2e9",
        SurfaceRaised: "#fafbfc",
        SurfaceInset: "#d9dde3",
        SurfaceOverlay: "#ffffff",
        Text: "#20252d",
        TextMuted: "#59616d",
        Border: "#5c6570",
        FocusRing: "#4b5968",
        Primary: "#303741",
        OnPrimary: "#ffffff",
        Success: "#2f7d62",
        Warning: "#9a641f",
        Error: "#b42318",
        Info: "#356b86",
        DisabledSurface: "#c9ced6",
        DisabledText: "#89919c");

    /// <summary>
    /// Gets the independently designed built-in dark color scheme.
    /// </summary>
    public static BzsThemeColors Dark { get; } = new(
        Canvas: "#0c0f14",
        Surface: "#141920",
        SurfaceRaised: "#20272f",
        SurfaceInset: "#0a0d11",
        SurfaceOverlay: "#262d37",
        Text: "#f3f4f6",
        TextMuted: "#aeb4bd",
        Border: "#7d8894",
        FocusRing: "#d3dae4",
        Primary: "#d9dee5",
        OnPrimary: "#171c22",
        Success: "#5fc69e",
        Warning: "#e8b45c",
        Error: "#f27d88",
        Info: "#7ab9d6",
        DisabledSurface: "#242b34",
        DisabledText: "#7f8996");

    /// <summary>
    /// Gets the complete built-in theme with independently designed light and dark schemes.
    /// </summary>
    public static BzsTheme Default { get; } = new(
        LightColors: Light,
        DarkColors: Dark,
        LightDepth: new BzsThemeDepth(
            RaisedShadow: "7px 7px 18px rgb(140 148 161 / 0.46), -6px -6px 14px rgb(255 255 255 / 0.88)",
            InsetShadow: "inset 3px 3px 8px rgb(140 148 161 / 0.5), inset -3px -3px 7px rgb(255 255 255 / 0.82)",
            OverlayShadow: "0 20px 44px rgb(38 45 56 / 0.24)",
            FocusShadow: "0 0 0 3px rgb(75 89 104 / 0.26)"),
        DarkDepth: new BzsThemeDepth(
            RaisedShadow: "7px 7px 18px rgb(2 4 7 / 0.66), -5px -5px 13px rgb(56 66 79 / 0.46)",
            InsetShadow: "inset 3px 3px 8px rgb(2 4 7 / 0.68), inset -3px -3px 7px rgb(56 66 79 / 0.38)",
            OverlayShadow: "0 22px 52px rgb(0 0 0 / 0.6)",
            FocusShadow: "0 0 0 3px rgb(211 218 228 / 0.28)"),
        Shape: new BzsThemeShape(
            ControlRadius: "0.625rem",
            ContainerRadius: "0.625rem",
            OverlayRadius: "0.625rem",
            BorderWidth: "1px"),
        Typography: new BzsThemeTypography(
            FontFamily: "\"Segoe UI Variable\", \"Segoe UI\", ui-sans-serif, system-ui, sans-serif",
            FontSize: "0.875rem",
            SmallFontSize: "0.75rem",
            LineHeight: "1.4",
            FontWeightRegular: "400",
            FontWeightMedium: "500",
            FontWeightBold: "600"),
        Motion: new BzsThemeMotion(
            FastDuration: "120ms",
            NormalDuration: "180ms",
            SlowDuration: "240ms",
            Easing: "cubic-bezier(0.2, 0, 0, 1)"));
}
