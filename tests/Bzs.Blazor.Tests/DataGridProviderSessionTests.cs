using System.Collections.Concurrent;

namespace Bzs.Blazor.Tests;

/// <summary>
/// Drives <see cref="BzsDataGridProviderSession{TItem}" /> directly, without rendering a grid, so
/// that supersession, queueing, failure retention, and page correction are asserted against the
/// module that decides them rather than through the markup they eventually produce.
/// </summary>
public sealed class DataGridProviderSessionTests
{
    [Fact]
    public async Task StructurallyEqualRequestDoesNotReachTheProviderTwice()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var first = session.Submit(new BzsDataGridRequest(1, 10))!;
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([1], totalCount: 1));
        await first;

        Assert.Null(session.Submit(new BzsDataGridRequest(1, 10)));
        Assert.Single(provider.Calls);
    }

    [Fact]
    public async Task ADifferentRequestSupersedesTheOneInFlight()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var stale = session.Submit(new BzsDataGridRequest(1, 10))!;
        var staleCall = provider.Calls.Single();
        var current = session.Submit(new BzsDataGridRequest(2, 10))!;
        var currentCall = provider.Calls.Last();

        currentCall.Completion.SetResult(new BzsDataGridResult<int>([2], totalCount: 20));
        staleCall.Completion.SetResult(new BzsDataGridResult<int>([1], totalCount: 20));
        await current;
        await stale;

        Assert.True(staleCall.CancellationToken.IsCancellationRequested);
        Assert.Equal([2], session.AcceptedItems);
        Assert.Equal(2, session.AcceptedRequest!.Page);
    }

    [Fact]
    public async Task ARefreshQueuedBeforeInteractivityRunsOnceForTheRequestItObserved()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);
        var request = new BzsDataGridRequest(2, 10);

        var refresh = session.QueueRefresh(request, isInteractive: false);

        Assert.Empty(provider.Calls);
        Assert.False(refresh.IsCompleted);

        Assert.Null(session.Submit(new BzsDataGridRequest(2, 10)));
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([2], totalCount: 20));
        await refresh;

        Assert.Equal([2], session.AcceptedItems);
        Assert.Single(provider.Calls);
    }

    [Fact]
    public async Task AQueuedRefreshIsSupersededWhenTheParametersNoLongerDescribeIt()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var refresh = session.QueueRefresh(new BzsDataGridRequest(2, 10), isInteractive: false);
        var load = session.Submit(new BzsDataGridRequest(3, 10))!;

        await refresh;
        Assert.Equal(3, provider.Calls.Single().Request.Page);

        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([3], totalCount: 30));
        await load;

        Assert.Equal(3, session.AcceptedRequest!.Page);
    }

    [Fact]
    public async Task AFailureKeepsTheAcceptedPageAndReportsOnce()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var accepted = session.Submit(new BzsDataGridRequest(1, 10))!;
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([1], totalCount: 20));
        await accepted;

        var failing = session.Submit(new BzsDataGridRequest(2, 10))!;
        provider.Calls.Last().Completion.SetException(new InvalidOperationException("provider down"));
        await failing;

        Assert.Equal([1], session.AcceptedItems);
        Assert.Equal(1, session.AcceptedRequest!.Page);
        Assert.IsType<InvalidOperationException>(session.Error);
        Assert.Same(session.Error, Assert.Single(recorder.Failures));
        Assert.False(session.IsLoading);
    }

    [Fact]
    public async Task AResultLargerThanThePageSizeFailsWithoutReplacingTheAcceptedPage()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var accepted = session.Submit(new BzsDataGridRequest(1, 2))!;
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([1, 2], totalCount: 4));
        await accepted;

        var invalid = session.Submit(new BzsDataGridRequest(2, 2))!;
        provider.Calls.Last().Completion.SetResult(new BzsDataGridResult<int>([3, 4, 5], totalCount: 4));
        await invalid;

        Assert.Equal([1, 2], session.AcceptedItems);
        Assert.Contains("page size", Assert.Single(recorder.Failures).Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARejectedItemKeyBecomesTheSessionFailure()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>
        {
            ItemKeyValidation = static _ =>
                throw new InvalidOperationException("BzsDataGrid requires unique item keys."),
        };
        using var session = CreateSession(provider, recorder);

        var load = session.Submit(new BzsDataGridRequest(1, 10))!;
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([1, 1], totalCount: 2));
        await load;

        Assert.False(session.HasAcceptedResult);
        Assert.Contains("unique item keys", session.Error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task APageBeyondTheKnownTotalAsksForACorrectionInsteadOfAccepting()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var load = session.Submit(new BzsDataGridRequest(5, 10))!;
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([], totalCount: 20));
        await load;

        Assert.Equal(2, Assert.Single(recorder.PageCorrections));
        Assert.False(session.HasAcceptedResult);
        Assert.Null(session.Error);
    }

    [Fact]
    public async Task RetryReloadsTheRequestThatFailed()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);
        var request = new BzsDataGridRequest(2, 10);

        var failing = session.Submit(request)!;
        provider.Calls.Single().Completion.SetException(new InvalidOperationException("transient"));
        await failing;

        Assert.Null(session.Submit(new BzsDataGridRequest(2, 10)));

        session.Retry();
        var retried = session.Submit(new BzsDataGridRequest(2, 10))!;
        provider.Calls.Last().Completion.SetResult(new BzsDataGridResult<int>([2], totalCount: 20));
        await retried;

        Assert.Equal(2, provider.Calls.Count);
        Assert.All(provider.Calls, call => Assert.Equal(2, call.Request.Page));
        Assert.Equal([2], session.AcceptedItems);
        Assert.Null(session.Error);
    }

    [Fact]
    public async Task ASessionIsLoadingUntilItAcceptsOrFails()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        Assert.True(session.IsLoading);
        Assert.False(session.HasAcceptedResult);
        Assert.Empty(session.AcceptedItems);

        var accepted = session.Submit(new BzsDataGridRequest(1, 10))!;
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([1], totalCount: 20));
        await accepted;

        Assert.False(session.IsLoading);

        var inFlight = session.Submit(new BzsDataGridRequest(2, 10))!;

        Assert.True(session.IsLoading);

        provider.Calls.Last().Completion.SetResult(new BzsDataGridResult<int>([2], totalCount: 20));
        await inFlight;

        Assert.False(session.IsLoading);
    }

    [Fact]
    public async Task DisposeCompletesAQueuedRefreshAndCancelsTheCallInFlight()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        var session = CreateSession(provider, recorder);

        var inFlight = session.Submit(new BzsDataGridRequest(1, 10))!;
        var call = provider.Calls.Single();
        var queued = session.QueueRefresh(new BzsDataGridRequest(2, 10), isInteractive: false);

        session.Dispose();

        await queued;
        Assert.True(call.CancellationToken.IsCancellationRequested);

        call.Completion.SetResult(new BzsDataGridResult<int>([1], totalCount: 10));
        await inFlight;

        Assert.False(session.HasAcceptedResult);
        Assert.Empty(recorder.Failures);
    }

    [Fact]
    public async Task AnAcceptedResultNotifiesTheOwnerBeforeItRenders()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var load = session.Submit(new BzsDataGridRequest(1, 10))!;
        provider.Calls.Single().Completion.SetResult(
            new BzsDataGridResult<int>([1, 2], hasNextPage: true));
        await load;

        Assert.Equal("accepted", recorder.Notifications[^2]);
        Assert.Equal("rendered", recorder.Notifications[^1]);
        Assert.Single(recorder.Notifications, static notification => notification == "accepted");
        Assert.True(session.AcceptedHasNextPage);
        Assert.Null(session.AcceptedTotalCount);
    }

    [Fact]
    public async Task AnInteractiveRefreshStartsWithoutWaitingForASubmittedRequest()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var refresh = session.QueueRefresh(new BzsDataGridRequest(2, 10), isInteractive: true);

        Assert.Equal(2, provider.Calls.Single().Request.Page);

        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([2], totalCount: 20));
        await refresh;

        Assert.Equal([2], session.AcceptedItems);
    }

    [Theory]
    [InlineData(3, 2, new[] { 1, 2, 3 }, 10, "page size")]
    [InlineData(3, 2, new[] { 5, 6 }, 2, "beyond its known total")]
    [InlineData(2, 2, new[] { 3, 4 }, 3, "remain in its known total")]
    [InlineData(1, 2, new[] { 1 }, 0, "zero total")]
    public async Task AResultThatContradictsItsRequestBecomesAFailure(
        int page,
        int pageSize,
        int[] items,
        int totalCount,
        string expectedMessagePart)
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var load = session.Submit(new BzsDataGridRequest(page, pageSize))!;
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>(items, totalCount));
        await load;

        Assert.False(session.HasAcceptedResult);
        Assert.Contains(expectedMessagePart, session.Error!.Message, StringComparison.Ordinal);
        Assert.Same(session.Error, Assert.Single(recorder.Failures));
    }

    [Fact]
    public async Task AnEmptyKnownTotalCorrectsToTheFirstPage()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        using var session = CreateSession(provider, recorder);

        var load = session.Submit(new BzsDataGridRequest(3, 10))!;
        provider.Calls.Single().Completion.SetResult(new BzsDataGridResult<int>([], totalCount: 0));
        await load;

        Assert.Equal(1, Assert.Single(recorder.PageCorrections));
        Assert.False(session.HasAcceptedResult);
    }

    [Fact]
    public void ADisposedSessionAcceptsNoFurtherWork()
    {
        var provider = new ControllableProvider<int>();
        var recorder = new CallbackRecorder<int>();
        var session = CreateSession(provider, recorder);

        session.Dispose();

        Assert.Null(session.Submit(new BzsDataGridRequest(1, 10)));
        Assert.Throws<ObjectDisposedException>(() =>
        {
            _ = session.QueueRefresh(new BzsDataGridRequest(1, 10), isInteractive: true);
        });
        Assert.Empty(provider.Calls);
    }

    private static BzsDataGridProviderSession<int> CreateSession(
        IBzsDataGridProvider<int> provider,
        CallbackRecorder<int> recorder) =>
        new(provider, recorder.ToCallbacks());

    private sealed class CallbackRecorder<TItem>
    {
        internal List<Exception> Failures { get; } = [];

        internal List<int> PageCorrections { get; } = [];

        /// <summary>Records "accepted" and "rendered" in the order the session raised them.</summary>
        internal List<string> Notifications { get; } = [];

        internal Action<IReadOnlyList<TItem>>? ItemKeyValidation { get; init; }

        internal BzsDataGridProviderSessionCallbacks<TItem> ToCallbacks() => new(
            StateChanged: () => Notifications.Add("rendered"),
            ResultAccepted: () => Notifications.Add("accepted"),
            ValidateItemKeys: items => ItemKeyValidation?.Invoke(items),
            ProviderFailed: error =>
            {
                Failures.Add(error);
                return Task.CompletedTask;
            },
            PageCorrectionRequested: page =>
            {
                PageCorrections.Add(page);
                return Task.CompletedTask;
            },
            UnhandledError: error =>
            {
                Failures.Add(error);
                return Task.CompletedTask;
            });
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
}
