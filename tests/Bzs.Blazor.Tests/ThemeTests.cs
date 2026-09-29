using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace Bzs.Blazor.Tests;

public sealed class ThemeTests
{
    [Fact]
    public void BuiltInThemeRendersExternalCssSelectorsOnly()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsThemeProvider>(parameters => parameters
            .Add(component => component.Mode, BzsThemeMode.Dark)
            .Add(component => component.Density, BzsDensity.Comfortable)
            .Add(component => component.ChildContent, "Themed content"));

        var root = cut.Find(".bzs-theme-provider");
        Assert.Equal("dark", root.GetAttribute("data-bzs-theme"));
        Assert.Equal("comfortable", root.GetAttribute("data-bzs-density"));
        Assert.DoesNotContain("<style", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Themed content", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltInLoadBearingBoundariesMeetNonTextContrastOnEverySurface()
    {
        foreach (var colors in new[] { BzsThemes.Light, BzsThemes.Dark })
        {
            var surfaces = new (string Name, string Value)[]
            {
                (nameof(colors.Canvas), colors.Canvas),
                (nameof(colors.Surface), colors.Surface),
                (nameof(colors.SurfaceRaised), colors.SurfaceRaised),
                (nameof(colors.SurfaceInset), colors.SurfaceInset),
                (nameof(colors.SurfaceOverlay), colors.SurfaceOverlay),
            };

            foreach (var (name, value) in surfaces)
            {
                Assert.True(
                    GetContrastRatio(colors.Border, value) >= 3,
                    $"{colors.Border} must contrast with {name} {value} by at least 3:1.");
            }
        }
    }

    [Fact]
    public void BuiltInInteriorRulesStayWeakerThanLoadBearingBoundaries()
    {
        foreach (var colors in new[] { BzsThemes.Light, BzsThemes.Dark })
        {
            var surfaces = new (string Name, string Value)[]
            {
                (nameof(colors.Canvas), colors.Canvas),
                (nameof(colors.Surface), colors.Surface),
                (nameof(colors.SurfaceRaised), colors.SurfaceRaised),
                (nameof(colors.SurfaceInset), colors.SurfaceInset),
                (nameof(colors.SurfaceOverlay), colors.SurfaceOverlay),
            };

            foreach (var (name, value) in surfaces)
            {
                var subtle = GetContrastRatio(colors.BorderSubtle, value);
                Assert.True(
                    subtle < GetContrastRatio(colors.Border, value),
                    $"{colors.BorderSubtle} must read more softly than {colors.Border} on {name} {value}.");
                Assert.True(
                    subtle >= 1.5,
                    $"{colors.BorderSubtle} must stay perceptible against {name} {value}.");
            }
        }
    }

    [Fact]
    public void BuiltInInformationTextMeetsTextContrastOnBaseSurfaces()
    {
        foreach (var colors in new[] { BzsThemes.Light, BzsThemes.Dark })
        {
            Assert.True(
                GetContrastRatio(colors.Info, colors.Surface) >= 4.5,
                $"{colors.Info} must contrast with surface {colors.Surface} by at least 4.5:1.");
        }
    }

    [Fact]
    public void BuiltInThemeRecordsMatchStaticCss()
    {
        var stylesheet = File.ReadAllText(FindRepositoryFile(
            "src",
            "Bzs.Blazor",
            "wwwroot",
            "bzs.blazor.css"));

        var light = AssertThemeBlockMatches(stylesheet, "light", BzsThemes.Light, BzsThemes.Default.LightDepth);
        var dark = AssertThemeBlockMatches(stylesheet, "dark", BzsThemes.Dark, BzsThemes.Default.DarkDepth);
        var shared = AssertSharedThemeBlockMatches(stylesheet, BzsThemes.Default);

        AssertTokenNamesExactly(light, SchemeTokenNames(), "light theme block");
        AssertTokenNamesExactly(dark, SchemeTokenNames(), "dark theme block");
        AssertTokenNamesExactly(
            shared,
            SharedRecordTokenNames().Concat(CssOnlyStructuralTokenNames()),
            "shared theme block");
    }

    [Fact]
    public void ThemeCssBuilderEmissionsMatchStaticCss()
    {
        var stylesheet = File.ReadAllText(FindRepositoryFile(
            "src",
            "Bzs.Blazor",
            "wwwroot",
            "bzs.blazor.css"));
        var light = ParseThemeBlock(
            stylesheet,
            @":root,\s*\[data-bzs-theme=""light""\]",
            "light theme block");
        var dark = ParseThemeBlock(
            stylesheet,
            @"\[data-bzs-theme=""dark""\]",
            "dark theme block");
        var shared = ParseThemeBlock(
            stylesheet,
            @":root,\s*\[data-bzs-theme\]",
            "shared theme block");

        var built = BzsThemeCssBuilder.Build("drift-probe", BzsThemes.Default);
        var builtLight = ParseThemeBlock(
            built,
            @"\[data-bzs-theme-scope=""drift-probe""\]\[data-bzs-theme=""light""\]",
            "builder light block");
        var builtDark = ParseThemeBlock(
            built,
            @"\[data-bzs-theme-scope=""drift-probe""\]\[data-bzs-theme=""dark""\]",
            "builder dark block");

        AssertTokenNamesExactly(
            builtLight,
            SchemeTokenNames().Concat(SharedRecordTokenNames()),
            "builder light block");
        AssertTokenNamesExactly(
            builtDark,
            SchemeTokenNames().Concat(SharedRecordTokenNames()),
            "builder dark block");

        foreach (var (name, _) in SchemeTokens(BzsThemes.Light, BzsThemes.Default.LightDepth))
        {
            Assert.Equal(light[name], builtLight[name]);
        }

        foreach (var (name, _) in SchemeTokens(BzsThemes.Dark, BzsThemes.Default.DarkDepth))
        {
            Assert.Equal(dark[name], builtDark[name]);
        }

        foreach (var name in SharedRecordTokenNames())
        {
            Assert.Equal(shared[name], builtLight[name]);
            Assert.Equal(shared[name], builtDark[name]);
        }

        AssertAccessibilityOverridesMatch(stylesheet, built);
    }

    [Fact]
    public void CustomThemeRequiresAndEmitsACspNonce()
    {
        using var context = new BunitContext();
        var customTheme = BzsThemes.Default with
        {
            LightColors = BzsThemes.Light with
            {
                Primary = "#0055aa",
                Scrim = "rgb(1 2 3 / 0.5)",
            },
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.Render<BzsThemeProvider>(parameters => parameters
                .Add(component => component.Theme, customTheme)
                .Add(component => component.ChildContent, "Custom")));
        Assert.Contains("CspNonce", exception.Message, StringComparison.Ordinal);

        var cut = context.Render<BzsThemeProvider>(parameters => parameters
            .Add(component => component.Theme, customTheme)
            .Add(component => component.CspNonce, "test-nonce")
            .Add(component => component.ChildContent, "Custom"));

        var style = cut.Find("style");
        Assert.Equal("test-nonce", style.GetAttribute("nonce"));
        Assert.Contains("--bzs-primary:#0055aa", style.TextContent, StringComparison.Ordinal);
        Assert.Contains("--bzs-scrim:rgb(1 2 3 / 0.5)", style.TextContent, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion:reduce", style.TextContent, StringComparison.Ordinal);
        Assert.Contains("forced-colors:active", style.TextContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("red;}body{display:none}")]
    [InlineData("</style><script>alert(1)</script>")]
    [InlineData("red !important")]
    [InlineData("120ms ! important")]
    [InlineData("red !/**/important")]
    [InlineData("red/*")]
    [InlineData("rgb(1 2 3")]
    [InlineData("'unterminated")]
    [InlineData("url(https://example.invalid/tracker)")]
    public void CustomThemeRejectsScopeBreakingTokens(string hostileValue)
    {
        using var context = new BunitContext();
        var customTheme = BzsThemes.Default with
        {
            LightColors = BzsThemes.Light with { Primary = hostileValue },
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.Render<BzsThemeProvider>(parameters => parameters
                .Add(component => component.Theme, customTheme)
                .Add(component => component.CspNonce, "test-nonce")
                .Add(component => component.ChildContent, "Custom")));

        Assert.Contains("not allowed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SystemPreferenceUpdatesOnlyTheEffectiveMode()
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(
            "./_content/Bzs.Blazor/Components/Theme/BzsThemeProvider.razor.js");
        module.Setup<bool>("setSystemMode", _ => true).SetResult(false);

        var cut = context.Render<BzsThemeProvider>(parameters => parameters
            .Add(component => component.Mode, BzsThemeMode.System)
            .Add(component => component.ChildContent, "System"));

        Assert.Equal("light", cut.Find(".bzs-theme-provider").GetAttribute("data-bzs-theme"));

        await cut.InvokeAsync(() => cut.Instance.OnSystemPreferenceChanged(true));

        Assert.Equal("dark", cut.Find(".bzs-theme-provider").GetAttribute("data-bzs-theme"));
    }

    [Fact]
    public void SystemModeSetupSwallowsJsDisconnectedExceptionAndRetries()
    {
        AssertTransientSystemModeSetupIsRecoverable(new JSDisconnectedException("Circuit disconnected."));
    }

    [Fact]
    public void SystemModeSetupSwallowsTaskCanceledExceptionAndRetries()
    {
        AssertTransientSystemModeSetupIsRecoverable(new TaskCanceledException());
    }

    [Fact]
    public async Task SystemModeDisposalSwallowsJsDisconnectedException()
    {
        await AssertTransientSystemModeDisposalIsRecoverableAsync(
            new JSDisconnectedException("Circuit disconnected."));
    }

    [Fact]
    public async Task SystemModeDisposalSwallowsTaskCanceledException()
    {
        await AssertTransientSystemModeDisposalIsRecoverableAsync(new TaskCanceledException());
    }

    [Fact]
    public async Task CascadedContextRequestsControlledChanges()
    {
        using var context = new BunitContext();
        BzsThemeMode? requestedMode = null;
        BzsDensity? requestedDensity = null;

        var cut = context.Render<BzsThemeProvider>(parameters => parameters
            .Add(component => component.ModeChanged, mode => requestedMode = mode)
            .Add(component => component.DensityChanged, density => requestedDensity = density)
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<ThemeContextProbe>(0);
                builder.CloseComponent();
            }));
        var probe = cut.FindComponent<ThemeContextProbe>();

        await probe.Instance.Context.RequestModeAsync(BzsThemeMode.Dark);
        await probe.Instance.Context.RequestDensityAsync(BzsDensity.Comfortable);

        Assert.Equal(BzsThemeMode.Dark, requestedMode);
        Assert.Equal(BzsDensity.Comfortable, requestedDensity);
        Assert.Equal(BzsThemeMode.Light, cut.Instance.Mode);
        Assert.Equal(BzsDensity.Compact, cut.Instance.Density);
    }

    [Fact]
    public void NestedProvidersLogAFreezeWarningOncePerInstance()
    {
        using var context = new BunitContext();
        var warnings = new List<string>();
        context.Services.AddSingleton<ILoggerFactory>(new CapturingLoggerFactory(warnings));

        var cut = context.Render<BzsThemeProvider>(parameters => parameters
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsThemeProvider>(0);
                builder.AddAttribute(
                    1,
                    nameof(BzsThemeProvider.ChildContent),
                    (RenderFragment)(content => content.AddContent(0, "Nested scope")));
                builder.CloseComponent();
            }));
        Assert.Single(warnings);
        Assert.Contains("nested BzsThemeProvider", warnings[0], StringComparison.OrdinalIgnoreCase);

        cut.Render();

        Assert.Single(warnings);
    }

