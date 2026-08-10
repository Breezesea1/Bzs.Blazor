using System.Collections.Concurrent;

namespace Bzs.Blazor.Tests;

public sealed class DataGridProviderTests
{
    [Fact]
    public void RequestSnapshotsFiltersAndRejectsDuplicateColumns()
    {
        var filters = new List<BzsDataGridFilter>
        {
            new BzsDataGridTextFilter("name", "Ada"),
        };
        var request = new BzsDataGridRequest(2, 25, filters: filters);

        filters.Clear();

        var filter = Assert.IsType<BzsDataGridTextFilter>(Assert.Single(request.Filters));
        Assert.Equal("name", filter.ColumnKey);
        Assert.Equal("Ada", filter.Value);
        Assert.Throws<ArgumentException>(() => new BzsDataGridRequest(
            1,
            10,
            filters:
            [
                new BzsDataGridTextFilter("name", "Ada"),
                new BzsDataGridBooleanFilter("name", true),
            ]));
    }

    [Fact]
    public void FilterContractsValidateAndNormalizeTheirValues()
    {
        var text = new BzsDataGridTextFilter(
            " name ",
            " Ada ",
            BzsDataGridTextOperator.StartsWith,
            caseSensitive: true);
        var number = new BzsDataGridNumberFilter("score", 42.5m, BzsDataGridComparisonOperator.GreaterThan);
        var date = new BzsDataGridDateFilter("created", new DateOnly(2026, 8, 9));
        var boolean = new BzsDataGridBooleanFilter("active", true);

        Assert.Equal("name", text.ColumnKey);
        Assert.Equal("Ada", text.Value);
        Assert.True(text.CaseSensitive);
        Assert.Equal(42.5m, number.Value);
        Assert.Equal(new DateOnly(2026, 8, 9), date.Value);
        Assert.True(boolean.Value);
        Assert.Throws<ArgumentException>(() => new BzsDataGridTextFilter("name", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BzsDataGridNumberFilter(
            "score",
            1,
            (BzsDataGridComparisonOperator)99));
    }

    [Fact]
    public void ResultConstructorsMakeKnownAndUnknownTotalsMutuallyExclusive()
    {
        var known = new BzsDataGridResult<int>([1, 2], totalCount: 5);
        var unknown = new BzsDataGridResult<int>([3], hasNextPage: true);

        Assert.Equal(5, known.TotalCount);
        Assert.Null(known.HasNextPage);
        Assert.Null(unknown.TotalCount);
        Assert.True(unknown.HasNextPage);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BzsDataGridResult<int>([], -1));
    }

    [Fact]
    public void ResultSnapshotsProviderOwnedItems()
    {
        var items = new List<int> { 1, 2 };
        var result = new BzsDataGridResult<int>(items, totalCount: 2);

        items[0] = 99;
        items.Add(3);

        Assert.Equal([1, 2], result.Items);
    }

