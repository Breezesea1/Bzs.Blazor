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
        Canvas: "#edf1f5",
        Surface: "#edf1f5",
        SurfaceRaised: "#f5f7fa",
        SurfaceInset: "#e4eaf0",
        SurfaceOverlay: "#ffffff",
        Text: "#172033",
        TextMuted: "#536174",
        Border: "#c8d1dc",
        FocusRing: "#2563eb",
        Primary: "#2563eb",
        OnPrimary: "#ffffff",
        Success: "#15803d",
        Warning: "#a16207",
        Error: "#b42318",
        Info: "#0369a1",
        DisabledSurface: "#e2e8f0",
        DisabledText: "#7b8796");

    /// <summary>
    /// Gets the independently designed built-in dark color scheme.
    /// </summary>
    public static BzsThemeColors Dark { get; } = new(
        Canvas: "#171c24",
        Surface: "#171c24",
        SurfaceRaised: "#202834",
        SurfaceInset: "#12161d",
        SurfaceOverlay: "#252f3d",
        Text: "#f2f5f8",
        TextMuted: "#b2bdca",
        Border: "#3b4655",
        FocusRing: "#7aa2ff",
        Primary: "#7aa2ff",
        OnPrimary: "#0d1726",
        Success: "#4ade80",
        Warning: "#fbbf24",
        Error: "#fb7185",
        Info: "#67c7ff",
        DisabledSurface: "#293240",
        DisabledText: "#7e8998");

    /// <summary>
    /// Gets the complete built-in theme with independently designed light and dark schemes.
    /// </summary>
    public static BzsTheme Default { get; } = new(
        LightColors: Light,
        DarkColors: Dark,
        LightDepth: new BzsThemeDepth(
            RaisedShadow: "6px 6px 14px rgb(163 177 198 / 0.42), -5px -5px 12px rgb(255 255 255 / 0.82)",
            InsetShadow: "inset 3px 3px 7px rgb(163 177 198 / 0.38), inset -3px -3px 7px rgb(255 255 255 / 0.68)",
            OverlayShadow: "0 18px 40px rgb(31 45 61 / 0.2)",
            FocusShadow: "0 0 0 3px rgb(36 99 235 / 0.32)"),
        DarkDepth: new BzsThemeDepth(
            RaisedShadow: "6px 6px 14px rgb(5 8 12 / 0.52), -4px -4px 10px rgb(53 66 84 / 0.32)",
            InsetShadow: "inset 3px 3px 7px rgb(5 8 12 / 0.52), inset -3px -3px 7px rgb(53 66 84 / 0.22)",
            OverlayShadow: "0 20px 48px rgb(0 0 0 / 0.48)",
            FocusShadow: "0 0 0 3px rgb(122 162 255 / 0.36)"),
        Shape: new BzsThemeShape(
            ControlRadius: "0.4375rem",
            ContainerRadius: "0.5rem",
            OverlayRadius: "0.5rem",
            BorderWidth: "1px"),
        Typography: new BzsThemeTypography(
            FontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, \"Segoe UI\", sans-serif",
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
