namespace Bzs.Blazor;

/// <summary>
/// Cascades semantic theme state and applies built-in or nonce-protected custom tokens.
/// </summary>
public sealed partial class BzsThemeProvider : BzsComponentBase, IAsyncDisposable
{
    private readonly string _scopeId = Guid.NewGuid().ToString("N");
    private BzsThemeContext _context = BzsThemeContext.Default;
    private BzsThemeProviderInterop? _interop;
    private DotNetObjectReference<BzsThemeProvider>? _dotNetReference;
    private ElementReference _rootElement;
    private bool _systemObserverEnabled;
    private bool _systemPrefersDark;
    private bool _disposed;
    private string? _customThemeCss;

    /// <summary>Gets or sets the semantic theme. The built-in theme is used by default.</summary>
    [Parameter]
    public BzsTheme Theme { get; set; } = BzsThemes.Default;

    /// <summary>Gets or sets the consumer-controlled requested theme mode.</summary>
    [Parameter]
    public BzsThemeMode Mode { get; set; } = BzsThemeMode.Light;

    /// <summary>Gets or sets the callback raised when a descendant requests a mode change.</summary>
    [Parameter]
    public EventCallback<BzsThemeMode> ModeChanged { get; set; }

    /// <summary>Gets or sets the consumer-controlled visual density.</summary>
    [Parameter]
    public BzsDensity Density { get; set; } = BzsDensity.Compact;

    /// <summary>Gets or sets the callback raised when a descendant requests a density change.</summary>
    [Parameter]
    public EventCallback<BzsDensity> DensityChanged { get; set; }

    /// <summary>Gets or sets the CSP nonce required for a runtime custom theme.</summary>
    [Parameter]
    public string? CspNonce { get; set; }

    /// <summary>Gets or sets the content that receives the theme context and CSS variables.</summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    private BzsThemeMode EffectiveMode => Mode == BzsThemeMode.System
        ? (_systemPrefersDark ? BzsThemeMode.Dark : BzsThemeMode.Light)
        : Mode;

    private string EffectiveModeName => EffectiveMode == BzsThemeMode.Dark ? "dark" : "light";

    private string DensityName => Density == BzsDensity.Comfortable ? "comfortable" : "compact";

    private IReadOnlyDictionary<string, object> RootAttributes => BuildAttributes("bzs-theme-provider");

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        BzsThemeContext.ValidateMode(Mode);
        BzsThemeContext.ValidateDensity(Density);
        ArgumentNullException.ThrowIfNull(Theme);

        var isCustomTheme = Theme != BzsThemes.Default;
        if (isCustomTheme && string.IsNullOrWhiteSpace(CspNonce))
        {
            throw new InvalidOperationException(
                "BzsThemeProvider requires a non-empty CspNonce when rendering a runtime custom theme.");
        }

        _customThemeCss = isCustomTheme ? BzsThemeCssBuilder.Build(_scopeId, Theme) : null;
        RebuildContext(isCustomTheme);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var shouldObserveSystemMode = Mode == BzsThemeMode.System;
        if (!shouldObserveSystemMode && !_systemObserverEnabled)
        {
            return;
        }

        _interop ??= new BzsThemeProviderInterop(JS);
        _dotNetReference ??= DotNetObjectReference.Create(this);
        if (_systemObserverEnabled != shouldObserveSystemMode || firstRender)
        {
            try
            {
                var prefersDark = await _interop.SetSystemModeAsync(
                    _rootElement,
                    _dotNetReference,
                    shouldObserveSystemMode);
                _systemObserverEnabled = shouldObserveSystemMode;
                if (shouldObserveSystemMode && ApplySystemPreference(prefersDark))
                {
                    StateHasChanged();
                }
            }
            catch (Exception exception) when (IsTransientInteropFailure(exception))
            {
            }
        }
    }

    /// <summary>Receives browser color-scheme changes while System mode is active.</summary>
    [JSInvokable]
    public async Task OnSystemPreferenceChanged(bool prefersDark)
    {
        if (_disposed || Mode != BzsThemeMode.System || _systemPrefersDark == prefersDark)
        {
            return;
        }

        if (ApplySystemPreference(prefersDark))
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private bool ApplySystemPreference(bool prefersDark)
    {
        if (_systemPrefersDark == prefersDark)
        {
            return false;
        }

        _systemPrefersDark = prefersDark;
        RebuildContext(Theme != BzsThemes.Default);
        return true;
    }

    private void RebuildContext(bool isCustomTheme)
    {
        _context = new BzsThemeContext(
            Theme,
            Mode,
            EffectiveMode,
            Density,
            isCustomTheme,
            ModeChanged,
            DensityChanged);
    }

    private static bool IsTransientInteropFailure(Exception exception) =>
        exception is JSDisconnectedException or TaskCanceledException;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_interop is not null)
        {
            await _interop.DisposeAsync(_rootElement);
        }

        _dotNetReference?.Dispose();
    }
}
