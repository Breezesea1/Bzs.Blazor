using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Bzs.Blazor.Tests;

public sealed class DataGridClientQueryTests
{
    [Fact]
    public void MultiSortAppliesPrecedenceOrderAndKeepsEqualItemsStable()
    {
        var items = new[]
        {
            new Row(1, "Ada", "Beta", 2),
            new Row(2, "Ada", "Alpha", 1),
            new Row(3, "Bo", "Alpha", 1),
            new Row(4, "Ada", "Alpha", 3),
        };

        var sorted = BzsDataGridOperations.Sort(
            items,
            [
                new BzsDataGridOperations.SortStep<Row>(
                    (left, right) => string.CompareOrdinal(left.Name, right.Name),
                    BzsDataGridSortDirection.Ascending),
                new BzsDataGridOperations.SortStep<Row>(
                    (left, right) => string.CompareOrdinal(left.Team, right.Team),
                    BzsDataGridSortDirection.Descending),
            ]);

        Assert.Equal([1, 2, 4, 3], sorted.Select(static row => row.Id));
    }

    [Theory]
    [InlineData(BzsDataGridTextOperator.Contains, "li", new[] { 1, 2 })]
    [InlineData(BzsDataGridTextOperator.NotContains, "li", new[] { 3 })]
    [InlineData(BzsDataGridTextOperator.StartsWith, "ali", new[] { 1 })]
    [InlineData(BzsDataGridTextOperator.EndsWith, "cia", new[] { 2 })]
    [InlineData(BzsDataGridTextOperator.Equals, "BO", new[] { 3 })]
    public void TextFiltersMatchCaseInsensitivelyByDefault(
        BzsDataGridTextOperator @operator,
        string value,
        int[] expected)
    {
        var items = new[]
        {
            new Row(1, "Alicja", "A", 1),
            new Row(2, "Felicia", "B", 2),
            new Row(3, "Bo", "C", 3),
        };
        var filter = new BzsDataGridTextFilter("name", value, @operator);

        var matches = items.Where(row => BzsDataGridOperations.Matches(filter, row.Name)).ToArray();

        Assert.Equal(expected, matches.Select(static row => row.Id));
    }

