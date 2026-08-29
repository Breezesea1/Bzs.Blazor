namespace Bzs.Blazor;

/// <summary>
/// The side effects a <see cref="BzsDataGridProviderSession{TItem}" /> hands back to its owning
/// grid: re-rendering, observing an accepted page, validating the keys of an incoming page,
/// reporting a failure, asking the consumer to correct an out-of-range page, and surfacing an
/// exception raised outside an awaited call.
/// </summary>
internal sealed record BzsDataGridProviderSessionCallbacks<TItem>(
    Action StateChanged,
    Action ResultAccepted,
    Action<IReadOnlyList<TItem>> ValidateItemKeys,
    Func<Exception, Task> ProviderFailed,
    Func<int, Task> PageCorrectionRequested,
    Func<Exception, Task> UnhandledError);

/// <summary>
/// Owns which provider request is current for one <see cref="IBzsDataGridProvider{TItem}" />, and
/// the accepted page, failure, and loading state derived from it. It queues a refresh raised before
/// the grid is interactive, drops a request structurally equal to the one already started, cancels
/// a superseded call, and keeps the last accepted page visible across a failure. The owning grid
/// builds requests and renders; a provider swap replaces the whole session.
/// </summary>
internal sealed class BzsDataGridProviderSession<TItem> : IDisposable
{
    private readonly BzsDataGridProviderAdapter<TItem> _providerAdapter;
    private readonly BzsDataGridProviderSessionCallbacks<TItem> _callbacks;
    private PendingRefresh? _queuedRefresh;
    private PendingRefresh? _activeRefresh;
    private BzsDataGridRequest? _startedRequest;
    private BzsDataGridRequest? _acceptedRequest;
    private BzsDataGridResult<TItem>? _acceptedResult;
    private Exception? _error;
    private bool _loading;
    private bool _disposed;

