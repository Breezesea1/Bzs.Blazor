using Bunit;
using Bzs.Blazor.Demo.Client.Components;
using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Tests;

/// <summary>
/// Pins the showcase-section frame that every demo page now shares: the heading level, the
/// accessible name wiring, the optional description, and the classes a page contributes.
/// </summary>
public sealed class DemoShowcaseSectionTests
{
    [Fact]
    public void ASectionLabelsItselfThroughItsHeading()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoShowcaseSection>(parameters => parameters
            .Add(section => section.HeadingId, "surface-title")
            .Add(section => section.Heading, "Surfaces")
            .Add(section => section.Description, "Four semantic levels.")
            .AddChildContent("<p>example</p>"));

        var element = cut.Find("section");
        Assert.Equal("surface-title", element.GetAttribute("aria-labelledby"));
        Assert.Equal("Surfaces", cut.Find("h2#surface-title").TextContent);
        Assert.Equal("Four semantic levels.", cut.Find("header p").TextContent);
        Assert.Equal("example", cut.Find("section > p").TextContent);
    }

    [Fact]
    public void ASubsectionRendersALowerHeadingLevel()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoShowcaseSection>(parameters => parameters
            .Add(section => section.HeadingLevel, 3)
            .Add(section => section.HeadingId, "compact-grid-title")
            .Add(section => section.Heading, "Compact DataGrid")
            .AddChildContent("<table></table>"));

        Assert.Equal("Compact DataGrid", cut.Find("h3#compact-grid-title").TextContent);
        Assert.Empty(cut.FindAll("h2"));
    }

    [Fact]
    public void ASectionWithoutADescriptionRendersNoParagraph()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoShowcaseSection>(parameters => parameters
            .Add(section => section.HeadingId, "toast-title")
            .Add(section => section.Heading, "Toast")
            .AddChildContent("<div></div>"));

        Assert.Empty(cut.FindAll("header p"));
        Assert.NotNull(cut.Find("header h2"));
    }

    [Fact]
    public void MarkupInADescriptionSurvivesAndWinsOverPlainText()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoShowcaseSection>(parameters => parameters
            .Add(section => section.HeadingId, "container-title")
            .Add(section => section.Heading, "Container")
            .Add(section => section.Description, "plain")
            .Add(section => section.DescriptionContent, (RenderFragment)(builder =>
            {
                builder.AddMarkupContent(0, "Use <code>Fixed</code> for stepped widths.");
            }))
            .AddChildContent("<div></div>"));

        var description = cut.Find("header p");
        Assert.Equal("Fixed", cut.Find("header p code").TextContent);
        Assert.DoesNotContain("plain", description.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ASectionIsAPanelUnlessThePageSaysOtherwise()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoShowcaseSection>(parameters => parameters
            .Add(section => section.HeadingId, "message-title")
            .Add(section => section.Heading, "Message")
            .AddChildContent("<div></div>"));

        Assert.Equal("demo-panel", cut.Find("section").GetAttribute("class"));
        Assert.Equal("demo-showcase-section-header", cut.Find("header").GetAttribute("class"));
    }

    [Fact]
    public void APageContributesItsOwnWrapperAndHeaderClasses()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoShowcaseSection>(parameters => parameters
            .Add(section => section.Class, "demo-panel demo-layout-panel demo-layout-panel--info")
            .Add(section => section.HeaderClass, "demo-layout-section-header")
            .Add(section => section.HeadingId, "grid-title")
            .Add(section => section.Heading, "Responsive grid")
            .AddChildContent("<div></div>"));

        Assert.Equal(
            "demo-panel demo-layout-panel demo-layout-panel--info",
            cut.Find("section").GetAttribute("class"));
        Assert.Equal(
            "demo-showcase-section-header demo-layout-section-header",
            cut.Find("header").GetAttribute("class"));
    }

    [Fact]
    public void ALocaleSpecimenForwardsItsLanguageAndDirection()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoShowcaseSection>(parameters => parameters
            .Add(section => section.HeadingId, "rtl-tabs-title")
            .Add(section => section.Heading, "RTL")
            .AddUnmatched("lang", "ar")
            .AddUnmatched("dir", "rtl")
            .AddChildContent("<div></div>"));

        var element = cut.Find("section");
        Assert.Equal("ar", element.GetAttribute("lang"));
        Assert.Equal("rtl", element.GetAttribute("dir"));
    }
}

/// <summary>
/// Pins the layout page's preview-beside-snippet frame, including the caption defaults its call
/// sites rely on by omitting the parameter.
/// </summary>
public sealed class DemoLayoutExampleTests
{
    [Fact]
    public void AnExampleCaptionsALivePreviewAndItsSnippet()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoLayoutExample>(parameters => parameters
            .Add(example => example.CodeSummary, "Fixed breakpoints")
            .Add(example => example.Preview, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<span>Fixed content region</span>")))
            .Add(example => example.Code, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<pre id=\"layout-container-code\"><code>snippet</code></pre>"))));

        Assert.Equal("demo-layout-example", cut.Find("div").GetAttribute("class"));
        Assert.Equal("Live preview", cut.Find(".demo-layout-example-label").TextContent);
        Assert.Equal("Fixed content region", cut.Find(".demo-layout-preview span").TextContent);
        Assert.Equal(
            ["Razor", "Fixed breakpoints"],
            cut.FindAll("figcaption span").Select(caption => caption.TextContent));
        Assert.Equal("snippet", cut.Find("figure pre#layout-container-code code").TextContent);
    }

    [Fact]
    public void AnExampleCanNameItsOwnPreviewLabelLanguageAndFrame()
    {
        using var context = new BunitContext();

        var cut = context.Render<DemoLayoutExample>(parameters => parameters
            .Add(example => example.Class, "demo-layout-example--shell")
            .Add(example => example.PreviewLabel, "Interactive preview")
            .Add(example => example.CodeLanguage, "Razor + CSS")
            .Add(example => example.CodeSummary, "Controlled navigation")
            .Add(example => example.Preview, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<span>shell</span>")))
            .Add(example => example.Code, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<pre><code>shell snippet</code></pre>"))));

        Assert.Equal(
            "demo-layout-example demo-layout-example--shell",
            cut.Find("div").GetAttribute("class"));
        Assert.Equal("Interactive preview", cut.Find(".demo-layout-example-label").TextContent);
        Assert.Equal(
            ["Razor + CSS", "Controlled navigation"],
            cut.FindAll("figcaption span").Select(caption => caption.TextContent));
    }
}
