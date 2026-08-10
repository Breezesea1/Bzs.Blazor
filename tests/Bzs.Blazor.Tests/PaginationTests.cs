using System.Globalization;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor.Tests;

public sealed class PaginationTests
{
    [Fact]
    public void RangeGenerationHandlesSmallLargeAndEmptyCounts()
    {
        Assert.Equal(
            new int?[] { 1, 2, 3, 4, 5 },
            BzsPaginationRange.Create(3, 5, siblingCount: 1, boundaryCount: 1));
        Assert.Equal(
            new int?[] { 1, null, 4, 5, 6, null, 20 },
            BzsPaginationRange.Create(5, 20, siblingCount: 1, boundaryCount: 1));
        Assert.Empty(BzsPaginationRange.Create(1, 0, siblingCount: 0, boundaryCount: 0));
    }

    [Fact]
    public void RangeGenerationFillsSinglePageGapsAndChangesWithPageCount()
    {
        Assert.Equal(
            new int?[] { 1, 2, 3, null, 10 },
            BzsPaginationRange.Create(3, 10, siblingCount: 0, boundaryCount: 1));
        Assert.Equal(
            new int?[] { 1, 2, 3, 4 },
            BzsPaginationRange.Create(2, 4, siblingCount: 1, boundaryCount: 1));
        Assert.Equal(
            new int?[] { 1, null, 6, null, 12 },
            BzsPaginationRange.Create(6, 12, siblingCount: 0, boundaryCount: 1));
    }

    [Fact]
    public void ControlledPageRequestsDoNotMutateTheSuppliedPage()
    {
        using var context = CreateContext();
        var requestedPages = new List<int>();
        var cut = context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.Page, 2)
            .Add(component => component.PageCount, 5)
            .Add(component => component.PageChanged, (int page) => requestedPages.Add(page)));

        cut.Find("button[data-bzs-pagination-page='4']").Click();

        Assert.Equal([4], requestedPages);
        Assert.Equal(2, cut.Instance.Page);
        Assert.Equal("page", cut.Find("button[data-bzs-pagination-page='2']").GetAttribute("aria-current"));
    }

    [Fact]
    public void NavigationCommandsRequestExpectedPagesAndRespectBoundaries()
    {
        using var context = CreateContext();
        var requestedPages = new List<int>();
        var cut = context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.Page, 3)
            .Add(component => component.PageCount, 5)
            .Add(component => component.PageChanged, (int page) => requestedPages.Add(page)));

        cut.Find("button[data-bzs-pagination-command='first']").Click();
        cut.Find("button[data-bzs-pagination-command='previous']").Click();
        cut.Find("button[data-bzs-pagination-command='next']").Click();
        cut.Find("button[data-bzs-pagination-command='last']").Click();

        Assert.Equal([1, 2, 4, 5], requestedPages);

        cut.Render(parameters => parameters.Add(component => component.Page, 1));
        Assert.True(cut.Find("button[data-bzs-pagination-command='first']").HasAttribute("disabled"));
        Assert.True(cut.Find("button[data-bzs-pagination-command='previous']").HasAttribute("disabled"));
    }

    [Fact]
    public void LabelsAndAccessibleNameAreLocalizedAndOverridable()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.Page, 2)
            .Add(component => component.PageCount, 4)
            .Add(component => component.AccessibleName, "Results pages")
            .Add(component => component.FirstPageLabel, "Start")
            .Add(component => component.PreviousPageLabel, "Back")
            .Add(component => component.NextPageLabel, "Forward")
            .Add(component => component.LastPageLabel, "End"));

        Assert.Equal("Results pages", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("Start", cut.Find("button[data-bzs-pagination-command='first']").GetAttribute("aria-label"));
        Assert.Equal("Back", cut.Find("button[data-bzs-pagination-command='previous']").GetAttribute("aria-label"));
        Assert.Equal("Forward", cut.Find("button[data-bzs-pagination-command='next']").GetAttribute("aria-label"));
        Assert.Equal("End", cut.Find("button[data-bzs-pagination-command='last']").GetAttribute("aria-label"));
    }

    [Fact]
    public void DisabledAndCompactModesExposeClearStaticState()
    {
        using var context = CreateContext();
        var callbackCount = 0;
        var cut = context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.Page, 2)
            .Add(component => component.PageCount, 5)
            .Add(component => component.Compact, true)
            .Add(component => component.Disabled, true)
            .Add(component => component.PageChanged, (int _) => callbackCount++));

        Assert.Equal("compact", cut.Find("nav").GetAttribute("data-bzs-pagination"));
        Assert.Equal("true", cut.Find("nav").GetAttribute("aria-disabled"));
        Assert.Single(cut.FindAll("[data-bzs-pagination-status]"));
        Assert.Empty(cut.FindAll("[data-bzs-pagination-page]"));
        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));

        cut.Find("button[data-bzs-pagination-command='next']").Click();
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void InvalidPageRangesAndCountsFailFast()
    {
        using var context = CreateContext();

        Assert.Throws<ArgumentOutOfRangeException>(() => context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.PageCount, -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.Page, 0)
            .Add(component => component.PageCount, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.Page, 3)
            .Add(component => component.PageCount, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.Page, 2)
            .Add(component => component.PageCount, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.SiblingCount, -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.Render<BzsPagination>(parameters => parameters
            .Add(component => component.BoundaryCount, -1)));
    }

    [Fact]
    public void PaginationLocalizesLibraryOwnedLabelsInZhHans()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-Hans");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
            using var context = CreateContext();
            var cut = context.Render<BzsPagination>(parameters => parameters
                .Add(component => component.Page, 2)
                .Add(component => component.PageCount, 4));

            Assert.Equal("分页导航", cut.Find("nav").GetAttribute("aria-label"));
            Assert.Equal("转到上一页", cut.Find("button[data-bzs-pagination-command='previous']").GetAttribute("aria-label"));
            Assert.Contains("第 2 页", cut.Find("button[data-bzs-pagination-page='2']").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task StaticRenderingExposesNamedNavigationAndDisabledCommands()
    {
        using var context = CreateContext();
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsPagination.Page)] = 1,
            [nameof(BzsPagination.PageCount)] = 3,
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsPagination>(parameters);
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var navigation = Assert.IsAssignableFrom<AngleSharp.Dom.IElement>(document.QuerySelector("nav"));

        Assert.Equal("Pagination", navigation.GetAttribute("aria-label"));
        Assert.True(navigation.QuerySelector("button[data-bzs-pagination-command='first']")?.HasAttribute("disabled"));
        Assert.Equal("page", navigation.QuerySelector("button[data-bzs-pagination-page='1']")?.GetAttribute("aria-current"));
        Assert.Equal(3, navigation.QuerySelectorAll("button[data-bzs-pagination-page]").Length);
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        return context;
    }
}