    [Fact]
    public async Task SupersededSuccessCannotReplaceTheCurrentResult()
    {
        var provider = new ControllableProvider<int>();
        using var coordinator = new BzsDataGridRequestCoordinator<int>(provider);
        var firstRequest = new BzsDataGridRequest(1, 10);
        var secondRequest = new BzsDataGridRequest(2, 10);

        var firstTask = coordinator.LoadAsync(firstRequest);
        var firstCall = provider.Calls.Single();
        var secondTask = coordinator.LoadAsync(secondRequest);
        var secondCall = provider.Calls.Last();
        secondCall.Completion.SetResult(new BzsDataGridResult<int>([2], false));
        firstCall.Completion.SetResult(new BzsDataGridResult<int>([1], true));

        var second = await secondTask;
        var first = await firstTask;

        Assert.True(second.IsCurrent);
        Assert.Equal([2], second.Result!.Items);
        Assert.False(first.IsCurrent);
        Assert.Null(first.Result);
        Assert.True(firstCall.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task SupersederCancelsWithoutDisposingTheRequestOwnedTokenSource()
    {
        var provider = new CancellationCleanupProvider();
        using var coordinator = new BzsDataGridRequestCoordinator<int>(provider);

        var firstTask = coordinator.LoadAsync(new BzsDataGridRequest(1, 10));
        var firstCall = provider.Calls.Single();
        var secondTask = coordinator.LoadAsync(new BzsDataGridRequest(2, 10));

        Assert.False((await firstTask).IsCurrent);
        Assert.True((await secondTask).IsCurrent);
        Assert.True(firstCall.CancellationToken.IsCancellationRequested);
        Assert.Null(firstCall.CleanupError);
    }

    [Fact]
    public async Task SupersededFailureIsSuppressed()
    {
        var provider = new ControllableProvider<int>();
        using var coordinator = new BzsDataGridRequestCoordinator<int>(provider);

        var firstTask = coordinator.LoadAsync(new BzsDataGridRequest(1, 10));
        var firstCall = provider.Calls.Single();
        var secondTask = coordinator.LoadAsync(new BzsDataGridRequest(2, 10));
        var secondCall = provider.Calls.Last();
        firstCall.Completion.SetException(new InvalidOperationException("stale"));
        secondCall.Completion.SetResult(new BzsDataGridResult<int>([2], false));

        var first = await firstTask;
        var second = await secondTask;

        Assert.False(first.IsCurrent);
        Assert.Null(first.Error);
        Assert.True(second.IsCurrent);
    }

    [Fact]
    public async Task CurrentUncanceledOperationCanceledExceptionIsAProviderFailure()
    {
        var provider = new ControllableProvider<int>();
        using var coordinator = new BzsDataGridRequestCoordinator<int>(provider);

        var task = coordinator.LoadAsync(new BzsDataGridRequest(1, 10));
        provider.Calls.Single().Completion.SetException(new OperationCanceledException("provider timeout"));

        var result = await task;

        Assert.True(result.IsCurrent);
        Assert.IsType<OperationCanceledException>(result.Error);
    }

    [Fact]
    public async Task DisposeCancelsAndSuppressesAnInFlightCompletion()
    {
        var provider = new ControllableProvider<int>();
        var coordinator = new BzsDataGridRequestCoordinator<int>(provider);

        var task = coordinator.LoadAsync(new BzsDataGridRequest(1, 10));
        var call = provider.Calls.Single();
        coordinator.Dispose();

        Assert.True(call.CancellationToken.IsCancellationRequested);
        Assert.Null(Record.Exception(() => _ = call.CancellationToken.WaitHandle));

        call.Completion.SetResult(new BzsDataGridResult<int>([1], false));

        var result = await task;

        Assert.False(result.IsCurrent);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => coordinator.LoadAsync(new BzsDataGridRequest(1, 10)));
    }

    [Fact]
    public async Task DisposeCancelsWithoutDisposingTheTokenDuringProviderCleanup()
    {
        var provider = new CancellationCleanupProvider();
        var coordinator = new BzsDataGridRequestCoordinator<int>(provider);

        var task = coordinator.LoadAsync(new BzsDataGridRequest(1, 10));
        var call = provider.Calls.Single();
        coordinator.Dispose();

        var result = await task;

        Assert.False(result.IsCurrent);
        Assert.True(call.CancellationToken.IsCancellationRequested);
        Assert.Null(call.CleanupError);
    }

    private sealed class ControllableProvider<TItem> : IBzsDataGridProvider<TItem>
    {
        internal ConcurrentQueue<ProviderCall<TItem>> Calls { get; } = new();

        public ValueTask<BzsDataGridResult<TItem>> GetItemsAsync(
            BzsDataGridRequest request,
            CancellationToken cancellationToken)
        {
            var call = new ProviderCall<TItem>(request, cancellationToken);
            Calls.Enqueue(call);
            return new ValueTask<BzsDataGridResult<TItem>>(call.Completion.Task);
        }
    }

    private sealed record ProviderCall<TItem>(
        BzsDataGridRequest Request,
        CancellationToken CancellationToken)
    {
        internal TaskCompletionSource<BzsDataGridResult<TItem>> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class CancellationCleanupProvider : IBzsDataGridProvider<int>
    {
        internal ConcurrentQueue<CancellationCleanupCall> Calls { get; } = new();

        public async ValueTask<BzsDataGridResult<int>> GetItemsAsync(
            BzsDataGridRequest request,
            CancellationToken cancellationToken)
        {
            var call = new CancellationCleanupCall(cancellationToken);
            Calls.Enqueue(call);
            if (request.Page != 1)
            {
                return new BzsDataGridResult<int>([request.Page], hasNextPage: false);
            }

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }

            try
            {
                _ = cancellationToken.WaitHandle;
            }
            catch (Exception exception)
            {
                call.CleanupError = exception;
            }

            return new BzsDataGridResult<int>([request.Page], hasNextPage: false);
        }
    }

    private sealed class CancellationCleanupCall(CancellationToken cancellationToken)
    {
        internal CancellationToken CancellationToken { get; } = cancellationToken;

        internal Exception? CleanupError { get; set; }
    }
}
