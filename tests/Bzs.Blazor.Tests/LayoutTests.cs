using Bunit;

namespace Bzs.Blazor.Tests;

public sealed class LayoutTests
{
    [Fact]
    public void ContainerRendersContentAndCommonAttributes()
    {
        using var context = new BunitContext();
        var cut = context.Render<BzsContainer>(parameters => parameters
            .Add(component => component.MaxWidth, BzsContainerMaxWidth.Medium)
            .Add(component => component.Id, "content-container")
            .Add(component => component.Class, "consumer-container")
            .Add(component => component.Style, "min-height: 10rem")
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object>
            {
                ["aria-label"] = "Content area",
            })
            .Add(component => component.ChildContent, "Container content"));

        var container = cut.Find("#content-container");
        Assert.Equal("content-container", container.Id);
        Assert.True(container.ClassList.Contains("consumer-container"));
        Assert.Equal("min-height: 10rem;", container.GetAttribute("style"));
        Assert.Equal("Content area", container.GetAttribute("aria-label"));
        Assert.Equal("Container content", container.TextContent.Trim());
    }

    [Fact]
    public void GridAndGridItemRenderConsumerIdentityAndContent()
    {
        using var context = new BunitContext();
        var cut = context.Render<BzsGrid>(parameters => parameters
            .Add(component => component.Id, "responsive-grid")
            .Add(component => component.ChildContent, itemBuilder =>
            {
                itemBuilder.OpenComponent<BzsGridItem>(0);
                itemBuilder.AddAttribute(1, nameof(BzsGridItem.Id), "responsive-item");
                itemBuilder.AddAttribute(2, nameof(BzsGridItem.Xs), 12);
                itemBuilder.AddAttribute(3, nameof(BzsGridItem.Sm), 10);
                itemBuilder.AddAttribute(4, nameof(BzsGridItem.Md), 8);
                itemBuilder.AddAttribute(5, nameof(BzsGridItem.Lg), 6);
                itemBuilder.AddAttribute(6, nameof(BzsGridItem.Xl), 4);
                itemBuilder.AddAttribute(7, nameof(BzsGridItem.Xxl), 3);
                itemBuilder.AddAttribute(8, nameof(BzsGridItem.ChildContent),
                    (Microsoft.AspNetCore.Components.RenderFragment)(contentBuilder =>
                        contentBuilder.AddContent(0, "Responsive item")));
                itemBuilder.CloseComponent();
            }));

        Assert.Equal("responsive-grid", cut.Find("#responsive-grid").Id);
        var item = cut.Find("#responsive-item");
        Assert.Equal("Responsive item", item.TextContent.Trim());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void GridItemRejectsSpansOutsideTheTwelveColumnRange(int span)
    {
        using var context = new BunitContext();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            context.Render<BzsGridItem>(parameters => parameters
                .Add(component => component.Xs, span)));

        Assert.Equal(nameof(BzsGridItem.Xs), exception.ParamName);
    }

    [Fact]
    public void StackRendersConsumerIdentityAndContent()
    {
        using var context = new BunitContext();
        var cut = context.Render<BzsStack>(parameters => parameters
            .Add(component => component.Id, "content-stack")
            .Add(component => component.Row, true)
            .Add(component => component.ChildContent, "Stack content"));

        var stack = cut.Find("#content-stack");
        Assert.Equal("Stack content", stack.TextContent.Trim());
    }

    [Fact]
    public void SpacerIsHiddenFromAssistiveTechnology()
    {
        using var context = new BunitContext();
        var cut = context.Render<BzsSpacer>(parameters => parameters
            .Add(component => component.Id, "content-spacer"));

        Assert.Equal("true", cut.Find("#content-spacer").GetAttribute("aria-hidden"));
    }

    [Theory]
    [InlineData(false, "horizontal")]
    [InlineData(true, "vertical")]
    public void DividerRendersSeparatorSemantics(bool vertical, string orientation)
    {
        using var context = new BunitContext();
        var cut = context.Render<BzsDivider>(parameters => parameters
            .Add(component => component.Id, "content-divider")
            .Add(component => component.Vertical, vertical));

        var divider = cut.Find("#content-divider");
        Assert.Equal("separator", divider.GetAttribute("role"));
        Assert.Equal(orientation, divider.GetAttribute("aria-orientation"));
    }

    [Fact]
    public void LayoutComponentsRejectUnsupportedEnums()
    {
        using var context = new BunitContext();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            context.Render<BzsGrid>(parameters => parameters
                .Add(component => component.Spacing, (BzsLayoutSpacing)999)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            context.Render<BzsDivider>(parameters => parameters
                .Add(component => component.Inset, (BzsDividerInset)999)));
    }
}
