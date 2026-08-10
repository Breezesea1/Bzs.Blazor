using AngleSharp.Html.Parser;
using Bunit;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor.Tests;

public sealed class NavigationTests
{
    [Fact]
    public void RouterAndControlledActiveStatesExposeCurrentPageSemantics()
    {
        using var context = new BunitContext();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("projects/42");

        var cut = context.Render<BzsNavMenu>(parameters => parameters
            .Add(component => component.AccessibleName, "Primary")
            .Add(component => component.ChildContent, builder =>
            {
                AddNavItem(builder, 0, "Projects", "projects", match: NavLinkMatch.Prefix);
                AddNavItem(builder, 10, "Suppressed", "projects/42", active: false);
                AddNavItem(builder, 20, "Forced", "settings", active: true);
            }));

        Assert.Equal("page", FindNavItem(cut, "Projects").GetAttribute("aria-current"));
        Assert.Null(FindNavItem(cut, "Suppressed").GetAttribute("aria-current"));
        Assert.Equal("page", FindNavItem(cut, "Forced").GetAttribute("aria-current"));
    }

    [Fact]
    public void NavigationRendersSemanticListsIconsTemplatesAndLinkAttributes()
    {
        using var context = new BunitContext();
        var linkAttributes = new Dictionary<string, object>
        {
            ["target"] = "_blank",
            ["rel"] = "noreferrer",
            ["data-test-link"] = "reports",
        };

        var cut = context.Render<BzsNavMenu>(parameters => parameters
            .Add(component => component.AccessibleName, "Workspace")
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsNavItem>(0);
                builder.AddAttribute(1, nameof(BzsNavItem.LabelContent), (RenderFragment)(content =>
                {
                    content.OpenElement(0, "strong");
                    content.AddContent(1, "Reports");
                    content.CloseElement();
                }));
                builder.AddAttribute(2, nameof(BzsNavItem.Icon), BzsIcons.Package);
                builder.AddAttribute(3, nameof(BzsNavItem.Href), "reports");
                builder.AddAttribute(4, nameof(BzsNavItem.LinkAttributes), linkAttributes);
                builder.CloseComponent();
            }));

        var navigation = cut.Find("nav[aria-label='Workspace']");
        Assert.Equal("UL", navigation.Children.Single().TagName);
        Assert.Single(cut.FindAll("li"));
        var link = cut.Find("a[data-test-link='reports']");
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noreferrer", link.GetAttribute("rel"));
        Assert.Equal("Reports", link.QuerySelector("strong")?.TextContent);
        Assert.NotNull(link.QuerySelector("svg[aria-hidden='true']"));
    }

    [Fact]
    public void DisabledLinksAreNotNavigableOrKeyboardReachable()
    {
        using var context = new BunitContext();
        var cut = context.Render<BzsNavMenu>(parameters => parameters
            .Add(component => component.ChildContent, builder =>
                AddNavItem(builder, 0, "Archived", "archive", disabled: true)));

        Assert.Empty(cut.FindAll("a"));
        var disabled = cut.Find("[aria-disabled='true']");
        Assert.Equal("SPAN", disabled.TagName);
        Assert.Equal("-1", disabled.GetAttribute("tabindex"));
        Assert.Equal("Archived", disabled.TextContent.Trim());
    }

    [Fact]
    public void DisclosureRequestsControlledChangesAndRetainsNestedSemantics()
    {
        using var context = new BunitContext();
        var requestedStates = new List<bool>();
        var cut = context.Render<BzsNavMenu>(parameters => parameters
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsNavItem>(0);
                builder.AddAttribute(1, nameof(BzsNavItem.Label), "Administration");
                builder.AddAttribute(2, nameof(BzsNavItem.Open), false);
                builder.AddAttribute(3, nameof(BzsNavItem.OpenChanged),
                    EventCallback.Factory.Create<bool>(this, value => requestedStates.Add(value)));
                builder.AddAttribute(4, nameof(BzsNavItem.ChildContent), (RenderFragment)(children =>
                    AddNavItem(children, 0, "Users", "users")));
                builder.CloseComponent();
            }));

        var summary = cut.Find("summary");
        var controlledId = summary.GetAttribute("aria-controls");
        Assert.False(cut.Find("details").HasAttribute("open"));
        Assert.NotNull(controlledId);
        Assert.NotNull(cut.Find($"#{controlledId}"));
        Assert.Equal("Users", cut.Find("ul ul a").TextContent.Trim());

        summary.Click();

        Assert.Equal([true], requestedStates);
        Assert.False(cut.Find("details").HasAttribute("open"));
    }

    [Fact]
    public void EscapeRequestsClosureAndDisabledDisclosureDoesNotRequestChanges()
    {
        using var context = new BunitContext();
        var requestedStates = new List<bool>();
        var cut = context.Render<BzsNavMenu>(parameters => parameters
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsNavItem>(0);
                builder.AddAttribute(1, nameof(BzsNavItem.Label), "Open group");
                builder.AddAttribute(2, nameof(BzsNavItem.Open), true);
                builder.AddAttribute(3, nameof(BzsNavItem.OpenChanged),
                    EventCallback.Factory.Create<bool>(this, value => requestedStates.Add(value)));
                builder.AddAttribute(4, nameof(BzsNavItem.ChildContent), (RenderFragment)(children =>
                    AddNavItem(children, 0, "Child", "child")));
                builder.CloseComponent();

                builder.OpenComponent<BzsNavItem>(10);
                builder.AddAttribute(11, nameof(BzsNavItem.Label), "Disabled group");
                builder.AddAttribute(12, nameof(BzsNavItem.Disabled), true);
                builder.AddAttribute(13, nameof(BzsNavItem.OpenChanged),
                    EventCallback.Factory.Create<bool>(this, value => requestedStates.Add(value)));
                builder.AddAttribute(14, nameof(BzsNavItem.ChildContent), (RenderFragment)(children =>
                    AddNavItem(children, 0, "Unavailable", "unavailable")));
                builder.CloseComponent();
            }));

        cut.Find("a[href='child']").KeyDown("Escape");
        cut.FindAll("summary")[1].Click();

        Assert.Equal([false], requestedStates);
        Assert.True(cut.FindAll("details")[1].HasAttribute("inert"));
        Assert.Equal("-1", cut.FindAll("summary")[1].GetAttribute("tabindex"));
    }

    [Fact]
    public void BreadcrumbsDefaultAndOverrideCurrentPageAndSupportTemplates()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        IReadOnlyList<BzsBreadcrumbItem> items =
        [
            new("Home", "/"),
            new("Projects", "/projects", current: true),
            new("Quarterly report", "/projects/report"),
        ];

        var cut = context.Render<BzsBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.AccessibleName, "Breadcrumb trail")
            .Add(component => component.ItemTemplate, item => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-label", item.Label);
                builder.AddContent(2, item.Label);
                builder.CloseElement();
            })
            .Add(component => component.SeparatorContent, "next"));

        Assert.Equal(3, cut.FindAll("ol > li").Count);
        Assert.Equal("Projects", cut.Find("[aria-current='page']").TextContent.Trim());
        Assert.Equal(3, cut.FindAll("[data-label]").Count);
        Assert.All(cut.FindAll(".bzs-breadcrumbs__separator"), separator =>
            Assert.Equal("true", separator.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void BreadcrumbsMarkTheFinalItemCurrentByDefault()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var cut = context.Render<BzsBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, new BzsBreadcrumbItem[]
            {
                new("Home", "/"),
                new("Settings"),
            }));

        var navigation = cut.Find("nav[aria-label='Breadcrumb']");
        Assert.Equal("OL", navigation.Children.Single().TagName);
        var current = cut.Find("[aria-current='page']");
        Assert.Equal("SPAN", current.TagName);
        Assert.Equal("Settings", current.TextContent.Trim());
    }

    [Fact]
    public void BreadcrumbsUseTheCurrentUICultureForTheirDefaultAccessibleName()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-Hans");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
            using var context = new BunitContext();
            context.Services.AddBzsBlazor();

            var cut = context.Render<BzsBreadcrumbs>(parameters => parameters
                .Add(component => component.Items, new BzsBreadcrumbItem[]
                {
                    new("首页", "/"),
                    new("设置"),
                }));

            Assert.Equal("面包屑导航", cut.Find("nav").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task NavigationAndBreadcrumbsRenderMeaningfulStaticHtml()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsNavMenu.AccessibleName)] = "Static navigation",
            [nameof(BzsNavMenu.ChildContent)] = (RenderFragment)(builder =>
            {
                AddNavItem(builder, 0, "Documentation", "docs");
                builder.OpenComponent<BzsNavItem>(10);
                builder.AddAttribute(11, nameof(BzsNavItem.Label), "Resources");
                builder.AddAttribute(12, nameof(BzsNavItem.ChildContent), (RenderFragment)(children =>
                    AddNavItem(children, 0, "Examples", "examples")));
                builder.CloseComponent();
            }),
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsNavMenu>(parameters);
            return output.ToHtmlString();
        });
        var breadcrumbHtml = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsBreadcrumbs>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(BzsBreadcrumbs.Items)] = new BzsBreadcrumbItem[]
                    {
                        new("Home", "/"),
                        new("Static page"),
                    },
                }));
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var breadcrumbDocument = await new HtmlParser().ParseDocumentAsync(breadcrumbHtml);

        Assert.Equal("Static navigation", document.QuerySelector("nav")?.GetAttribute("aria-label"));
        Assert.Equal("docs", document.QuerySelector("nav ul li a")?.GetAttribute("href"));
        Assert.Equal("Resources", document.QuerySelector("details summary")?.TextContent.Trim());
        Assert.Equal("examples", document.QuerySelector("details ul a")?.GetAttribute("href"));
        Assert.Contains("Documentation", document.Body?.TextContent, StringComparison.Ordinal);
        Assert.Equal("Breadcrumb", breadcrumbDocument.QuerySelector("nav")?.GetAttribute("aria-label"));
        Assert.Equal("Static page", breadcrumbDocument.QuerySelector("[aria-current='page']")?.TextContent.Trim());
    }

    private static AngleSharp.Dom.IElement FindNavItem(IRenderedComponent<BzsNavMenu> cut, string label) =>
        Assert.Single(cut.FindAll("a"), item =>
            string.Equals(item.TextContent.Trim(), label, StringComparison.Ordinal));

    private static void AddNavItem(
        RenderTreeBuilder builder,
        int sequence,
        string label,
        string href,
        bool disabled = false,
        bool? active = null,
        NavLinkMatch match = NavLinkMatch.Prefix)
    {
        builder.OpenComponent<BzsNavItem>(sequence);
        builder.AddAttribute(sequence + 1, nameof(BzsNavItem.Label), label);
        builder.AddAttribute(sequence + 2, nameof(BzsNavItem.Href), href);
        builder.AddAttribute(sequence + 3, nameof(BzsNavItem.Disabled), disabled);
        builder.AddAttribute(sequence + 4, nameof(BzsNavItem.Match), match);
        if (active.HasValue)
        {
            builder.AddAttribute(sequence + 5, nameof(BzsNavItem.Active), active);
        }
        builder.CloseComponent();
    }
}