    [Fact]
    public void TopLevelProvidersDoNotLogANestingWarning()
    {
        using var context = new BunitContext();
        var warnings = new List<string>();
        context.Services.AddSingleton<ILoggerFactory>(new CapturingLoggerFactory(warnings));

        _ = context.Render<BzsThemeProvider>(parameters => parameters
            .Add(component => component.ChildContent, "Top level"));

        Assert.Empty(warnings);
    }

    [Fact]
    public void CommonAttributesMergeWithoutMutatingInputs()
    {
        using var context = new BunitContext();
        var additional = new Dictionary<string, object>
        {
            ["id"] = "additional-id",
            ["class"] = "additional-class",
            ["style"] = "padding: 1rem",
            ["aria-label"] = "Attribute probe",
        };

        var cut = context.Render<AttributeProbe>(parameters => parameters
            .Add(component => component.Id, "parameter-id")
            .Add(component => component.Class, "consumer-class")
            .Add(component => component.Style, "margin: 0")
            .Add(component => component.ComponentClass, "component-class")
            .Add(component => component.AdditionalAttributes, additional));
        var root = cut.Find("div");

        Assert.Equal("parameter-id", root.Id);
        Assert.Equal("component-class additional-class consumer-class", root.ClassName);
        Assert.Equal("color: red; padding: 1rem; margin: 0;", root.GetAttribute("style"));
        Assert.Equal("Attribute probe", root.GetAttribute("aria-label"));
        Assert.Equal("additional-id", additional["id"]);
    }