    [Fact]
    public void PresenceTextFiltersCarryNoValueAndMatchEmptiness()
    {
        var isEmpty = new BzsDataGridTextFilter("name", BzsDataGridTextOperator.IsEmpty);
        var isNotEmpty = new BzsDataGridTextFilter("name", BzsDataGridTextOperator.IsNotEmpty);

        Assert.Equal(string.Empty, isEmpty.Value);
        Assert.True(BzsDataGridOperations.Matches(isEmpty, string.Empty));
        Assert.False(BzsDataGridOperations.Matches(isEmpty, "Ada"));
        Assert.True(BzsDataGridOperations.Matches(isNotEmpty, "Ada"));
        Assert.False(BzsDataGridOperations.Matches(isNotEmpty, string.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BzsDataGridTextFilter("name", BzsDataGridTextOperator.Contains));
    }

    [Theory]
    [InlineData(BzsDataGridComparisonOperator.Equals, new[] { 2 })]
    [InlineData(BzsDataGridComparisonOperator.NotEquals, new[] { 1, 3 })]
    [InlineData(BzsDataGridComparisonOperator.LessThan, new[] { 1 })]
    [InlineData(BzsDataGridComparisonOperator.LessThanOrEqual, new[] { 1, 2 })]
    [InlineData(BzsDataGridComparisonOperator.GreaterThan, new[] { 3 })]
    [InlineData(BzsDataGridComparisonOperator.GreaterThanOrEqual, new[] { 2, 3 })]
    public void NumberFiltersCompareConvertedValues(
        BzsDataGridComparisonOperator @operator,
        int[] expected)
    {
        var items = new[] { new Row(1, "A", "A", 1), new Row(2, "B", "B", 2), new Row(3, "C", "C", 3) };
        var filter = new BzsDataGridNumberFilter("score", 2m, @operator);

        var matches = items.Where(row => BzsDataGridOperations.Matches(filter, row.Score)).ToArray();

        Assert.Equal(expected, matches.Select(static row => row.Id));
    }

    [Fact]
    public void DateFiltersAcceptDateOnlyAndDateTimeValues()
    {
        var filter = new BzsDataGridDateFilter("due", new DateOnly(2026, 8, 22));

        Assert.True(BzsDataGridOperations.Matches(filter, new DateOnly(2026, 8, 22)));
        Assert.True(BzsDataGridOperations.Matches(filter, new DateTime(2026, 8, 22, 13, 45, 0)));
        Assert.False(BzsDataGridOperations.Matches(filter, new DateOnly(2026, 8, 23)));
    }

    [Fact]
    public void ChoiceFiltersMatchAnySelectedValueAndRejectEmptySelections()
    {
        var filter = new BzsDataGridChoiceFilter("priority", ["High", "Normal", "High"]);

        Assert.Equal(["High", "Normal"], filter.Values);
        Assert.True(BzsDataGridOperations.Matches(filter, "Normal"));
        Assert.False(BzsDataGridOperations.Matches(filter, "Low"));
        Assert.Throws<ArgumentException>(() => new BzsDataGridChoiceFilter("priority", []));
    }

    [Fact]
    public void AggregatesIgnoreMissingValuesAndCountRows()
    {
        var items = new[] { new Row(1, "A", "A", 4), new Row(2, "B", "B", 6), new Row(3, "C", "C", 0) };
        Func<Row, decimal?> selector = row => row.Score == 0 ? null : row.Score;

        Assert.Equal(10m, BzsDataGridOperations.Aggregate(items, selector, BzsDataGridAggregate.Sum));
        Assert.Equal(5m, BzsDataGridOperations.Aggregate(items, selector, BzsDataGridAggregate.Average));
        Assert.Equal(4m, BzsDataGridOperations.Aggregate(items, selector, BzsDataGridAggregate.Minimum));
        Assert.Equal(6m, BzsDataGridOperations.Aggregate(items, selector, BzsDataGridAggregate.Maximum));
        Assert.Equal(2m, BzsDataGridOperations.Aggregate(items, selector, BzsDataGridAggregate.Count));
        Assert.Null(BzsDataGridOperations.Aggregate(items, selector, BzsDataGridAggregate.None));
    }

    [Fact]
    public void ClientFilteringNarrowsRowsAndRecomputesThePagerRange()
    {
        using var context = CreateContext();
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.ClientFiltering, true);
                parameters.Add(component => component.PageSize, 2);
                parameters.Add(component => component.PageSizeOptions, new[] { 2, 10 });
                parameters.Add(component => component.ShowResultSummary, true);
                parameters.Add(
                    component => component.Filters,
                    new BzsDataGridFilter[] { new BzsDataGridTextFilter("team", "alpha") });
            });

        Assert.Equal(["Ada", "Bo"], BodyNames(cut));
        Assert.Equal("Showing 1-2 of 2", cut.Find(".bzs-data-grid__result-summary").TextContent.Trim());
    }

    [Fact]
    public void ClientFilteringRequiresOptInWhenFiltersAreSupplied()
    {
        using var context = CreateContext();

        var exception = Assert.Throws<InvalidOperationException>(() => RenderGrid(
            context,
            AllRows,
            parameters => parameters.Add(
                component => component.Filters,
                new BzsDataGridFilter[] { new BzsDataGridTextFilter("team", "alpha") })));

        Assert.Contains("ClientFiltering", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalSearchMatchesAnyVisibleColumnAndRequestsControlledChanges()
    {
        using var context = CreateContext();
        string? requested = "unset";
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.ClientFiltering, true);
                parameters.Add(component => component.ShowSearch, true);
                parameters.Add(component => component.SearchText, "gamma");
                parameters.Add(component => component.SearchTextChanged, value => requested = value);
            });

        Assert.Equal(["Cyd"], BodyNames(cut));

        cut.Find("input[type='search']").Input("ada");
        Assert.Equal("ada", requested);
        Assert.Equal(["Cyd"], BodyNames(cut));

        cut.Find("button[aria-label='Clear search']").Click();
        Assert.Null(requested);
    }

    [Fact]
    public void MultiSortCommandsAccumulateAcrossColumnsAndStayControlled()
    {
        using var context = CreateContext();
        IReadOnlyList<BzsDataGridSort>? requested = null;
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.MultiSort, true);
                parameters.Add(
                    component => component.Sorts,
                    new[] { new BzsDataGridSort("team", BzsDataGridSortDirection.Ascending) });
                parameters.Add(component => component.SortsChanged, value => requested = value);
            });

        cut.Find("button[aria-label='Name column menu']").Click();
        cut.Find("button[aria-label='Name column menu']")
            .ParentElement!
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Trim() == "Add to sort")
            .Click();

        Assert.Equal(
            [("team", BzsDataGridSortDirection.Ascending), ("name", BzsDataGridSortDirection.Ascending)],
            requested!.Select(sort => (sort.ColumnKey, sort.Direction)));
        Assert.Single(cut.Instance.Sorts);
    }

    [Fact]
    public void MultiSortRendersPrecedenceBadgesAndAriaSortPerColumn()
    {
        using var context = CreateContext();
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.MultiSort, true);
                parameters.Add(
                    component => component.Sorts,
                    new[]
                    {
                        new BzsDataGridSort("team", BzsDataGridSortDirection.Descending),
                        new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending),
                    });
            });

        var headers = cut.FindAll("thead th[data-bzs-data-grid-column]");
        Assert.Equal("ascending", headers.Single(h => h.GetAttribute("data-bzs-data-grid-column") == "name").GetAttribute("aria-sort"));
        Assert.Equal("descending", headers.Single(h => h.GetAttribute("data-bzs-data-grid-column") == "team").GetAttribute("aria-sort"));
        Assert.Equal(
            ["2", "1"],
            cut.FindAll(".bzs-data-grid__sort-precedence").Select(badge => badge.TextContent.Trim()));
    }

    [Fact]
    public void SingleSortRejectsMoreThanOneControlledSort()
    {
        using var context = CreateContext();

        Assert.Throws<InvalidOperationException>(() => RenderGrid(
            context,
            AllRows,
            parameters => parameters.Add(
                component => component.Sorts,
                new[]
                {
                    new BzsDataGridSort("team", BzsDataGridSortDirection.Ascending),
                    new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending),
                })));
    }

    [Fact]
    public void HiddenColumnsLeaveTheTableAndTheChooserRequestsControlledVisibility()
    {
        using var context = CreateContext();
        IReadOnlyList<string>? requested = null;
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.ShowColumnChooser, true);
                parameters.Add(component => component.HiddenColumnKeys, new[] { "team" });
                parameters.Add(component => component.HiddenColumnKeysChanged, value => requested = value);
            });

        Assert.Equal(["Name", "Score"], HeaderTitles(cut));

        cut.Find("button[aria-label='Columns']").Click();
        cut.FindAll(".bzs-data-grid__chooser-option input")[1].Change(true);

        Assert.Empty(requested!);
        Assert.Equal(["Name", "Score"], HeaderTitles(cut));
    }

    [Fact]
    public void HiddenColumnsAreExcludedFromGlobalSearch()
    {
        using var context = CreateContext();
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.ClientFiltering, true);
                parameters.Add(component => component.SearchText, "gamma");
                parameters.Add(component => component.HiddenColumnKeys, new[] { "team" });
            });

        Assert.Empty(cut.FindAll("tbody tr td.bzs-data-grid__cell"));
    }

    [Fact]
    public void DetailRowsExpandThroughControlledKeysAndExposeExpansionState()
    {
        using var context = CreateContext();
        IReadOnlyList<object?>? requested = null;
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.ItemKey, row => row.Id);
                parameters.Add(
                    component => component.DetailTemplate,
                    (RenderFragment<Row>)(row => builder => builder.AddContent(0, $"Detail {row.Name}")));
                parameters.Add(component => component.ExpandedItemKeys, new object?[] { 1 });
                parameters.Add(component => component.ExpandedItemKeysChanged, value => requested = value);
            });

        Assert.Equal("Detail Ada", cut.Find(".bzs-data-grid__detail-panel").TextContent.Trim());
        var toggles = cut.FindAll(".bzs-data-grid__detail-toggle");
        Assert.Equal("true", toggles[0].GetAttribute("aria-expanded"));
        Assert.Equal("false", toggles[1].GetAttribute("aria-expanded"));

        toggles[1].Click();
        Assert.Equal([1, 2], requested!.Cast<int>());
    }

    [Fact]
    public void RowActivationReportsTheClickedItemOnlyWhenRowsAreClickable()
    {
        using var context = CreateContext();
        var activations = new List<int>();
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.RowClickable, true);
                parameters.Add(
                    component => component.RowClick,
                    (BzsDataGridRowEventArgs<Row> args) => activations.Add(args.Item.Id));
                parameters.Add(component => component.RowClass, row => $"row-{row.Id}");
            });

        cut.FindAll("tbody tr")[1].Click();
        cut.FindAll("tbody tr")[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal([2, 1], activations);
        Assert.Contains("row-1", cut.FindAll("tbody tr")[0].GetAttribute("class"));
    }

    [Fact]
    public void RowActivationStaysSilentWithoutOptIn()
    {
        using var context = CreateContext();
        var activations = 0;
        var cut = RenderGrid(
            context,
            AllRows,
            parameters => parameters.Add(
                component => component.RowClick,
                (BzsDataGridRowEventArgs<Row> args) => activations++));

        cut.FindAll("tbody tr")[0].Click();

        Assert.Equal(0, activations);
        Assert.Null(cut.FindAll("tbody tr")[0].GetAttribute("tabindex"));
    }

    [Fact]
    public void FooterAggregatesSummarizeEveryQueriedRowNotOnlyTheCurrentPage()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, AllRows)
            .Add(component => component.PageSize, 2)
            .Add(component => component.PageSizeOptions, new[] { 2, 10 })
            .Add(component => component.ChildContent, BuildAggregateColumns()));

        var footerCells = cut.FindAll("tfoot td");
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Equal("3", footerCells[0].TextContent.Trim());
        Assert.Equal("12", footerCells[1].TextContent.Trim());
    }

    [Fact]
    public void FooterAggregatesFollowClientFiltering()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, AllRows)
            .Add(component => component.ClientFiltering, true)
            .Add(component => component.Filters, new BzsDataGridFilter[] { new BzsDataGridTextFilter("name", "a", BzsDataGridTextOperator.StartsWith) })
            .Add(component => component.ChildContent, BuildAggregateColumns()));

        var footerCells = cut.FindAll("tfoot td");
        Assert.Equal("1", footerCells[0].TextContent.Trim());
        Assert.Equal("3", footerCells[1].TextContent.Trim());
    }

    [Fact]
    public void NumericAggregatesRequireAnAggregateValueSelector()
    {
        using var context = CreateContext();

        Assert.Throws<InvalidOperationException>(() => context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, AllRows)
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsDataGridColumn<Row>>(0);
                builder.AddAttribute(1, nameof(BzsDataGridColumn<Row>.Key), "score");
                builder.AddAttribute(2, nameof(BzsDataGridColumn<Row>.Title), "Score");
                builder.AddAttribute(3, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Score));
                builder.AddAttribute(4, nameof(BzsDataGridColumn<Row>.Aggregate), BzsDataGridAggregate.Sum);
                builder.CloseComponent();
            })));
    }

    [Fact]
    public void PresentationParametersProjectOntoRootDataAttributes()
    {
        using var context = CreateContext();
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.Density, BzsDataGridDensity.Comfortable);
                parameters.Add(component => component.Striped, true);
                parameters.Add(component => component.Bordered, true);
                parameters.Add(component => component.StickyHeader, true);
            });

        var root = cut.Find("[data-bzs-data-grid]");
        Assert.Equal("comfortable", root.GetAttribute("data-bzs-density"));
        Assert.Equal("true", root.GetAttribute("data-bzs-striped"));
        Assert.Equal("true", root.GetAttribute("data-bzs-bordered"));
        Assert.Equal("true", root.GetAttribute("data-bzs-sticky-header"));
        Assert.Contains("bzs-data-grid--comfortable", root.GetAttribute("class"));
    }

    [Fact]
    public void ColumnSizingAndAlignmentProjectOntoHeaderAndCellAttributes()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, AllRows)
            .Add(component => component.ResizableColumns, true)
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsDataGridColumn<Row>>(0);
                builder.AddAttribute(1, nameof(BzsDataGridColumn<Row>.Key), "score");
                builder.AddAttribute(2, nameof(BzsDataGridColumn<Row>.Title), "Score");
                builder.AddAttribute(3, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Score));
                builder.AddAttribute(4, nameof(BzsDataGridColumn<Row>.Align), BzsDataGridAlignment.End);
                builder.AddAttribute(5, nameof(BzsDataGridColumn<Row>.Width), " 8rem ");
                builder.AddAttribute(6, nameof(BzsDataGridColumn<Row>.MinWidth), "4rem");
                builder.AddAttribute(7, nameof(BzsDataGridColumn<Row>.Sticky), BzsDataGridColumnSticky.Start);
                builder.CloseComponent();
            }));

        var header = cut.Find("thead th[data-bzs-data-grid-column='score']");
        Assert.Equal("end", header.GetAttribute("data-bzs-align"));
        Assert.Equal("8rem", header.GetAttribute("data-bzs-width"));
        Assert.Equal("4rem", header.GetAttribute("data-bzs-min-width"));
        Assert.Equal("start", header.GetAttribute("data-bzs-sticky"));
        Assert.Equal("end", cut.Find("tbody td[data-bzs-data-grid-column='score']").GetAttribute("data-bzs-align"));
        Assert.Equal(
            "Resize Score column",
            cut.Find("[data-bzs-data-grid-resize='score']").GetAttribute("aria-label"));
        Assert.Single(cut.FindAll("colgroup col[data-bzs-data-grid-column='score']"));
    }

    [Fact]
    public async Task ReportColumnResizedRejectsInvalidBrowserValues()
    {
        using var context = CreateContext();
        BzsDataGridColumnResizeEventArgs? reported = null;
        var cut = RenderGrid(context, AllRows, parameters =>
        {
            parameters.Add(component => component.ResizableColumns, true);
            parameters.Add(
                component => component.ColumnResized,
                (BzsDataGridColumnResizeEventArgs args) => reported = args);
        });

        await cut.Instance.ReportColumnResizedAsync("  name  ", 180.5);
        Assert.Equal("name", reported!.ColumnKey);
        Assert.Equal(180.5, reported.Width);

        reported = null;
        await cut.Instance.ReportColumnResizedAsync("name", 0);
        await cut.Instance.ReportColumnResizedAsync("   ", 120);
        Assert.Null(reported);
    }

    [Fact]
    public void ProviderRequestsCarryPrecedenceOrderedSortsAndSearchText()
    {
        var sorts = new[]
        {
            new BzsDataGridSort("team", BzsDataGridSortDirection.Descending),
            new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending),
        };

        var request = new BzsDataGridRequest(2, 25, sorts[0], null, sorts, "  ada  ");

        Assert.Equal("ada", request.SearchText);
        Assert.Equal(sorts[0], request.Sort);
        Assert.Equal(["team", "name"], request.Sorts.Select(static sort => sort.ColumnKey));
        Assert.Null(new BzsDataGridRequest(1, 10, searchText: "   ").SearchText);
        Assert.Empty(new BzsDataGridRequest(1, 10).Sorts);
        Assert.Single(new BzsDataGridRequest(1, 10, sorts[0]).Sorts);
    }

    [Fact]
    public void ProviderRequestsRejectInconsistentAndDuplicatedSorts()
    {
        var ascending = new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending);
        var descending = new BzsDataGridSort("name", BzsDataGridSortDirection.Descending);

        Assert.Throws<ArgumentException>(() => new BzsDataGridRequest(
            1,
            10,
            ascending,
            null,
            [descending]));
        Assert.Throws<ArgumentException>(() => new BzsDataGridRequest(
            1,
            10,
            ascending,
            null,
            [ascending, descending]));
    }

    [Fact]
    public void ClientFilteringNarrowingBelowTheControlledPageClampsInsteadOfFailingFast()
    {
        using var context = CreateContext();
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.ClientFiltering, true);
                parameters.Add(component => component.Page, 2);
                parameters.Add(component => component.PageSize, 2);
                parameters.Add(component => component.PageSizeOptions, new[] { 2, 10 });
                parameters.Add(
                    component => component.Filters,
                    new BzsDataGridFilter[] { new BzsDataGridTextFilter("team", "gamma") });
            });

        Assert.Equal(["Cyd"], BodyNames(cut));
        Assert.Equal(2, cut.Instance.Page);
    }

    [Fact]
    public void HeaderSortCyclesOneMultiSortColumnWithoutLosingPrecedence()
    {
        using var context = CreateContext();
        IReadOnlyList<BzsDataGridSort>? requested = null;
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.MultiSort, true);
                parameters.Add(
                    component => component.Sorts,
                    new[]
                    {
                        new BzsDataGridSort("team", BzsDataGridSortDirection.Ascending),
                        new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending),
                    });
                parameters.Add(component => component.SortsChanged, value => requested = value);
            });

        cut.Find("thead th[data-bzs-data-grid-column='team'] button.bzs-data-grid__sort").Click();

        Assert.Equal(
            [("team", BzsDataGridSortDirection.Descending), ("name", BzsDataGridSortDirection.Ascending)],
            requested!.Select(sort => (sort.ColumnKey, sort.Direction)));
    }

    [Fact]
    public void ClearingOneMultiSortColumnKeepsTheRemainingPrecedence()
    {
        using var context = CreateContext();
        IReadOnlyList<BzsDataGridSort>? requestedSorts = null;
        BzsDataGridSort? requestedSort = null;
        var cut = RenderGrid(
            context,
            AllRows,
            parameters =>
            {
                parameters.Add(component => component.MultiSort, true);
                parameters.Add(
                    component => component.Sorts,
                    new[]
                    {
                        new BzsDataGridSort("team", BzsDataGridSortDirection.Descending),
                        new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending),
                    });
                parameters.Add(component => component.SortsChanged, value => requestedSorts = value);
                parameters.Add(component => component.SortChanged, value => requestedSort = value);
            });
        cut.Find("thead th[data-bzs-data-grid-column='name'] button.bzs-data-grid__sort").Click();

        Assert.Equal(
            [("team", BzsDataGridSortDirection.Descending), ("name", BzsDataGridSortDirection.Descending)],
            requestedSorts!.Select(sort => (sort.ColumnKey, sort.Direction)));
        Assert.Equal("team", requestedSort!.ColumnKey);

        cut.Find("thead th[data-bzs-data-grid-column='team'] button.bzs-data-grid__sort").Click();
        Assert.Equal(
            [("name", BzsDataGridSortDirection.Ascending)],
            requestedSorts!.Select(sort => (sort.ColumnKey, sort.Direction)));
        Assert.Equal("name", requestedSort!.ColumnKey);
    }

    [Fact]
    public void CountAggregatesIgnoreTheColumnValueFormat()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, AllRows)
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsDataGridColumn<Row>>(0);
                builder.AddAttribute(1, nameof(BzsDataGridColumn<Row>.Key), "name");
                builder.AddAttribute(2, nameof(BzsDataGridColumn<Row>.Title), "Name");
                builder.AddAttribute(3, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Name));
                builder.AddAttribute(4, nameof(BzsDataGridColumn<Row>.Format), "yyyy-MM-dd");
                builder.AddAttribute(5, nameof(BzsDataGridColumn<Row>.Aggregate), BzsDataGridAggregate.Count);
                builder.CloseComponent();
            }));

        Assert.Equal("3", cut.Find("tfoot td").TextContent.Trim());
    }

    [Fact]
    public void FooterTemplatesReceiveEveryQueriedRow()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, AllRows)
            .Add(component => component.PageSize, 1)
            .Add(component => component.PageSizeOptions, new[] { 1, 10 })
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsDataGridColumn<Row>>(0);
                builder.AddAttribute(1, nameof(BzsDataGridColumn<Row>.Key), "name");
                builder.AddAttribute(2, nameof(BzsDataGridColumn<Row>.Title), "Name");
                builder.AddAttribute(3, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Name));
                builder.AddAttribute(
                    4,
                    nameof(BzsDataGridColumn<Row>.FooterTemplate),
                    (RenderFragment<BzsDataGridFooterContext<Row>>)(footer => content =>
                        content.AddContent(0, string.Join('/', footer.Items.Select(row => row.Name)))));
                builder.CloseComponent();
            }));

        Assert.Equal("Ada/Bo/Cyd", cut.Find("tfoot td").TextContent.Trim());
    }

    [Fact]
    public void SearchInputChangeRoundTripsThroughTheControlledParameter()
    {
        using var context = CreateContext();
        string? searchText = null;
        var cut = context.Render<BzsDataGrid<Row>>(parameters =>
        {
            parameters.Add(component => component.Items, AllRows);
            parameters.Add(component => component.ChildContent, BuildColumns());
            parameters.Add(component => component.ClientFiltering, true);
            parameters.Add(component => component.ShowSearch, true);
            parameters.Add(component => component.ShowResultSummary, true);
            parameters.Add(component => component.SearchText, searchText);
            parameters.Add(component => component.SearchTextChanged, (string? value) => searchText = value);
        });

        cut.Find("input[type='search']").Input("gamma");

        Assert.Equal("gamma", searchText);

        // Re-render with the accepted value the way a controlled parent would.
        cut.Render(parameters =>
        {
            parameters.Add(component => component.Items, AllRows);
            parameters.Add(component => component.ChildContent, BuildColumns());
            parameters.Add(component => component.ClientFiltering, true);
            parameters.Add(component => component.ShowSearch, true);
            parameters.Add(component => component.ShowResultSummary, true);
            parameters.Add(component => component.SearchText, searchText);
            parameters.Add(component => component.SearchTextChanged, (string? value) => searchText = value);
        });

        Assert.Equal(["Cyd"], BodyNames(cut));
        Assert.Single(cut.FindAll("button[aria-label='Clear search']"));
    }

    private static readonly Row[] AllRows =
    [
        new(1, "Ada", "Alpha", 3),
        new(2, "Bo", "Alpha", 4),
        new(3, "Cyd", "Gamma", 5),
    ];

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        context.Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        context.JSInterop.Mode = JSRuntimeMode.Loose;
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
        AddColumn(builder, 0, "name", "Name", row => row.Name, sortable: true, filterKind: BzsDataGridFilterKind.Text);
        AddColumn(builder, 10, "team", "Team", row => row.Team, sortable: true, filterKind: BzsDataGridFilterKind.Text);
        AddColumn(builder, 20, "score", "Score", row => row.Score);
    };

    private static RenderFragment BuildAggregateColumns() => builder =>
    {
        builder.OpenComponent<BzsDataGridColumn<Row>>(0);
        builder.AddAttribute(1, nameof(BzsDataGridColumn<Row>.Key), "name");
        builder.AddAttribute(2, nameof(BzsDataGridColumn<Row>.Title), "Name");
        builder.AddAttribute(3, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Name));
        builder.AddAttribute(4, nameof(BzsDataGridColumn<Row>.Aggregate), BzsDataGridAggregate.Count);
        builder.CloseComponent();

        builder.OpenComponent<BzsDataGridColumn<Row>>(10);
        builder.AddAttribute(11, nameof(BzsDataGridColumn<Row>.Key), "score");
        builder.AddAttribute(12, nameof(BzsDataGridColumn<Row>.Title), "Score");
        builder.AddAttribute(13, nameof(BzsDataGridColumn<Row>.ValueSelector), (Func<Row, object?>)(row => row.Score));
        builder.AddAttribute(14, nameof(BzsDataGridColumn<Row>.Aggregate), BzsDataGridAggregate.Sum);
        builder.AddAttribute(
            15,
            nameof(BzsDataGridColumn<Row>.AggregateValueSelector),
            (Func<Row, decimal?>)(row => row.Score));
        builder.CloseComponent();
    };

    private static void AddColumn(
        RenderTreeBuilder builder,
        int sequence,
        string key,
        string title,
        Func<Row, object?> valueSelector,
        bool sortable = false,
        BzsDataGridFilterKind filterKind = BzsDataGridFilterKind.None)
    {
        builder.OpenComponent<BzsDataGridColumn<Row>>(sequence);
        builder.SetKey(key);
        builder.AddAttribute(sequence + 1, nameof(BzsDataGridColumn<Row>.Key), key);
        builder.AddAttribute(sequence + 2, nameof(BzsDataGridColumn<Row>.Title), title);
        builder.AddAttribute(sequence + 3, nameof(BzsDataGridColumn<Row>.ValueSelector), valueSelector);
        builder.AddAttribute(sequence + 4, nameof(BzsDataGridColumn<Row>.Sortable), sortable);
        builder.AddAttribute(sequence + 5, nameof(BzsDataGridColumn<Row>.FilterKind), filterKind);
        builder.CloseComponent();
    }

    private static string[] BodyNames(IRenderedComponent<BzsDataGrid<Row>> cut) =>
        cut.FindAll("tbody tr")
            .Select(row => row.QuerySelector("td")?.TextContent.Trim() ?? string.Empty)
            .ToArray();

    private static string[] HeaderTitles(IRenderedComponent<BzsDataGrid<Row>> cut) =>
        cut.FindAll("thead th[data-bzs-data-grid-column]").Select(header => header.TextContent.Trim()).ToArray();

    private sealed record Row(int Id, string Name, string Team, int Score);
}
