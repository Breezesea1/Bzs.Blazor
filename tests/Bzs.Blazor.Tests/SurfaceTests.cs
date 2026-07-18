using Bunit;

namespace Bzs.Blazor.Tests;

public sealed class SurfaceTests
{
    [Theory]
    [InlineData(BzsSurfaceLevel.Base, "base")]
    [InlineData(BzsSurfaceLevel.Raised, "raised")]
    [InlineData(BzsSurfaceLevel.Inset, "inset")]
    [InlineData(BzsSurfaceLevel.Overlay, "overlay")]
    public void SurfaceRendersItsSemanticLevelAndForwardedAttributes(
        BzsSurfaceLevel level,
        string expectedLevel)
    {
        using var context = new BunitContext();
        var attributes = new Dictionary<string, object>
        {
            ["aria-label"] = "Example surface",
            ["data-test-id"] = "surface-example",
        };

        var cut = context.Render<BzsSurface>(parameters => parameters
            .Add(component => component.Level, level)
            .Add(component => component.AdditionalAttributes, attributes)
            .Add(component => component.ChildContent, "Surface content"));

        var surface = cut.Find("[data-bzs-surface]");
        Assert.Equal(expectedLevel, surface.GetAttribute("data-bzs-surface"));
        Assert.Equal("Example surface", surface.GetAttribute("aria-label"));
        Assert.Equal("surface-example", surface.GetAttribute("data-test-id"));
        Assert.Equal("Surface content", surface.TextContent.Trim());
    }
}
