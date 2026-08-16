using System.Globalization;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor.Tests;

public sealed class DataGridTests
{
    [Fact]
    public void ClientOperationsSortStablyBeforePaging()
    {
        var items = new[]
        {
            new Row(1, "Third", 2),
            new Row(2, "First", 1),
            new Row(3, "Second", 1),
        };
        var firstPage = BzsDataGridOperations.Apply(
            items,
            (left, right) => left.Score.CompareTo(right.Score),
            BzsDataGridSortDirection.Ascending,
            page: 1,
            pageSize: 2);
        var secondPage = BzsDataGridOperations.Apply(
            items,
            (left, right) => left.Score.CompareTo(right.Score),
            BzsDataGridSortDirection.Ascending,
            page: 2,
            pageSize: 2);

        Assert.Equal([2, 3], firstPage.Select(static row => row.Id));
        Assert.Equal([1], secondPage.Select(static row => row.Id));
    }

    [Fact]
    public void GridRendersSemanticFieldAndTemplateColumns()
    {
        using var context = CreateContext();
        var cut = RenderGrid(context, [new(1, "Alpha", 42)]);

        var table = cut.Find("table");
        Assert.Equal("Data grid", table.GetAttribute("aria-label"));
        Assert.True(cut.Instance.ShowPageSizeSelector);
        Assert.True(cut.Instance.ShowPagination);
        Assert.Equal(new[] { "Name", "Score" }, cut.FindAll("th").Select(header => header.TextContent.Trim()));
        Assert.Equal("Alpha", cut.Find("tbody td:first-child").TextContent.Trim());
        Assert.Equal("42 points", cut.Find("tbody strong").TextContent.Trim());
        Assert.Equal("Rows per page", cut.Find("label > span").TextContent.Trim());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ClientFooterControlsFollowIndependentVisibilityParameters(
        bool showPageSizeSelector,
        bool showPagination)
    {
        using var context = CreateContext();
        var cut = RenderGrid(
            context,
            [new(1, "Alpha", 42)],
            parameters =>
            {
                parameters.Add(component => component.ShowPageSizeSelector, showPageSizeSelector);
                parameters.Add(component => component.ShowPagination, showPagination);
            });

        Assert.Equal(showPageSizeSelector, cut.FindAll("label > select").Count == 1);
        Assert.Equal(showPagination, cut.FindAll("nav[aria-label='Data pages']").Count == 1);
    }

    [Fact]
    public void NonSortableTemplatedHeaderExposesItsAccessibleNameOnTheHeaderCell()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, new[] { new Row(1, "Alpha", 42) })
            .Add(component => component.ChildContent, BuildTemplatedHeaderColumn()));

        var header = cut.Find("thead th");
        Assert.Equal("Status", header.GetAttribute("aria-label"));
        Assert.Empty(header.QuerySelectorAll("button"));
    }

    [Fact]
    public void RendererRowKeysHonorComparerDistinctness()
    {
        using var context = CreateContext();
        var firstKey = new RenderKey(1);
        var secondKey = new RenderKey(1);
        var cut = RenderGrid(
            context,
            [new(1, "One", 1), new(2, "Two", 2)],
            parameters =>
            {
                parameters.Add(component => component.ItemKey, row => row.Id == 1 ? firstKey : secondKey);
                parameters.Add(component => component.ItemKeyComparer, ReferenceEqualityComparer.Instance);
            });

        Assert.Equal(2, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public void ConditionalKeyedColumnsInsertInDeclarationOrder()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, new[] { new Row(1, "Alpha", 42) })
            .Add(component => component.ChildContent, BuildConditionalColumns(false, false)));

        Assert.Equal(new[] { "A", "B" }, HeaderTitles(cut));

        cut.Render(parameters => parameters.Add(
            component => component.ChildContent,
            BuildConditionalColumns(true, true)));

        Assert.Equal(new[] { "Leading", "A", "Middle", "B" }, HeaderTitles(cut));
    }

    [Fact]
    public void ConditionalKeyedColumnsRetainDeclarationOrderWhenRemoved()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, new[] { new Row(1, "Alpha", 42) })
            .Add(component => component.ChildContent, BuildConditionalColumns(true, true)));

        cut.Render(parameters => parameters.Add(
            component => component.ChildContent,
            BuildConditionalColumns(false, true)));
        Assert.Equal(new[] { "A", "Middle", "B" }, HeaderTitles(cut));

        cut.Render(parameters => parameters.Add(
            component => component.ChildContent,
            BuildConditionalColumns(false, false)));
        Assert.Equal(new[] { "A", "B" }, HeaderTitles(cut));
    }

    [Fact]
    public void SortCommandsRequestControlledCyclesWithoutMutatingTheSort()
    {
        using var context = CreateContext();
        var requests = new List<BzsDataGridSort?>();
        var items = new[] { new Row(1, "Zulu", 2), new Row(2, "Alpha", 1) };
        var cut = RenderGrid(
            context,
            items,
            parameters => parameters.Add(component => component.SortChanged, requests.Add));

        cut.Find("thead button").Click();

        var ascending = Assert.IsType<BzsDataGridSort>(Assert.Single(requests));
        Assert.Equal("name", ascending.ColumnKey);
        Assert.Equal(BzsDataGridSortDirection.Ascending, ascending.Direction);
        Assert.Null(cut.Instance.Sort);
        Assert.Equal(new[] { "Zulu", "Alpha" }, BodyNames(cut));

        cut.Render(parameters => parameters.Add(
            component => component.Sort,
            new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending)));
        Assert.Equal("ascending", cut.Find("th[aria-sort]").GetAttribute("aria-sort"));
        Assert.Equal(new[] { "Alpha", "Zulu" }, BodyNames(cut));

        cut.Find("thead button").Click();
        Assert.Equal(BzsDataGridSortDirection.Descending, requests[1]?.Direction);
        cut.Render(parameters => parameters.Add(
            component => component.Sort,
            new BzsDataGridSort("name", BzsDataGridSortDirection.Descending)));
        cut.Find("thead button").Click();
        Assert.Null(requests[2]);
    }

    [Fact]
    public void PagingAndPageSizeCommandsRemainControlled()
    {
        using var context = CreateContext();
        var pageRequests = new List<int>();
        var sizeRequests = new List<int>();
        var items = Enumerable.Range(1, 12).Select(index => new Row(index, $"Row {index}", index)).ToArray();
        var cut = RenderGrid(
            context,
            items,
            parameters =>
            {
                parameters.Add(component => component.Page, 2);
                parameters.Add(component => component.PageChanged, pageRequests.Add);
                parameters.Add(component => component.PageSizeChanged, sizeRequests.Add);
            });

        Assert.Equal(new[] { "Row 11", "Row 12" }, BodyNames(cut));
        cut.Find("button[data-bzs-pagination-command='previous']").Click();
        Assert.Equal([1], pageRequests);
        Assert.Equal(2, cut.Instance.Page);

        cut.Find("select").Change("25");
        Assert.Equal([25], sizeRequests);
        Assert.Equal([1, 1], pageRequests);
        Assert.Equal(10, cut.Instance.PageSize);
    }

    [Fact]
    public void AcceptedPageSizeChangeResetsPageBeforeApplyingTheNewSize()
    {
        using var context = CreateContext();
        var items = Enumerable.Range(1, 12).Select(index => new Row(index, $"Row {index}", index)).ToArray();
        var page = 2;
        var pageSize = 10;
        IRenderedComponent<BzsDataGrid<Row>>? cut = null;
        cut = RenderGrid(
            context,
            items,
            parameters =>
            {
                parameters.Add(component => component.Page, page);
                parameters.Add(component => component.PageSize, pageSize);
                parameters.Add(component => component.PageChanged, requested =>
                {
                    page = requested;
                    cut!.Render(updated => updated.Add(component => component.Page, requested));
                });
                parameters.Add(component => component.PageSizeChanged, requested =>
                {
                    pageSize = requested;
                    cut!.Render(updated => updated.Add(component => component.PageSize, requested));
                });
            });

        cut.Find("select").Change("25");

        Assert.Equal(1, page);
        Assert.Equal(25, pageSize);
        Assert.Equal(12, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public void MultipleSelectionUsesKeysAcrossEquivalentCollectionReplacement()
    {
        using var context = CreateContext();
        IReadOnlyList<Row>? requested = null;
        var original = new[] { new Row(1, "One", 1), new Row(2, "Two", 2) };
        var cut = RenderGrid(
            context,
            original,
            parameters =>
            {
                parameters.Add(component => component.SelectionMode, BzsDataGridSelectionMode.Multiple);
                parameters.Add(component => component.ItemKey, row => row.Id);
                parameters.Add(component => component.SelectedItems, new[] { original[1] });
                parameters.Add(component => component.SelectedItemsChanged, value => requested = value);
            });

        var replacement = new[] { new Row(1, "One replacement", 1), new Row(2, "Two replacement", 2) };
        cut.Render(parameters => parameters.Add(component => component.Items, replacement));

        var inputs = cut.FindAll("tbody input[type='checkbox']");
        Assert.False(inputs[0].HasAttribute("checked"));
        Assert.True(inputs[1].HasAttribute("checked"));
        inputs[0].Change(true);

        Assert.NotNull(requested);
        Assert.Equal([2, 1], requested.Select(static row => row.Id));
        Assert.Same(replacement[1], requested[0]);
        Assert.Same(replacement[0], requested[1]);
        Assert.False(inputs[0].HasAttribute("checked"));
    }

    [Fact]
    public void MultipleSelectionComparisonGrowthIsBoundedByItemsAndSelectedItems()
    {
        using var context = CreateContext();
        var comparer = new CountingKeyComparer();
        var items = Enumerable.Range(1, 100).Select(index => new Row(index, $"Row {index}", index)).ToArray();
        IReadOnlyList<Row>? requested = null;
        var cut = RenderGrid(
            context,
            items,
            parameters =>
            {
                parameters.Add(component => component.PageSize, 100);
                parameters.Add(component => component.PageSizeOptions, new[] { 100 });
                parameters.Add(component => component.SelectionMode, BzsDataGridSelectionMode.Multiple);
                parameters.Add(component => component.ItemKey, row => row.Id);
                parameters.Add(component => component.ItemKeyComparer, comparer);
                parameters.Add(component => component.SelectedItems, items.Take(50).ToArray());
                parameters.Add(component => component.SelectedItemsChanged, value => requested = value);
            });
        comparer.Reset();

        cut.FindAll("tbody input[type='checkbox']")[75].Change(true);

        Assert.NotNull(requested);
        Assert.Equal(51, requested.Count);
        Assert.True(
            comparer.EqualityCallCount <= 500,
            $"Expected at most 500 key comparisons, but observed {comparer.EqualityCallCount}.");
    }

    [Fact]
    public void SingleSelectionUsesKeysAndOnlyRequestsTheCurrentItem()
    {
        using var context = CreateContext();
        Row? requested = null;
        var selected = new Row(2, "Old two", 2);
        var replacement = new[] { new Row(1, "New one", 1), new Row(2, "New two", 2) };
        var cut = RenderGrid(
            context,
            replacement,
            parameters =>
            {
                parameters.Add(component => component.SelectionMode, BzsDataGridSelectionMode.Single);
                parameters.Add(component => component.ItemKey, row => row.Id);
                parameters.Add(component => component.SelectedItem, selected);
                parameters.Add(component => component.SelectedItemChanged, value => requested = value);
            });

        var inputs = cut.FindAll("tbody input[type='radio']");
        Assert.False(inputs[0].HasAttribute("checked"));
        Assert.True(inputs[1].HasAttribute("checked"));

        inputs[0].Change(true);

        Assert.Same(replacement[0], requested);
        Assert.True(inputs[1].HasAttribute("checked"));
    }

    [Fact]
    public void LoadingEmptyAndErrorStatesUseSemanticTableRowsAndTemplates()
    {
        using var context = CreateContext();
        var cut = RenderGrid(
            context,
            Array.Empty<Row>(),
            parameters => parameters.Add(component => component.EmptyTemplate, "Nothing here"));

        Assert.Equal("Nothing here", cut.Find("tbody td").TextContent.Trim());
        Assert.Equal("2", cut.Find("tbody td").GetAttribute("colspan"));

        cut.Render(parameters => parameters
            .Add(component => component.Items, new[] { new Row(1, "Alpha", 1) })
            .Add(component => component.Loading, true));
        Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy"));
        Assert.Contains("Loading data", cut.Find("[role='status']").TextContent, StringComparison.Ordinal);

        cut.Render(parameters => parameters
            .Add(component => component.Loading, false)
            .Add(component => component.Error, new InvalidOperationException("offline"))
            .Add(component => component.ErrorTemplate, error => builder => builder.AddContent(0, error.Message)));
        Assert.Equal("offline", cut.Find("tbody td").TextContent.Trim());
    }

    [Fact]
    public void InvalidPagingSelectionKeysAndColumnsFailFast()
    {
        using var context = CreateContext();

        Assert.Throws<InvalidOperationException>(() => RenderGrid(
            context,
            [new(1, "One", 1)],
            parameters => parameters.Add(component => component.SelectionMode, BzsDataGridSelectionMode.Single)));

        Assert.Throws<InvalidOperationException>(() => RenderGrid(
            context,
            [new(1, "One", 1), new(1, "Duplicate", 2)],
            parameters =>
            {
                parameters.Add(component => component.SelectionMode, BzsDataGridSelectionMode.Multiple);
                parameters.Add(component => component.ItemKey, row => row.Id);
            }));

        Assert.Throws<ArgumentOutOfRangeException>(() => RenderGrid(
            context,
            Array.Empty<Row>(),
            parameters => parameters.Add(component => component.Page, 2)));

        Assert.Throws<InvalidOperationException>(() => context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, new[] { new Row(1, "One", 1) })
            .Add(component => component.ChildContent, BuildDuplicateColumns())));
    }

    [Fact]
    public void DefaultGridTextUsesTheActiveUiCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-Hans");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
            using var context = CreateContext();
            var cut = RenderGrid(context, Array.Empty<Row>());

            Assert.Equal("数据表格", cut.Find("table").GetAttribute("aria-label"));
            Assert.Equal("暂无数据", cut.Find("tbody td").TextContent.Trim());
            Assert.Contains("每页行数", cut.Find("label").TextContent, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task StaticRenderingIncludesCaptionHeadersRowsAndPager()
    {
        using var context = CreateContext();
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsDataGrid<Row>.Items)] = new[] { new Row(1, "Alpha", 42) },
            [nameof(BzsDataGrid<Row>.Caption)] = "Projects",
            [nameof(BzsDataGrid<Row>.ChildContent)] = BuildColumns(),
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsDataGrid<Row>>(parameters);
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("Projects", document.QuerySelector("caption")?.TextContent.Trim());
        Assert.Equal(2, document.QuerySelectorAll("thead th").Length);
        Assert.Equal(2, document.QuerySelectorAll("tbody td").Length);
        Assert.Equal("Alpha", document.QuerySelector("tbody td")?.TextContent.Trim());
        Assert.NotNull(document.QuerySelector("nav[aria-label='Data pages']"));
    }

    [Fact]
    public async Task StaticRenderingOmitsFooterControlsWhenBothAreHidden()
    {
        using var context = CreateContext();
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsDataGrid<Row>.Items)] = new[] { new Row(1, "Alpha", 42) },
            [nameof(BzsDataGrid<Row>.ChildContent)] = BuildColumns(),
            [nameof(BzsDataGrid<Row>.ShowPageSizeSelector)] = false,
            [nameof(BzsDataGrid<Row>.ShowPagination)] = false,
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsDataGrid<Row>>(parameters);
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("Data grid", document.QuerySelector("table")?.GetAttribute("aria-label"));
        Assert.Equal("Alpha", document.QuerySelector("tbody td")?.TextContent.Trim());
        Assert.Empty(document.QuerySelectorAll("label, nav[aria-label='Data pages']"));
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        return context;
    }

    private static IRenderedComponent<BzsDataGrid<Row>> RenderGrid(
        BunitContext context,
        IReadOnlyList<Row> items,
        Action<ComponentParameterCollectionBuilder<BzsDataGrid<Row>>>? configure = null) =>
        context.Render<BzsDataGrid<Row>>(parameters =>
        {
            parameters.Add(component => component.Items, items);
            parameters.Add(component => component.ChildContent, BuildColumns());
            configure?.Invoke(parameters);
        });

    private static RenderFragment BuildColumns() => builder =>
    {
        builder.OpenComponent<BzsDataGridColumn<Row>>(0);
        builder.AddAttribute(1, nameof(BzsDataGridColumn<Row>.Key), "name");
        builder.AddAttribute(2, nameof(BzsDataGridColumn<Row>.Title), "Name");
        builder.AddAttribute(3, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Name));
        builder.AddAttribute(4, nameof(BzsDataGridColumn<Row>.Sortable), true);
        builder.CloseComponent();

        builder.OpenComponent<BzsDataGridColumn<Row>>(10);
        builder.AddAttribute(11, nameof(BzsDataGridColumn<Row>.Key), "score");
        builder.AddAttribute(12, nameof(BzsDataGridColumn<Row>.Title), "Score");
        builder.AddAttribute(13, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Score));
        builder.AddAttribute(
            14,
            nameof(BzsDataGridColumn<Row>.CellTemplate),
            (RenderFragment<Row>)(row => content =>
            {
                content.OpenElement(0, "strong");
                content.AddContent(1, $"{row.Score} points");
                content.CloseElement();
            }));
        builder.CloseComponent();
    };

    private static RenderFragment BuildDuplicateColumns() => builder =>
    {
        for (var index = 0; index < 2; index++)
        {
            builder.OpenComponent<BzsDataGridColumn<Row>>(index * 10);
            builder.AddAttribute((index * 10) + 1, nameof(BzsDataGridColumn<Row>.Key), "duplicate");
            builder.AddAttribute((index * 10) + 2, nameof(BzsDataGridColumn<Row>.Title), $"Column {index}");
            builder.AddAttribute(
                (index * 10) + 3,
                nameof(BzsDataGridColumn<Row>.ValueSelector),
                (Func<Row, object?>)(row => row.Name));
            builder.CloseComponent();
        }
    };

    private static RenderFragment BuildTemplatedHeaderColumn() => builder =>
    {
        builder.OpenComponent<BzsDataGridColumn<Row>>(0);
        builder.AddAttribute(1, nameof(BzsDataGridColumn<Row>.Key), "status");
        builder.AddAttribute(2, nameof(BzsDataGridColumn<Row>.AccessibleName), "Status");
        builder.AddAttribute(
            3,
            nameof(BzsDataGridColumn<Row>.HeaderTemplate),
            (RenderFragment)(content => content.AddMarkupContent(0, "<span aria-hidden=\"true\">*</span>")));
        builder.AddAttribute(4, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Name));
        builder.CloseComponent();
    };

    private static RenderFragment BuildConditionalColumns(bool includeLeading, bool includeMiddle) => builder =>
    {
        if (includeLeading)
        {
            AddColumn(builder, 0, "leading", "Leading");
        }

        AddColumn(builder, 10, "a", "A");

        if (includeMiddle)
        {
            AddColumn(builder, 20, "middle", "Middle");
        }

        AddColumn(builder, 30, "b", "B");
    };

    private static void AddColumn(RenderTreeBuilder builder, int sequence, string key, string title)
    {
        builder.OpenComponent<BzsDataGridColumn<Row>>(sequence);
        builder.SetKey(key);
        builder.AddAttribute(sequence + 1, nameof(BzsDataGridColumn<Row>.Key), key);
        builder.AddAttribute(sequence + 2, nameof(BzsDataGridColumn<Row>.Title), title);
        builder.AddAttribute(
            sequence + 3,
            nameof(BzsDataGridColumn<Row>.ValueSelector),
            (Func<Row, object?>)(row => row.Name));
        builder.CloseComponent();
    }

    private static string[] BodyNames(IRenderedComponent<BzsDataGrid<Row>> cut) =>
        cut.FindAll("tbody tr").Select(row => row.QuerySelector("td")?.TextContent.Trim() ?? string.Empty).ToArray();

    private static string[] HeaderTitles(IRenderedComponent<BzsDataGrid<Row>> cut) =>
        cut.FindAll("thead th").Select(header => header.TextContent.Trim()).ToArray();

    private sealed class CountingKeyComparer : IEqualityComparer<object?>
    {
        public int EqualityCallCount { get; private set; }

        public new bool Equals(object? left, object? right)
        {
            EqualityCallCount++;
            return EqualityComparer<object?>.Default.Equals(left, right);
        }

        public int GetHashCode(object? value) => value?.GetHashCode() ?? 0;

        public void Reset() => EqualityCallCount = 0;
    }

    private sealed record RenderKey(int Value);

    private sealed record Row(int Id, string Name, int Score);
}
