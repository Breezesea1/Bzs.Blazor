using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor.Tests;

public sealed class ButtonTests
{
    [Fact]
    public void ButtonInvokesClickAndPreservesItsNativeSubmitType()
    {
        using var context = new BunitContext();
        var clickCount = 0;

        var cut = context.Render<BzsButton>(parameters => parameters
            .Add(component => component.Type, BzsButtonType.Submit)
            .Add(component => component.StartIcon, BzsIcons.Check)
            .Add(component => component.EndIcon, BzsIcons.ChevronRight)
            .Add(component => component.Click, (MouseEventArgs _) => clickCount++)
            .Add(component => component.ChildContent, "Save"));

        var button = cut.Find("button");
        Assert.Equal("submit", button.GetAttribute("type"));
        Assert.Equal(2, cut.FindAll("svg").Count);

        button.Click();

        Assert.Equal(1, clickCount);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void DisabledOrLoadingButtonSuppressesClick(bool disabled, bool loading)
    {
        using var context = new BunitContext();
        var clickCount = 0;

        var cut = context.Render<BzsButton>(parameters => parameters
            .Add(component => component.Disabled, disabled)
            .Add(component => component.Loading, loading)
            .Add(component => component.Click, (MouseEventArgs _) => clickCount++)
            .Add(component => component.ChildContent, "Save"));

        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
        Assert.Equal(loading ? "true" : null, button.GetAttribute("aria-busy"));

        button.Click();

        Assert.Equal(0, clickCount);
    }

    [Fact]
    public void IconOnlyButtonRequiresAndUsesAnAccessibleName()
    {
        using var context = new BunitContext();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.Render<BzsButton>(parameters => parameters
                .Add(component => component.StartIcon, BzsIcons.Close)));
        Assert.Contains("AccessibleName", exception.Message, StringComparison.Ordinal);

        var cut = context.Render<BzsButton>(parameters => parameters
            .Add(component => component.StartIcon, BzsIcons.Close)
            .Add(component => component.AccessibleName, "Close dialog")
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object>
            {
                ["aria-describedby"] = "dialog-help",
                ["data-action"] = "close",
            }));

        var button = cut.Find("button");
        Assert.Equal("Close dialog", button.GetAttribute("aria-label"));
        Assert.Equal("dialog-help", button.GetAttribute("aria-describedby"));
        Assert.Equal("close", button.GetAttribute("data-action"));
    }

    [Theory]
    [InlineData(BzsButtonVariant.Primary, BzsButtonSize.Small, "primary", "small")]
    [InlineData(BzsButtonVariant.Secondary, BzsButtonSize.Medium, "secondary", "medium")]
    [InlineData(BzsButtonVariant.Outline, BzsButtonSize.Large, "outline", "large")]
    [InlineData(BzsButtonVariant.Ghost, BzsButtonSize.Medium, "ghost", "medium")]
    [InlineData(BzsButtonVariant.Danger, BzsButtonSize.Small, "danger", "small")]
    public void ButtonReportsItsSemanticVariantAndSize(
        BzsButtonVariant variant,
        BzsButtonSize size,
        string expectedVariant,
        string expectedSize)
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsButton>(parameters => parameters
            .Add(component => component.Variant, variant)
            .Add(component => component.Size, size)
            .Add(component => component.ChildContent, "Action"));

        var button = cut.Find("button");
        Assert.Equal(expectedVariant, button.GetAttribute("data-bzs-variant"));
        Assert.Equal(expectedSize, button.GetAttribute("data-bzs-size"));
    }

    [Fact]
    public void IconOnlyButtonAcceptsAnExternalLabel()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsButton>(parameters => parameters
            .Add(component => component.StartIcon, BzsIcons.Close)
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object>
            {
                ["aria-labelledby"] = "close-label",
            }));

        var button = cut.Find("button");
        Assert.Equal("close-label", button.GetAttribute("aria-labelledby"));
        Assert.Null(button.GetAttribute("aria-label"));
    }
}
