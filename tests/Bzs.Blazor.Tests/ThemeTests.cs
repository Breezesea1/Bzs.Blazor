using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
    public void BuiltInInputBoundariesMeetNonTextContrast()
    {
        foreach (var colors in new[] { BzsThemes.Light, BzsThemes.Dark })
        {
            Assert.True(
                GetContrastRatio(colors.Border, colors.Canvas) >= 3,
                $"{colors.Border} must contrast with canvas {colors.Canvas} by at least 3:1.");
            Assert.True(
                GetContrastRatio(colors.Border, colors.SurfaceInset) >= 3,
                $"{colors.Border} must contrast with input fill {colors.SurfaceInset} by at least 3:1.");
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

        AssertThemeBlockMatches(stylesheet, "light", BzsThemes.Light, BzsThemes.Default.LightDepth);
        AssertThemeBlockMatches(stylesheet, "dark", BzsThemes.Dark, BzsThemes.Default.DarkDepth);
        AssertSharedThemeBlockMatches(stylesheet, BzsThemes.Default);
    }

    [Fact]
    public void CustomThemeRequiresAndEmitsACspNonce()
    {
        using var context = new BunitContext();
        var customTheme = BzsThemes.Default with
        {
            LightColors = BzsThemes.Light with { Primary = "#0055aa" },
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

    private static double GetContrastRatio(string first, string second)
    {
        var firstLuminance = GetRelativeLuminance(first);
        var secondLuminance = GetRelativeLuminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05)
            / (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    private static void AssertThemeBlockMatches(
        string stylesheet,
        string mode,
        BzsThemeColors colors,
        BzsThemeDepth depth)
    {
        var selector = mode == "light"
            ? @":root,\s*\[data-bzs-theme=""light""\]"
            : @"\[data-bzs-theme=""dark""\]";
        var match = Regex.Match(
            stylesheet,
            $@"{selector}\s*\{{(?<body>.*?)\}}",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"The static stylesheet is missing the {mode} theme block.");

        var properties = ParseCustomProperties(match.Groups["body"].Value, $"{mode} theme block");
        var tokens = new (string Name, string Value)[]
        {
            ("canvas", colors.Canvas),
            ("surface", colors.Surface),
            ("surface-raised", colors.SurfaceRaised),
            ("surface-inset", colors.SurfaceInset),
            ("surface-overlay", colors.SurfaceOverlay),
            ("text", colors.Text),
            ("text-muted", colors.TextMuted),
            ("border", colors.Border),
            ("focus-ring", colors.FocusRing),
            ("primary", colors.Primary),
            ("on-primary", colors.OnPrimary),
            ("success", colors.Success),
            ("warning", colors.Warning),
            ("error", colors.Error),
            ("info", colors.Info),
            ("disabled-surface", colors.DisabledSurface),
            ("disabled-text", colors.DisabledText),
            ("shadow-raised", depth.RaisedShadow),
            ("shadow-inset", depth.InsetShadow),
            ("shadow-overlay", depth.OverlayShadow),
            ("shadow-focus", depth.FocusShadow),
        };

        foreach (var (name, value) in tokens)
        {
            Assert.True(properties.TryGetValue(name, out var actual), $"The {mode} theme block is missing --bzs-{name}.");
            Assert.Equal(value, actual);
        }
    }

    private static void AssertSharedThemeBlockMatches(string stylesheet, BzsTheme theme)
    {
        var match = Regex.Match(
            stylesheet,
            @":root,\s*\[data-bzs-theme\]\s*\{(?<body>.*?)\}",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(match.Success, "The static stylesheet is missing the shared theme block.");

        var properties = ParseCustomProperties(match.Groups["body"].Value, "shared theme block");
        var tokens = new (string Name, string Value)[]
        {
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
        };

        foreach (var (name, value) in tokens)
        {
            Assert.True(properties.TryGetValue(name, out var actual), $"The shared theme block is missing --bzs-{name}.");
            Assert.Equal(value, actual);
        }
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
