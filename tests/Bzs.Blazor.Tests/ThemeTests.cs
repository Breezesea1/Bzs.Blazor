using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

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
