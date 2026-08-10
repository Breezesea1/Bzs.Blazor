using Bunit;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor.Tests;

public sealed class StatusPrimitiveTests
{
    [Theory]
    [InlineData(BzsSkeletonShape.Text, BzsSkeletonSize.Small, "text", "small")]
    [InlineData(BzsSkeletonShape.Rectangle, BzsSkeletonSize.Medium, "rectangle", "medium")]
    [InlineData(BzsSkeletonShape.Circle, BzsSkeletonSize.Large, "circle", "large")]
    public void SkeletonIsDecorativeAndReportsItsPresentation(
        BzsSkeletonShape shape,
        BzsSkeletonSize size,
        string expectedShape,
        string expectedSize)
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsSkeleton>(parameters => parameters
            .Add(component => component.Shape, shape)
            .Add(component => component.Size, size)
            .Add(component => component.Animated, false)
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object>
            {
                ["aria-label"] = "Loading account",
                ["data-region"] = "account",
            }));

        var skeleton = cut.Find("span");
        Assert.Equal("true", skeleton.GetAttribute("aria-hidden"));
        Assert.Null(skeleton.GetAttribute("aria-label"));
        Assert.Equal(expectedShape, skeleton.GetAttribute("data-bzs-skeleton-shape"));
        Assert.Equal(expectedSize, skeleton.GetAttribute("data-bzs-skeleton-size"));
        Assert.Equal("false", skeleton.GetAttribute("data-bzs-skeleton-animated"));
        Assert.Equal("account", skeleton.GetAttribute("data-region"));
    }

    [Fact]
    public void BadgeBoundsCountsAndDefinesZeroVisibility()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsBadge>(parameters => parameters
            .Add(component => component.Count, 120)
            .Add(component => component.Maximum, 99)
            .Add(component => component.Severity, BzsMessageSeverity.Error)
            .Add(component => component.AccessibleName, "120 unread messages"));

        var badge = cut.Find("span.bzs-badge");
        Assert.Equal("99+", cut.Find("span.bzs-badge__content").TextContent);
        Assert.Equal("error", badge.GetAttribute("data-bzs-badge-severity"));
        Assert.Equal("120 unread messages", badge.GetAttribute("aria-label"));
        Assert.Equal("true", cut.Find("svg").GetAttribute("aria-hidden"));

        var hiddenZero = context.Render<BzsBadge>(parameters => parameters
            .Add(component => component.Count, 0)
            .Add(component => component.Maximum, 99)
            .Add(component => component.ShowZero, false));

        Assert.Empty(hiddenZero.FindAll("span.bzs-badge"));

        var visibleZero = context.Render<BzsBadge>(parameters => parameters
            .Add(component => component.Count, 0)
            .Add(component => component.Maximum, 99)
            .Add(component => component.ShowZero, true));

        Assert.Equal("0", visibleZero.Find("span.bzs-badge__content").TextContent);
    }

    [Fact]
    public void BadgeRendersComposedTextAndRejectsAmbiguousContent()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsBadge>(parameters => parameters
            .Add(component => component.ChildContent, "Ready")
            .Add(component => component.Severity, BzsMessageSeverity.Success));

        Assert.Equal("Ready", cut.Find("span.bzs-badge__content").TextContent);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.Render<BzsBadge>(parameters => parameters
                .Add(component => component.Count, 1)
                .Add(component => component.ChildContent, "One")));
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BadgeFormatsVisibleCountsWithTheCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fa-IR");
            using var context = new BunitContext();
            var cut = context.Render<BzsBadge>(parameters => parameters
                .Add(component => component.Count, 120)
                .Add(component => component.Maximum, 99));

            Assert.Equal(
                $"{99.ToString(CultureInfo.CurrentCulture)}+",
                cut.Find("span.bzs-badge__content").TextContent);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ChipSelectionAndRemovalAreSeparateControlledCommands()
    {
        using var context = new BunitContext();
        bool? requestedSelection = null;
        var removalCount = 0;

        var cut = context.Render<BzsChip>(parameters => parameters
            .Add(component => component.Selectable, true)
            .Add(component => component.Selected, false)
            .Add(component => component.SelectedChanged, value => requestedSelection = value)
            .Add(component => component.Removable, true)
            .Add(component => component.RemoveAccessibleName, "Remove Finance filter")
            .Add(component => component.RemoveRequested, (MouseEventArgs _) => removalCount++)
            .Add(component => component.StartIcon, BzsIcons.Info)
            .Add(component => component.ChildContent, "Finance"));

        var select = cut.Find("button[data-bzs-chip-command='select']");
        var remove = cut.Find("button[data-bzs-chip-command='remove']");
        Assert.Equal("false", select.GetAttribute("aria-pressed"));
        Assert.Equal("Remove Finance filter", remove.GetAttribute("aria-label"));

        select.Click();
        Assert.True(requestedSelection);
        Assert.Equal("false", select.GetAttribute("aria-pressed"));
        Assert.Equal(0, removalCount);

        remove.Click();
        Assert.Equal(1, removalCount);
        Assert.True(requestedSelection);
    }

    [Fact]
    public void DisabledChipSuppressesBothCommands()
    {
        using var context = new BunitContext();
        var selectionCount = 0;
        var removalCount = 0;

        var cut = context.Render<BzsChip>(parameters => parameters
            .Add(component => component.Selectable, true)
            .Add(component => component.SelectedChanged, _ => selectionCount++)
            .Add(component => component.Removable, true)
            .Add(component => component.RemoveAccessibleName, "Remove filter")
            .Add(component => component.RemoveRequested, (MouseEventArgs _) => removalCount++)
            .Add(component => component.Disabled, true)
            .Add(component => component.ChildContent, "Finance"));

        Assert.Equal("true", cut.Find("span.bzs-chip").GetAttribute("aria-disabled"));
        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));

        cut.Find("button[data-bzs-chip-command='select']").Click();
        cut.Find("button[data-bzs-chip-command='remove']").Click();

        Assert.Equal(0, selectionCount);
        Assert.Equal(0, removalCount);
    }

    [Fact]
    public void AvatarRendersNativeImageFallbackWithoutChangingItsRootDimensions()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsAvatar>(parameters => parameters
            .Add(component => component.ImageUrl, "/people/ada.png")
            .Add(component => component.Initials, "AL")
            .Add(component => component.AccessibleName, "Ada Lovelace")
            .Add(component => component.Size, BzsAvatarSize.Large)
            .Add(component => component.Shape, BzsAvatarShape.Rounded));

        var avatar = cut.Find("span.bzs-avatar");
        Assert.Equal("img", avatar.GetAttribute("role"));
        Assert.Equal("Ada Lovelace", avatar.GetAttribute("aria-label"));
        Assert.Equal("large", avatar.GetAttribute("data-bzs-avatar-size"));
        Assert.Equal("rounded", avatar.GetAttribute("data-bzs-avatar-shape"));
        Assert.Equal("/people/ada.png", cut.Find("img").GetAttribute("src"));
        Assert.Equal(string.Empty, cut.Find("img").GetAttribute("alt"));
        Assert.Equal("AL", cut.Find("span.bzs-avatar__initials").TextContent);
        Assert.Equal("true", cut.Find("img").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void AvatarUsesIconFallbackAndIsDecorativeWithoutAName()
    {
        using var context = new BunitContext();

        var cut = context.Render<BzsAvatar>(parameters => parameters
            .Add(component => component.Icon, BzsIcons.Package));

        var avatar = cut.Find("span.bzs-avatar");
        Assert.Equal("true", avatar.GetAttribute("aria-hidden"));
        Assert.Null(avatar.GetAttribute("role"));
        Assert.Empty(cut.FindAll("img"));
        Assert.Single(cut.FindAll("svg"));
    }
}
