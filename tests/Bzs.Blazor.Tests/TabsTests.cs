using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor.Tests;

public sealed class TabsTests
{
    [Fact]
    public void ControlledTabsRequestSelectionWithoutMutatingTheActiveValue()
    {
        using var context = CreateContext();
        var requestedValues = new List<string>();

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ActiveValue, "details")
            .Add(component => component.ActiveValueChanged, (string value) => requestedValues.Add(value))
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("overview", "Overview", "Overview panel"),
                new TabDefinition("details", "Details", "Details panel"),
                new TabDefinition("settings", "Settings", "Settings panel"))));

        FindTab(cut, "Settings").Click();

        Assert.Equal(["settings"], requestedValues);
        Assert.Equal("details", cut.Instance.ActiveValue);
        Assert.Equal("true", FindTab(cut, "Details").GetAttribute("aria-selected"));
        Assert.Equal("false", FindTab(cut, "Settings").GetAttribute("aria-selected"));
    }

    [Fact]
    public void UncontrolledTabsUseTheInitialValueAndKeepSelectionInternally()
    {
        using var context = CreateContext();
        var changedValues = new List<string>();

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.InitialActiveValue, "settings")
            .Add(component => component.ActiveValueChanged, (string value) => changedValues.Add(value))
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("overview", "Overview", "Overview panel"),
                new TabDefinition("settings", "Settings", "Settings panel"))));

        Assert.Equal("true", FindTab(cut, "Settings").GetAttribute("aria-selected"));

        FindTab(cut, "Overview").Click();

        Assert.Equal(["overview"], changedValues);
        Assert.Equal("true", FindTab(cut, "Overview").GetAttribute("aria-selected"));
        Assert.Equal("Overview panel", cut.Find("[role=tabpanel]:not([hidden])").TextContent.Trim());
    }

    [Fact]
    public void AutomaticArrowNavigationSkipsDisabledTabs()
    {
        using var context = CreateContext();
        var changedValues = new List<string>();

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ActiveValueChanged, (string value) => changedValues.Add(value))
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("first", "First", "First panel"),
                new TabDefinition("second", "Second", "Second panel", Disabled: true),
                new TabDefinition("third", "Third", "Third panel"))));

        FindTab(cut, "First").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", Code = "ArrowRight" });

        Assert.Equal(["third"], changedValues);
        Assert.Equal("true", FindTab(cut, "Third").GetAttribute("aria-selected"));
        Assert.True(FindTab(cut, "Second").HasAttribute("disabled"));
    }

    [Fact]
    public void HomeAndEndMoveAcrossEnabledTabs()
    {
        using var context = CreateContext();
        var changedValues = new List<string>();

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ActiveValueChanged, (string value) => changedValues.Add(value))
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("first", "First", "First panel"),
                new TabDefinition("second", "Second", "Second panel", Disabled: true),
                new TabDefinition("third", "Third", "Third panel"))));

        FindTab(cut, "First").KeyDown(new KeyboardEventArgs { Key = "End" });
        FindTab(cut, "Third").KeyDown(new KeyboardEventArgs { Key = "Home" });

        Assert.Equal(["third", "first"], changedValues);
        Assert.Equal("true", FindTab(cut, "First").GetAttribute("aria-selected"));
    }

    [Fact]
    public void TabsExposeTheRequiredRolesAndRelationships()
    {
        using var context = CreateContext();

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("overview", "Overview", "Overview panel"),
                new TabDefinition("settings", "Settings", "Settings panel"))));

        var tabList = cut.Find("[role=tablist]");
        Assert.Equal("horizontal", tabList.GetAttribute("aria-orientation"));

        var tabs = cut.FindAll("[role=tab]");
        Assert.Equal(2, tabs.Count);
        foreach (var tab in tabs)
        {
            var panelId = tab.GetAttribute("aria-controls");
            Assert.NotNull(panelId);
            var panel = cut.Find($"#{panelId}");

            Assert.Equal("tabpanel", panel.GetAttribute("role"));
            Assert.Equal(tab.GetAttribute("id"), panel.GetAttribute("aria-labelledby"));
        }

        Assert.DoesNotContain(cut.FindAll("[role=tabpanel][hidden]"), panel =>
            panel.GetAttribute("aria-labelledby") == FindTab(cut, "Overview").GetAttribute("id"));
        Assert.Single(cut.FindAll("[role=tabpanel][hidden]"));
    }

    [Fact]
    public void TabsPreserveAdditionalAttributesAndExposeDirectionHooks()
    {
        using var context = CreateContext();
        var attributes = new Dictionary<string, object>
        {
            ["aria-label"] = "Project sections",
            ["data-test-id"] = "project-tabs",
            ["dir"] = "rtl",
        };

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.AdditionalAttributes, attributes)
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("overview", "Overview", "Overview panel"))));

        var root = cut.Find("[data-test-id=project-tabs]");
        Assert.Null(root.GetAttribute("aria-label"));
        Assert.Equal("rtl", root.GetAttribute("dir"));
        Assert.Equal("rtl", root.GetAttribute("data-bzs-tabs-direction"));
        Assert.Equal("Project sections", cut.Find("[role=tablist]").GetAttribute("aria-label"));
    }

    [Fact]
    public void HorizontalArrowNavigationFollowsAnExplicitRtlDirection()
    {
        using var context = CreateContext();
        var changedValues = new List<string>();

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["dir"] = "rtl" })
            .Add(component => component.ActiveValueChanged, (string value) => changedValues.Add(value))
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("first", "First", "First panel"),
                new TabDefinition("second", "Second", "Second panel"),
                new TabDefinition("third", "Third", "Third panel"))));

        FindTab(cut, "First").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", Code = "ArrowRight" });

        Assert.Equal(["third"], changedValues);
        Assert.Equal("true", FindTab(cut, "Third").GetAttribute("aria-selected"));
    }

    [Fact]
    public void TabsRejectEmptyAndDuplicateValues()
    {
        using var emptyContext = CreateContext();
        Assert.Throws<ArgumentException>(() => emptyContext.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition(" ", "Empty", "Empty panel")))));

        using var duplicateContext = CreateContext();
        var exception = Assert.Throws<InvalidOperationException>(() => duplicateContext.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("duplicate", "First", "First panel"),
                new TabDefinition("duplicate", "Second", "Second panel")))));

        Assert.Contains("unique", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ControlledTabsAllowAnExplicitNullSelection()
    {
        using var context = CreateContext();

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ActiveValue, (string?)null)
            .Add(component => component.ChildContent, CreateItems(
                new TabDefinition("overview", "Overview", "Overview panel"),
                new TabDefinition("settings", "Settings", "Settings panel"))));

        Assert.All(cut.FindAll("[role=tab]"), tab =>
            Assert.Equal("false", tab.GetAttribute("aria-selected")));
        Assert.Empty(cut.FindAll("[role=tabpanel]:not([hidden])"));
    }

    [Fact]
    public void TabAndPanelIdsRemainUniqueAcrossSuffixCollisions()
    {
        using var context = CreateContext();

        var cut = context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsTabItem>(0);
                builder.AddAttribute(1, nameof(BzsTabItem.Id), "foo");
                builder.AddAttribute(2, nameof(BzsTabItem.Value), "one");
                builder.AddAttribute(3, nameof(BzsTabItem.Title), "One");
                builder.CloseComponent();
                builder.OpenComponent<BzsTabItem>(4);
                builder.AddAttribute(5, nameof(BzsTabItem.Id), "foo-panel");
                builder.AddAttribute(6, nameof(BzsTabItem.Value), "two");
                builder.AddAttribute(7, nameof(BzsTabItem.Title), "Two");
                builder.CloseComponent();
            }));

        var ids = cut.FindAll("[id]").Select(element => element.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TabIdsRejectWhitespaceTokens()
    {
        using var context = CreateContext();

        Assert.Throws<ArgumentException>(() => context.Render<BzsTabs>(parameters => parameters
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsTabItem>(0);
                builder.AddAttribute(1, nameof(BzsTabItem.Id), "invalid id");
                builder.AddAttribute(2, nameof(BzsTabItem.Value), "value");
                builder.AddAttribute(3, nameof(BzsTabItem.Title), "Title");
                builder.CloseComponent();
            })));
    }

    private static IElement FindTab(IRenderedComponent<BzsTabs> cut, string title) =>
        Assert.Single(cut.FindAll("[role=tab]"), tab =>
            string.Equals(tab.TextContent.Trim(), title, StringComparison.Ordinal));

    private static RenderFragment CreateItems(params TabDefinition[] tabs) => builder =>
    {
        var sequence = 0;
        foreach (var tab in tabs)
        {
            builder.OpenComponent<BzsTabItem>(sequence++);
            builder.AddAttribute(sequence++, nameof(BzsTabItem.Value), tab.Value);
            builder.AddAttribute(sequence++, nameof(BzsTabItem.Title), tab.Title);
            builder.AddAttribute(sequence++, nameof(BzsTabItem.Disabled), tab.Disabled);
            builder.AddAttribute(sequence++, nameof(BzsTabItem.ChildContent), (RenderFragment)(content =>
                content.AddContent(0, tab.Content)));
            builder.CloseComponent();
        }
    };

    private sealed record TabDefinition(string Value, string Title, string Content, bool Disabled = false);

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }
}