    internal BzsDataGridProviderSession(
        IBzsDataGridProvider<TItem> provider,
        BzsDataGridProviderSessionCallbacks<TItem> callbacks)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(callbacks);
        _providerAdapter = new BzsDataGridProviderAdapter<TItem>(provider);
        _callbacks = callbacks;
    }

    /// <summary>Gets the items of the accepted page, or none while no page has been accepted.</summary>
    internal IReadOnlyList<TItem> AcceptedItems => _acceptedResult?.Items ?? Array.Empty<TItem>();

    /// <summary>Gets the request the accepted page answered, which trails the controlled parameters.</summary>
    internal BzsDataGridRequest? AcceptedRequest => _acceptedRequest;

    internal bool HasAcceptedResult => _acceptedResult is not null;

    internal int? AcceptedTotalCount => _acceptedResult?.TotalCount;

    internal bool AcceptedHasNextPage => _acceptedResult?.HasNextPage == true;

    internal Exception? Error => _error;

    /// <summary>
    /// Gets whether the grid is waiting for a page, which includes the state before the first
    /// request has been started so that the first render is not mistaken for an empty result.
    /// </summary>
    internal bool IsLoading => _loading || _acceptedResult is null && _error is null;

    /// <summary>
    /// Queues a refresh of <paramref name="request" />, superseding any refresh already waiting or
    /// in flight, and returns the task that completes when this refresh succeeds, fails, or is
    /// itself superseded. While the grid is not interactive the refresh waits for the first
    /// interactive render instead of starting, so that it runs once against the parameters it
    /// observed.
    /// </summary>
    internal Task QueueRefresh(BzsDataGridRequest request, bool isInteractive)
    {
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(_disposed, this);
        Supersede(_queuedRefresh);
        Supersede(_activeRefresh);
        _providerAdapter.Cancel();
        _queuedRefresh = null;
        var refresh = new PendingRefresh(request);
        if (isInteractive)
        {
            StartDetached(refresh);
            return refresh.Completion.Task;
        }

        _queuedRefresh = refresh;
        _callbacks.StateChanged();
        return refresh.Completion.Task;
    }

    /// <summary>
    /// Reports the request the grid's current parameters describe and returns the load the caller
    /// must await, or null when nothing has to start. A queued refresh runs only while the
    /// parameters still describe the request it observed; otherwise it is superseded in favour of
    /// the current one. A request structurally equal to the one already started is dropped.
    /// </summary>
    internal Task? Submit(BzsDataGridRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_disposed)
        {
            return null;
        }

        if (_queuedRefresh is { } queued)
        {
            _queuedRefresh = null;
            if (BzsDataGridRequestEquality.RequestsEqual(queued.Request, request))
            {
                StartDetached(queued);
                return null;
            }

            Supersede(queued);
        }

        if (BzsDataGridRequestEquality.RequestsEqual(_startedRequest, request))
        {
            return null;
        }

        Supersede(_activeRefresh);
        _startedRequest = request;
        return LoadAsync(request);
    }

    /// <summary>
    /// Forgets which request was started so that the next submitted request loads again, which is
    /// how the rendered error state retries the request that failed.
    /// </summary>
    internal void Retry() => _startedRequest = null;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Supersede(_queuedRefresh);
        Supersede(_activeRefresh);
        _queuedRefresh = null;
        _activeRefresh = null;
        _providerAdapter.Dispose();
    }

    private async void StartDetached(PendingRefresh refresh)
    {
        try
        {
            await RunAsync(refresh);
        }
        catch (Exception exception)
        {
            await _callbacks.UnhandledError(exception);
        }
    }

    private async Task RunAsync(PendingRefresh refresh)
    {
        _activeRefresh = refresh;
        _startedRequest = refresh.Request;
        try
        {
            await LoadAsync(refresh.Request);
        }
        finally
        {
            if (ReferenceEquals(_activeRefresh, refresh))
            {
                _activeRefresh = null;
            }

            refresh.Completion.TrySetResult();
        }
    }

    private async Task LoadAsync(BzsDataGridRequest request)
    {
        _loading = true;
        _error = null;
        _callbacks.StateChanged();
        var outcome = await _providerAdapter.LoadAsync(request);
        if (_disposed || outcome is BzsCurrentProviderCallOutcome<BzsDataGridResult<TItem>>.Superseded)
        {
            return;
        }

        _loading = false;
        if (outcome is BzsCurrentProviderCallOutcome<BzsDataGridResult<TItem>>.Failed failed)
        {
            await FailAsync(failed.Error);
            return;
        }

        var result = ((BzsCurrentProviderCallOutcome<BzsDataGridResult<TItem>>.Succeeded)outcome).Value;
        try
        {
            ValidateResult(request, result);
            _callbacks.ValidateItemKeys(result.Items);
        }
        catch (InvalidOperationException exception)
        {
            await FailAsync(exception);
            return;
        }

        if (result.TotalCount is int totalCount)
        {
            var pageCount = totalCount == 0
                ? 0
                : (int)((totalCount + (long)request.PageSize - 1) / request.PageSize);
            var lastValidPage = Math.Max(1, pageCount);
            if (request.Page > lastValidPage)
            {
                _callbacks.StateChanged();
                await _callbacks.PageCorrectionRequested(lastValidPage);
                return;
            }
        }

        _acceptedRequest = request;
        _acceptedResult = result;
        _error = null;
        _callbacks.ResultAccepted();
        _callbacks.StateChanged();
    }

    private async Task FailAsync(Exception error)
    {
        _error = error;
        if (!_disposed)
        {
            _callbacks.StateChanged();
        }

        await _callbacks.ProviderFailed(error);
    }

    private static void ValidateResult(BzsDataGridRequest request, BzsDataGridResult<TItem> result)
    {
        if (result.Items.Count > request.PageSize)
        {
            throw new InvalidOperationException("The DataGrid provider returned more items than the requested page size.");
        }

        if (result.TotalCount is not int totalCount)
        {
            return;
        }

        var offset = (request.Page - 1L) * request.PageSize;
        if (offset >= totalCount && totalCount > 0 && result.Items.Count != 0)
        {
            throw new InvalidOperationException("The DataGrid provider returned items beyond its known total.");
        }
        if (offset < totalCount && result.Items.Count > totalCount - offset)
        {
            throw new InvalidOperationException("The DataGrid provider returned more items than remain in its known total.");
        }
        if (totalCount == 0 && result.Items.Count != 0)
        {
            throw new InvalidOperationException("A DataGrid provider result with a zero total cannot contain items.");
        }
    }

    private static void Supersede(PendingRefresh? refresh) => refresh?.Completion.TrySetResult();

    private sealed class PendingRefresh(BzsDataGridRequest request)
    {
        internal BzsDataGridRequest Request { get; } = request;

        internal TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