    private sealed class ThemeContextProbe : ComponentBase
    {
        [CascadingParameter]
        public BzsThemeContext Context { get; set; } = BzsThemeContext.Default;
    }

    private sealed class CapturingLoggerFactory(List<string> warnings) : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(warnings);

        public void Dispose() { }
    }

    private sealed class CapturingLogger(List<string> warnings) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                warnings.Add(formatter(state, exception));
            }
        }
    }

    private static double GetContrastRatio(string first, string second)
    {
        var firstLuminance = GetRelativeLuminance(first);
        var secondLuminance = GetRelativeLuminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05)
            / (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    private static IReadOnlyDictionary<string, string> AssertThemeBlockMatches(
        string stylesheet,
        string mode,
        BzsThemeColors colors,
        BzsThemeDepth depth)
    {
        var selector = mode == "light"
            ? @":root,\s*\[data-bzs-theme=""light""\]"
            : @"\[data-bzs-theme=""dark""\]";
        var properties = ParseThemeBlock(stylesheet, selector, $"{mode} theme block");

        foreach (var (name, value) in SchemeTokens(colors, depth))
        {
            Assert.True(properties.TryGetValue(name, out var actual), $"The {mode} theme block is missing --bzs-{name}.");
            Assert.Equal(value, actual);
        }

        return properties;
    }

    private static IReadOnlyDictionary<string, string> AssertSharedThemeBlockMatches(string stylesheet, BzsTheme theme)
    {
        var properties = ParseThemeBlock(stylesheet, @":root,\s*\[data-bzs-theme\]", "shared theme block");

        foreach (var (name, value) in SharedRecordTokens(theme).Concat(CssOnlyStructuralTokens()))
        {
            Assert.True(properties.TryGetValue(name, out var actual), $"The shared theme block is missing --bzs-{name}.");
            Assert.Equal(value, actual);
        }

        return properties;
    }

    private static (string Name, string Value)[] SchemeTokens(BzsThemeColors colors, BzsThemeDepth depth) =>
    [
        ("canvas", colors.Canvas),
        ("surface", colors.Surface),
        ("surface-raised", colors.SurfaceRaised),
        ("surface-inset", colors.SurfaceInset),
        ("surface-overlay", colors.SurfaceOverlay),
        ("text", colors.Text),
        ("text-muted", colors.TextMuted),
        ("border", colors.Border),
        ("border-subtle", colors.BorderSubtle),
        ("focus-ring", colors.FocusRing),
        ("primary", colors.Primary),
        ("on-primary", colors.OnPrimary),
        ("success", colors.Success),
        ("warning", colors.Warning),
        ("error", colors.Error),
        ("info", colors.Info),
        ("disabled-surface", colors.DisabledSurface),
        ("disabled-text", colors.DisabledText),
        ("scrim", colors.Scrim),
        ("shadow-raised", depth.RaisedShadow),
        ("shadow-inset", depth.InsetShadow),
        ("shadow-overlay", depth.OverlayShadow),
        ("shadow-focus", depth.FocusShadow),
    ];

    private static (string Name, string Value)[] SharedRecordTokens(BzsTheme theme) =>
    [
        ("radius-control", theme.Shape.ControlRadius),
        ("radius-container", theme.Shape.ContainerRadius),
        ("radius-overlay", theme.Shape.OverlayRadius),
        ("border-width", theme.Shape.BorderWidth),
        ("font-family", theme.Typography.FontFamily),
        ("font-size", theme.Typography.FontSize),
        ("font-size-small", theme.Typography.SmallFontSize),
        ("line-height", theme.Typography.LineHeight),
        ("font-weight-regular", theme.Typography.FontWeightRegular),
        ("font-weight-medium", theme.Typography.FontWeightMedium),
        ("font-weight-bold", theme.Typography.FontWeightBold),
        ("motion-fast", theme.Motion.FastDuration),
        ("motion-normal", theme.Motion.NormalDuration),
        ("motion-slow", theme.Motion.SlowDuration),
        ("motion-easing", theme.Motion.Easing),
    ];

    private static (string Name, string Value)[] CssOnlyStructuralTokens() =>
    [
        ("control-height", "2.25rem"),
        ("control-padding-inline", "0.75rem"),
        ("control-gap", "0.5rem"),
        ("layout-spacing-extra-small", "0.25rem"),
        ("layout-spacing-small", "0.5rem"),
        ("layout-spacing-medium", "0.75rem"),
        ("layout-spacing-large", "1rem"),
        ("layout-spacing-extra-large", "1.5rem"),
    ];

    private static IEnumerable<string> SchemeTokenNames() =>
        SchemeTokens(BzsThemes.Light, BzsThemes.Default.LightDepth).Select(token => token.Name);

    private static IEnumerable<string> SharedRecordTokenNames() =>
        SharedRecordTokens(BzsThemes.Default).Select(token => token.Name);

    private static IEnumerable<string> CssOnlyStructuralTokenNames() =>
        CssOnlyStructuralTokens().Select(token => token.Name);

    private static void AssertTokenNamesExactly(
        IReadOnlyDictionary<string, string> properties,
        IEnumerable<string> expected,
        string description)
    {
        var expectedNames = expected.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var actualNames = properties.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.True(
            actualNames.SequenceEqual(expectedNames),
            $"The {description} declares [{string.Join(", ", actualNames)}] "
            + $"instead of [{string.Join(", ", expectedNames)}].");
    }

    private static void AssertAccessibilityOverridesMatch(string staticCss, string builtCss)
    {
        var staticReducedMotion = staticCss[staticCss.IndexOf("prefers-reduced-motion", StringComparison.Ordinal)..];
        var builtReducedMotion = builtCss[builtCss.IndexOf("prefers-reduced-motion", StringComparison.Ordinal)..];
        var staticForcedColors = staticCss[staticCss.IndexOf("forced-colors: active", StringComparison.Ordinal)..];
        var builtForcedColors = builtCss[builtCss.IndexOf("forced-colors:active", StringComparison.Ordinal)..];

        foreach (var name in (string[])["motion-fast", "motion-normal", "motion-slow"])
        {
            Assert.Equal(
                MatchTokenValue(staticReducedMotion, name),
                MatchTokenValue(builtReducedMotion, name));
        }

        foreach (var name in (string[])
                 [
                     "border", "border-subtle", "focus-ring",
                     "shadow-raised", "shadow-inset", "shadow-overlay", "shadow-focus",
                 ])
        {
            Assert.Equal(
                MatchTokenValue(staticForcedColors, name),
                MatchTokenValue(builtForcedColors, name));
        }
    }

    private static string MatchTokenValue(string css, string name)
    {
        var match = Regex.Match(
            css,
            $@"--bzs-{name}\s*:\s*(?<value>[^;{{}}]+);",
            RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"The CSS snippet does not declare --bzs-{name}.");
        return match.Groups["value"].Value.Trim();
    }

    private static IReadOnlyDictionary<string, string> ParseThemeBlock(
        string css,
        string selectorPattern,
        string description)
    {
        var match = Regex.Match(
            css,
            $@"{selectorPattern}\s*\{{(?<body>.*?)\}}",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"The CSS snippet is missing the {description}.");
        return ParseCustomProperties(match.Groups["body"].Value, description);
    }

    private static IReadOnlyDictionary<string, string> ParseCustomProperties(string block, string description)
    {
        var uncommented = Regex.Replace(
            block,
            @"/\*.*?\*/",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match declaration in Regex.Matches(
            uncommented,
            @"--bzs-(?<name>[a-z0-9-]+)\s*:\s*(?<value>[^;{}]+);",
            RegexOptions.CultureInvariant))
        {
            var name = declaration.Groups["name"].Value;
            var value = declaration.Groups["value"].Value.Trim();
            Assert.True(
                properties.TryAdd(name, value),
                $"The {description} declares --bzs-{name} more than once.");
        }

        return properties;
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bzs.Blazor.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Bzs.Blazor repository root.");
    }

    private static double GetRelativeLuminance(string color)
    {
        var channels = new[] { 1, 3, 5 }
            .Select(index => Convert.ToInt32(color.Substring(index, 2), 16) / 255d)
            .Select(channel => channel <= 0.04045
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4))
            .ToArray();
        return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
    }

    private static void AssertTransientSystemModeSetupIsRecoverable(Exception exception)
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(
            "./_content/Bzs.Blazor/Components/Theme/BzsThemeProvider.razor.js");
        var setup = module
            .Setup<bool>("setSystemMode", _ => true)
            .SetException(exception);

        var cut = context.Render<BzsThemeProvider>(parameters => parameters
            .Add(component => component.Mode, BzsThemeMode.System)
            .Add(component => component.ChildContent, "System"));

        setup.SetResult(false);
        cut.Render();

        setup.VerifyInvoke("setSystemMode", 2);
    }

    private static async Task AssertTransientSystemModeDisposalIsRecoverableAsync(Exception exception)
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(
            "./_content/Bzs.Blazor/Components/Theme/BzsThemeProvider.razor.js");
        module.Setup<bool>("setSystemMode", _ => true).SetResult(false);
        var dispose = module.SetupVoid("dispose", _ => true).SetException(exception);

        var cut = context.Render<BzsThemeProvider>(parameters => parameters
            .Add(component => component.Mode, BzsThemeMode.System)
            .Add(component => component.ChildContent, "System"));

        await cut.Instance.DisposeAsync();

        dispose.VerifyInvoke("dispose");
    }

    private sealed class AttributeProbe : BzsComponentBase
    {
        [Parameter]
        public string? ComponentClass { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, BuildAttributes(ComponentClass, "color: red"));
            builder.CloseElement();
        }
    }
}
