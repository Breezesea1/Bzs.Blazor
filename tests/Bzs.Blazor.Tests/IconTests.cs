using Bunit;

namespace Bzs.Blazor.Tests;

public sealed class IconTests
{
    [Fact]
    public void DecorativeIconIsHiddenFromAssistiveTechnology()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsIcon>(parameters => parameters
            .Add(component => component.Icon, BzsIcons.Close));

        var icon = cut.Find("svg");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.Null(icon.GetAttribute("role"));
        Assert.Null(icon.GetAttribute("aria-label"));
    }

    [Fact]
    public void MeaningfulIconUsesItsAccessibleName()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsIcon>(parameters => parameters
            .Add(component => component.Icon, BzsIcons.Warning)
            .Add(component => component.AccessibleName, "Warning"));

        var icon = cut.Find("svg");
        Assert.Equal("img", icon.GetAttribute("role"));
        Assert.Equal("Warning", icon.GetAttribute("aria-label"));
        Assert.Null(icon.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void ConsumerCreatedDataRendersWithCurrentColor()
    {
        using var context = new BunitContext();
        var iconData = new BzsIconData("M3 3h18");

        var cut = context.Render<BzsIcon>(parameters => parameters
            .Add(component => component.Icon, iconData));

        var icon = cut.Find("svg");
        Assert.Equal("0 0 24 24", icon.GetAttribute("viewBox"));
        Assert.Equal("currentColor", icon.GetAttribute("stroke"));
        Assert.Equal("M3 3h18", cut.Find("path").GetAttribute("d"));
    }

    [Fact]
    public void CuratedPasswordVisibilityIconsRenderAsDecorativeGeometry()
    {
        using var context = new BunitContext();

        var eye = context.Render<BzsIcon>(parameters => parameters
            .Add(component => component.Icon, BzsIcons.Eye));
        var eyeOff = context.Render<BzsIcon>(parameters => parameters
            .Add(component => component.Icon, BzsIcons.EyeOff));

        Assert.False(string.IsNullOrWhiteSpace(eye.Find("path").GetAttribute("d")));
        Assert.False(string.IsNullOrWhiteSpace(eyeOff.Find("path").GetAttribute("d")));
        Assert.Equal("true", eye.Find("svg").GetAttribute("aria-hidden"));
        Assert.Equal("true", eyeOff.Find("svg").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void ExternallyLabelledIconRemainsExposed()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsIcon>(parameters => parameters
            .Add(component => component.Icon, BzsIcons.Info)
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object>
            {
                ["aria-labelledby"] = "icon-label",
            }));

        var icon = cut.Find("svg");
        Assert.Equal("img", icon.GetAttribute("role"));
        Assert.Equal("icon-label", icon.GetAttribute("aria-labelledby"));
        Assert.Null(icon.GetAttribute("aria-hidden"));
        Assert.Null(icon.GetAttribute("aria-label"));
    }
}
