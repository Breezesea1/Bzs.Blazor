using System.Collections.Concurrent;
using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor.Tests;

public sealed class DataGridServerTests
{
    [Fact]
    public void ProviderRequestLoadsAcceptedKnownTotalRows()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request =>
            new BzsDataGridResult<Row>([new(11, "Ada")], totalCount: 25));

        var cut = RenderProviderGrid(
            context,
            provider,
            page: 2,
            filters: [new BzsDataGridTextFilter("name", "Ad")]);

        cut.WaitForAssertion(() => Assert.Equal("Ada", cut.Find("tbody td").TextContent.Trim()));
        var request = Assert.Single(provider.Calls);
        Assert.Equal(2, request.Page);
        Assert.Equal(10, request.PageSize);
        Assert.IsType<BzsDataGridTextFilter>(Assert.Single(request.Filters));
        Assert.Equal("2", cut.Find("[data-bzs-pagination-page='2']").TextContent.Trim());
    }

    [Fact]
    public void StructurallyEqualControlledStateDoesNotReload()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request =>
            new BzsDataGridResult<Row>([new(1, "Ada")], false));
        var cut = RenderProviderGrid(
            context,
            provider,
            filters: [new BzsDataGridTextFilter("name", "Ada")]);
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));

        cut.Render(parameters => parameters.Add(
            component => component.Filters,
            new BzsDataGridFilter[] { new BzsDataGridTextFilter("name", "Ada") }));

        Assert.Single(provider.Calls);
    }

    [Fact]
    public void StaleProviderCompletionCannotReplaceTheCurrentPage()
    {
        using var context = CreateContext();
        var provider = new ControllableProvider();
        var cut = RenderProviderGrid(context, provider);
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));
        var first = provider.Calls.First();

        cut.Render(parameters => parameters.Add(component => component.Page, 2));
        cut.WaitForAssertion(() => Assert.Equal(2, provider.Calls.Count));
        var second = provider.Calls.Last();
        second.Completion.SetResult(new BzsDataGridResult<Row>([new(2, "Current")], false));
        cut.WaitForAssertion(() => Assert.Contains("Current", cut.Find("tbody").TextContent));

        first.Completion.SetResult(new BzsDataGridResult<Row>([new(1, "Stale")], true));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Current", cut.Find("tbody").TextContent);
            Assert.DoesNotContain("Stale", cut.Find("tbody").TextContent);
            Assert.True(first.CancellationToken.IsCancellationRequested);
        });
    }

    [Fact]
    public void KnownTotalShrinkRequestsCorrectionOnceWithoutRenderingAnInvalidPager()
    {
        using var context = CreateContext();
        var requestedPages = new List<int>();
        var provider = new RecordingProvider(request => new BzsDataGridResult<Row>([], totalCount: 20));

        var cut = RenderProviderGrid(
            context,
            provider,
            page: 5,
            configure: parameters => parameters.Add(component => component.PageChanged, requestedPages.Add));

        cut.WaitForAssertion(() => Assert.Equal([2], requestedPages));
        cut.Render(parameters => parameters.Add(component => component.Page, 5));

        Assert.Equal([2], requestedPages);
        Assert.Single(provider.Calls);
        Assert.Empty(cut.FindAll("[data-bzs-pagination-page]"));
    }

    [Fact]
    public void AcceptedKnownTotalShrinkCorrectsThenAcceptsTheLastValidPage()
    {
        using var context = CreateContext();
        var provider = new ControllableProvider();
        var page = 5;
        IRenderedComponent<BzsDataGrid<Row>>? cut = null;
        cut = RenderProviderGrid(
            context,
            provider,
            page,
            configure: parameters => parameters.Add(component => component.PageChanged, requested =>
            {
                page = requested;
                cut!.Render(updated => updated.Add(component => component.Page, requested));
            }));
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));
        provider.Calls.Single().Completion.SetResult(
            new BzsDataGridResult<Row>([new(41, "Previously accepted")], totalCount: 50));
        cut.WaitForAssertion(() => Assert.Contains("Previously accepted", cut.Find("tbody").TextContent));

        cut.Render(parameters => parameters.Add(
            component => component.Sort,
            new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending)));
        cut.WaitForAssertion(() => Assert.Equal(2, provider.Calls.Count));
        provider.Calls.Last().Completion.SetResult(new BzsDataGridResult<Row>([], totalCount: 20));

        cut.WaitForAssertion(() => Assert.Equal(2, page));
        cut.WaitForAssertion(() => Assert.Equal(3, provider.Calls.Count));
        Assert.Contains("Previously accepted", cut.Find("tbody").TextContent);
        provider.Calls.Last().Completion.SetResult(
            new BzsDataGridResult<Row>([new(11, "Corrected page")], totalCount: 20));

        cut.WaitForAssertion(() => Assert.Contains("Corrected page", cut.Find("tbody").TextContent));
        Assert.Equal("2", cut.Find("[data-bzs-pagination-page='2']").TextContent.Trim());
    }

    [Fact]
    public void FilterEditorRequestsControlledSnapshotAndStaysOutsideTheMenu()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request => new BzsDataGridResult<Row>([], false));
        IReadOnlyList<BzsDataGridFilter>? requested = null;
        var cut = RenderProviderGrid(
            context,
            provider,
            configure: parameters => parameters.Add(component => component.FiltersChanged, value => requested = value));
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));

        var input = cut.Find("[data-bzs-data-grid-filter='name'] input");
        Assert.Null(input.Closest("[role='menu']"));
        input.Input("Ada");
        cut.Find("button[aria-label='Apply Name filter']").Click();

        var filter = Assert.IsType<BzsDataGridTextFilter>(Assert.Single(requested!));
        Assert.Equal("Ada", filter.Value);
        Assert.Empty(cut.Instance.Filters);
        Assert.Single(provider.Calls);

        cut.Find("button[aria-label='Name column menu']").Click();
        Assert.Contains("Sort ascending", cut.Find("[role='menu']").TextContent);
        Assert.Contains("Clear filter", cut.Find("[role='menu']").TextContent);
    }

    [Fact]
    public void EditingATextFilterPreservesItsCaseSensitivity()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request => new BzsDataGridResult<Row>([], false));
        IReadOnlyList<BzsDataGridFilter>? requested = null;
        var cut = RenderProviderGrid(
            context,
            provider,
            filters: [new BzsDataGridTextFilter("name", "Ada", caseSensitive: true)],
            configure: parameters => parameters.Add(component => component.FiltersChanged, value => requested = value));
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));

        cut.Find("[data-bzs-data-grid-filter='name'] input").Input("Grace");
        cut.Find("button[aria-label='Apply Name filter']").Click();

        var filter = Assert.IsType<BzsDataGridTextFilter>(Assert.Single(requested!));
        Assert.Equal("Grace", filter.Value);
        Assert.True(filter.CaseSensitive);
    }

    [Fact]
    public void RejectedClearFilterRetainsDraftUntilTheControlledFilterIsRemoved()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request => new BzsDataGridResult<Row>([], false));
        IReadOnlyList<BzsDataGridFilter>? requested = null;
        var cut = RenderProviderGrid(
            context,
            provider,
            filters: [new BzsDataGridTextFilter("name", "Ada")],
            configure: parameters => parameters.Add(component => component.FiltersChanged, value => requested = value));
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));

        cut.Find("button[aria-label='Name column menu']").Click();
        cut.FindAll("[role='menuitem']").Single(item => item.TextContent.Contains("Clear filter", StringComparison.Ordinal)).Click();

        Assert.Empty(requested!);
        Assert.Equal("Ada", cut.Find("[data-bzs-data-grid-filter='name'] input").GetAttribute("value"));

        cut.Render(parameters => parameters.Add(component => component.Filters, Array.Empty<BzsDataGridFilter>()));
        Assert.Equal(string.Empty, cut.Find("[data-bzs-data-grid-filter='name'] input").GetAttribute("value"));
    }

    [Fact]
    public void ProviderFailureRetainsAcceptedRowsAndRetryUsesTheCurrentRequest()
    {
        using var context = CreateContext();
        var provider = new ControllableProvider();
        var failures = new List<Exception>();
        var cut = RenderProviderGrid(
            context,
            provider,
            configure: parameters => parameters.Add(component => component.ProviderFailed, failures.Add));
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));
        provider.Calls.Single().Completion.SetResult(
            new BzsDataGridResult<Row>([new(1, "Accepted")], true));
        cut.WaitForAssertion(() => Assert.Contains("Accepted", cut.Find("tbody").TextContent));

        cut.Render(parameters => parameters.Add(component => component.Page, 2));
        cut.WaitForAssertion(() => Assert.Equal(2, provider.Calls.Count));
        provider.Calls.Last().Completion.SetException(new InvalidOperationException("private detail"));
        cut.WaitForAssertion(() => Assert.Single(failures));

        Assert.Contains("Accepted", cut.Find("tbody").TextContent);
        Assert.DoesNotContain("private detail", cut.Markup);
        cut.Find(".bzs-data-grid__provider-state button").Click();
        cut.WaitForAssertion(() => Assert.Equal(3, provider.Calls.Count));
        Assert.Equal(2, provider.Calls.Last().Request.Page);
    }

    [Fact]
    public void InitialFailureWithErrorTemplateStillOffersRetry()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(_ => throw new InvalidOperationException("private detail"));
        var cut = RenderProviderGrid(
            context,
            provider,
            configure: parameters => parameters.Add(
                component => component.ErrorTemplate,
                error => builder => builder.AddContent(0, $"Custom: {error.Message}")));

        cut.WaitForAssertion(() => Assert.Contains("Custom: private detail", cut.Find("tbody").TextContent));
        cut.Find("tbody button").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, provider.Calls.Count));
    }

    [Fact]
    public async Task ProviderFailureCommitsErrorBeforeCallbackAndSurvivesDisposalDuringCallback()
    {
        using var context = CreateContext();
        var provider = new ControllableProvider();
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IRenderedComponent<BzsDataGrid<Row>>? cut = null;
        cut = RenderProviderGrid(
            context,
            provider,
            configure: parameters => parameters.Add(component => component.ProviderFailed, async _ =>
            {
                callbackStarted.SetResult();
                await cut!.Instance.DisposeAsync();
                callbackFinished.SetResult();
            }));
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));
        provider.Calls.Single().Completion.SetException(new InvalidOperationException("private detail"));

        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("Data could not be loaded", cut.Markup);
        await callbackFinished.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidProviderItemKeysFailRetryablyAndRetainAcceptedRows(bool nullKey)
    {
        using var context = CreateContext();
        var provider = new ControllableProvider();
        var failures = new List<Exception>();
        var cut = RenderProviderGrid(
            context,
            provider,
            configure: parameters =>
            {
                parameters.Add(
                    component => component.ItemKey,
                    nullKey
                        ? row => row.Id == 0 ? null : row.Id
                        : row => row.Id == 1 ? row.Id : 0);
                parameters.Add(component => component.ProviderFailed, failures.Add);
            });
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));
        provider.Calls.Single().Completion.SetResult(
            new BzsDataGridResult<Row>([new(1, "Accepted")], hasNextPage: true));
        cut.WaitForAssertion(() => Assert.Contains("Accepted", cut.Find("tbody").TextContent));

        cut.Render(parameters => parameters.Add(component => component.Page, 2));
        cut.WaitForAssertion(() => Assert.Equal(2, provider.Calls.Count));
        provider.Calls.Last().Completion.SetResult(nullKey
            ? new BzsDataGridResult<Row>([new(0, "Null key")], hasNextPage: false)
            : new BzsDataGridResult<Row>([new(2, "Duplicate one"), new(3, "Duplicate two")], hasNextPage: false));

        cut.WaitForAssertion(() => Assert.Single(failures));
        Assert.Contains(nullKey ? "returned null" : "duplicate key", failures[0].Message, StringComparison.Ordinal);
        Assert.Contains("Accepted", cut.Find("tbody").TextContent);
        Assert.DoesNotContain(nullKey ? "Null key" : "Duplicate one", cut.Find("tbody").TextContent);
        cut.Find(".bzs-data-grid__provider-state button").Click();
        cut.WaitForAssertion(() => Assert.Equal(3, provider.Calls.Count));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RetainedPagerRetriesAFailedControlledPageForKnownAndUnknownTotals(bool knownTotal)
    {
        using var context = CreateContext();
        var provider = new ControllableProvider();
        var cut = RenderProviderGrid(context, provider);
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));
        provider.Calls.Single().Completion.SetResult(knownTotal
            ? new BzsDataGridResult<Row>([new(1, "Accepted")], totalCount: 20)
            : new BzsDataGridResult<Row>([new(1, "Accepted")], hasNextPage: true));
        cut.WaitForAssertion(() => Assert.Contains("Accepted", cut.Find("tbody").TextContent));

        cut.Render(parameters => parameters.Add(component => component.Page, 2));
        cut.WaitForAssertion(() => Assert.Equal(2, provider.Calls.Count));
        provider.Calls.Last().Completion.SetException(new InvalidOperationException("page failed"));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".bzs-data-grid__provider-state")));

        var next = knownTotal
            ? cut.Find("button[data-bzs-pagination-command='next']")
            : cut.Find(".bzs-data-grid__unknown-pagination button[aria-label='Next page']");
        Assert.False(next.HasAttribute("disabled"));
        next.Click();

        cut.WaitForAssertion(() => Assert.Equal(3, provider.Calls.Count));
        Assert.Equal(2, provider.Calls.Last().Request.Page);
    }

    [Fact]
    public void ProviderAriaSortTracksOnlyTheAcceptedRequest()
    {
        using var context = CreateContext();
        var provider = new ControllableProvider();
        var ascending = new BzsDataGridSort("name", BzsDataGridSortDirection.Ascending);
        var descending = new BzsDataGridSort("name", BzsDataGridSortDirection.Descending);
        var cut = RenderProviderGrid(
            context,
            provider,
            configure: parameters => parameters.Add(component => component.Sort, ascending));
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));
        Assert.Empty(cut.FindAll("th[aria-sort]"));

        provider.Calls.Single().Completion.SetResult(
            new BzsDataGridResult<Row>([new(1, "Accepted")], hasNextPage: false));
        cut.WaitForAssertion(() => Assert.Equal("ascending", cut.Find("th[aria-sort]").GetAttribute("aria-sort")));

        cut.Render(parameters => parameters.Add(component => component.Sort, descending));
        cut.WaitForAssertion(() => Assert.Equal(2, provider.Calls.Count));
        Assert.Equal("ascending", cut.Find("th[aria-sort]").GetAttribute("aria-sort"));

        provider.Calls.Last().Completion.SetException(new InvalidOperationException("sort failed"));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".bzs-data-grid__provider-state")));
        Assert.Equal("ascending", cut.Find("th[aria-sort]").GetAttribute("aria-sort"));
    }

    [Fact]
    public void MultipleSelectionPreservesItemsFromOtherProviderPages()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request =>
            new BzsDataGridResult<Row>([new(2, "Second page")], false));
        var firstPageSelection = new Row(1, "First page");
        IReadOnlyList<Row>? requested = null;
        var cut = RenderProviderGrid(
            context,
            provider,
            page: 2,
            configure: parameters =>
            {
                parameters.Add(component => component.SelectionMode, BzsDataGridSelectionMode.Multiple);
                parameters.Add(component => component.ItemKey, row => row.Id);
                parameters.Add(component => component.SelectedItems, new[] { firstPageSelection });
                parameters.Add(component => component.SelectedItemsChanged, value => requested = value);
            });
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("tbody input[type='checkbox']")));

        cut.Find("tbody input[type='checkbox']").Change(true);

        var selection = Assert.IsAssignableFrom<IReadOnlyList<Row>>(requested);
        Assert.Equal([1, 2], selection.Select(static row => row.Id));
        Assert.Same(firstPageSelection, selection[0]);
    }

    [Fact]
    public void PageSizeChangeCoalescesTheResetAndSizeCallbacksIntoOneRequest()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request => new BzsDataGridResult<Row>([], false));
        IRenderedComponent<BzsDataGrid<Row>>? cut = null;
        var page = 2;
        var pageSize = 10;
        Action<ComponentParameterCollectionBuilder<BzsDataGrid<Row>>> configure = parameters =>
        {
            parameters.Add(component => component.PageSize, pageSize);
            parameters.Add(component => component.PageChanged, requested =>
            {
                page = requested;
                cut!.Render(updated =>
                {
                    updated.Add(component => component.Page, page);
                    updated.Add(component => component.PageSize, pageSize);
                });
            });
            parameters.Add(component => component.PageSizeChanged, requested =>
            {
                pageSize = requested;
                cut!.Render(updated =>
                {
                    updated.Add(component => component.Page, page);
                    updated.Add(component => component.PageSize, pageSize);
                });
            });
        };
        cut = RenderProviderGrid(context, provider, page, configure: configure);
        cut.WaitForAssertion(() => Assert.Single(provider.Calls));

        cut.Find(".bzs-data-grid__page-size select").Change("25");

        cut.WaitForAssertion(() => Assert.Equal(2, provider.Calls.Count));
        Assert.Equal(1, provider.Calls.Last().Page);
        Assert.Equal(25, provider.Calls.Last().PageSize);
    }

    [Fact]
    public void ReplacingTheProviderCancelsOldWorkAndClearsItsAcceptedState()
    {
        using var context = CreateContext();
        var firstProvider = new ControllableProvider();
        var secondProvider = new RecordingProvider(request =>
            new BzsDataGridResult<Row>([new(2, "Replacement")], false));
        var cut = RenderProviderGrid(context, firstProvider);
        cut.WaitForAssertion(() => Assert.Single(firstProvider.Calls));
        var oldCall = firstProvider.Calls.Single();

        cut.Render(parameters => parameters.Add(component => component.Provider, secondProvider));

        cut.WaitForAssertion(() => Assert.Contains("Replacement", cut.Find("tbody").TextContent));
        Assert.True(oldCall.CancellationToken.IsCancellationRequested);
        oldCall.Completion.SetResult(new BzsDataGridResult<Row>([new(1, "Old")], false));
        cut.WaitForAssertion(() => Assert.DoesNotContain("Old", cut.Find("tbody").TextContent));
    }

    [Fact]
    public void UnknownTotalsExposeOnlyPreviousAndNextCommands()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request =>
            new BzsDataGridResult<Row>([new(2, "Last")], hasNextPage: false));
        var cut = RenderProviderGrid(context, provider, page: 2);
        cut.WaitForAssertion(() => Assert.Contains("Last", cut.Find("tbody").TextContent));

        var pager = cut.Find(".bzs-data-grid__unknown-pagination");
        Assert.Empty(pager.QuerySelectorAll("[data-bzs-pagination-page]"));
        Assert.False(pager.QuerySelector("button[aria-label='Previous page']")!.HasAttribute("disabled"));
        Assert.True(pager.QuerySelector("button[aria-label='Next page']")!.HasAttribute("disabled"));
    }

    [Fact]
    public async Task StaticSsrRendersHeadersAndLoadingStateWithoutInvokingTheProvider()
    {
        using var context = CreateContext();
        var provider = new RecordingProvider(request => new BzsDataGridResult<Row>([], false));
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsDataGrid<Row>.Provider)] = provider,
            [nameof(BzsDataGrid<Row>.ChildContent)] = BuildColumns(),
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsDataGrid<Row>>(parameters);
            return output.ToHtmlString();
        });

        Assert.Empty(provider.Calls);
        Assert.Contains("<th", html, StringComparison.Ordinal);
        Assert.Contains("Loading data", html, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidProviderResultBecomesObservableRetryableError()
    {
        using var context = CreateContext();
        var failures = new List<Exception>();
        var provider = new RecordingProvider(request =>
            new BzsDataGridResult<Row>(
                Enumerable.Range(1, 11).Select(index => new Row(index, $"Row {index}")).ToArray(),
                hasNextPage: false));
        var cut = RenderProviderGrid(
            context,
            provider,
            configure: parameters => parameters.Add(component => component.ProviderFailed, failures.Add));

        cut.WaitForAssertion(() => Assert.Single(failures));
        Assert.Contains("Data could not be loaded", cut.Markup);
        Assert.DoesNotContain(failures[0].Message, cut.Markup);
        Assert.Equal("Retry", cut.Find("tbody button").TextContent.Trim());
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        context.Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();
        return context;
    }

    private static IRenderedComponent<BzsDataGrid<Row>> RenderProviderGrid(
        BunitContext context,
        IBzsDataGridProvider<Row> provider,
        int page = 1,
        IReadOnlyList<BzsDataGridFilter>? filters = null,
        Action<ComponentParameterCollectionBuilder<BzsDataGrid<Row>>>? configure = null) =>
        context.Render<BzsDataGrid<Row>>(parameters =>
        {
            parameters.Add(component => component.Provider, provider);
            parameters.Add(component => component.Page, page);
            parameters.Add(component => component.Filters, filters ?? Array.Empty<BzsDataGridFilter>());
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
        builder.AddAttribute(5, nameof(BzsDataGridColumn<Row>.FilterKind), BzsDataGridFilterKind.Text);
        builder.CloseComponent();
    };

    private sealed class RecordingProvider(
        Func<BzsDataGridRequest, BzsDataGridResult<Row>> resultFactory) : IBzsDataGridProvider<Row>
    {
        internal ConcurrentQueue<BzsDataGridRequest> Calls { get; } = new();

        public ValueTask<BzsDataGridResult<Row>> GetItemsAsync(
            BzsDataGridRequest request,
            CancellationToken cancellationToken)
        {
            Calls.Enqueue(request);
            return ValueTask.FromResult(resultFactory(request));
        }
    }

    private sealed class ControllableProvider : IBzsDataGridProvider<Row>
    {
        internal ConcurrentQueue<ProviderCall> Calls { get; } = new();

        public ValueTask<BzsDataGridResult<Row>> GetItemsAsync(
            BzsDataGridRequest request,
            CancellationToken cancellationToken)
        {
            var call = new ProviderCall(request, cancellationToken);
            Calls.Enqueue(call);
            return new ValueTask<BzsDataGridResult<Row>>(call.Completion.Task);
        }
    }

    private sealed record ProviderCall(BzsDataGridRequest Request, CancellationToken CancellationToken)
    {
        internal TaskCompletionSource<BzsDataGridResult<Row>> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record Row(int Id, string Name);
}
