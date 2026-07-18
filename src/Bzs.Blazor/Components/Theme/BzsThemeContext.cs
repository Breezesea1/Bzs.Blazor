namespace Bzs.Blazor;

/// <summary>
/// Provides the immutable theme state cascaded by <see cref="BzsThemeProvider" />.
/// </summary>
public sealed record BzsThemeContext
{
    private readonly EventCallback<BzsThemeMode> _modeChanged;
    private readonly EventCallback<BzsDensity> _densityChanged;

    internal BzsThemeContext(
        BzsTheme theme,
        BzsThemeMode mode,
        BzsThemeMode effectiveMode,
        BzsDensity density,
        bool isCustomTheme,
        EventCallback<BzsThemeMode> modeChanged,
        EventCallback<BzsDensity> densityChanged)
    {
        Theme = theme;
        Mode = mode;
        EffectiveMode = effectiveMode;
        Density = density;
        IsCustomTheme = isCustomTheme;
        _modeChanged = modeChanged;
        _densityChanged = densityChanged;
    }

    /// <summary>
    /// Gets the deterministic light, compact context used when no provider is present.
    /// </summary>
    public static BzsThemeContext Default { get; } = new(
        BzsThemes.Default,
        BzsThemeMode.Light,
        BzsThemeMode.Light,
        BzsDensity.Compact,
        false,
        EventCallback<BzsThemeMode>.Empty,
        EventCallback<BzsDensity>.Empty);

    /// <summary>
    /// Gets the configured semantic theme.
    /// </summary>
    public BzsTheme Theme { get; }

    /// <summary>
    /// Gets the consumer-selected theme mode.
    /// </summary>
    public BzsThemeMode Mode { get; }

    /// <summary>
    /// Gets the effective light or dark mode currently rendered by the provider.
    /// </summary>
    public BzsThemeMode EffectiveMode { get; }

    /// <summary>
    /// Gets the consumer-selected visual density.
    /// </summary>
    public BzsDensity Density { get; }

    /// <summary>
    /// Gets a value indicating whether the provider emitted a nonce-protected runtime theme.
    /// </summary>
    public bool IsCustomTheme { get; }

    /// <summary>
    /// Requests a controlled theme-mode change from the provider's consumer.
    /// </summary>
    /// <param name="mode">The requested mode.</param>
    /// <returns>A task that completes when the consumer callback completes.</returns>
    public Task RequestModeAsync(BzsThemeMode mode)
    {
        ValidateMode(mode);
        return _modeChanged.InvokeAsync(mode);
    }

    /// <summary>
    /// Requests a controlled density change from the provider's consumer.
    /// </summary>
    /// <param name="density">The requested density.</param>
    /// <returns>A task that completes when the consumer callback completes.</returns>
    public Task RequestDensityAsync(BzsDensity density)
    {
        ValidateDensity(density);
        return _densityChanged.InvokeAsync(density);
    }

    internal static void ValidateMode(BzsThemeMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "The theme mode is not supported.");
        }
    }

    internal static void ValidateDensity(BzsDensity density)
    {
        if (!Enum.IsDefined(density))
        {
            throw new ArgumentOutOfRangeException(nameof(density), density, "The density is not supported.");
        }
    }
}
