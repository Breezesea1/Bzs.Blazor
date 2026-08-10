namespace Bzs.Blazor;

internal sealed class BzsDataGridRequestCoordinator<TItem> : IDisposable
{
    private readonly IBzsDataGridProvider<TItem> _provider;
    private readonly object _gate = new();
    private CancellationTokenSource? _requestCancellation;
    private long _version;
    private bool _disposed;

    internal BzsDataGridRequestCoordinator(IBzsDataGridProvider<TItem> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    internal async Task<BzsDataGridRequestOutcome<TItem>> LoadAsync(BzsDataGridRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CancellationTokenSource cancellation;
        CancellationTokenSource? superseded;
        long version;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            version = ++_version;
            cancellation = new CancellationTokenSource();
            superseded = _requestCancellation;
            _requestCancellation = cancellation;
        }

        CancelRequest(superseded);

        try
        {
            var result = await _provider.GetItemsAsync(request, cancellation.Token);
            if (result is null)
            {
                throw new InvalidOperationException("The DataGrid provider returned a null result.");
            }

            return IsCurrent(version, cancellation)
                ? BzsDataGridRequestOutcome<TItem>.Succeeded(request, result)
                : BzsDataGridRequestOutcome<TItem>.Superseded(request);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return BzsDataGridRequestOutcome<TItem>.Superseded(request);
        }
        catch (Exception exception)
        {
            return IsCurrent(version, cancellation)
                ? BzsDataGridRequestOutcome<TItem>.Failed(request, exception)
                : BzsDataGridRequestOutcome<TItem>.Superseded(request);
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_requestCancellation, cancellation))
                {
                    _requestCancellation = null;
                }
            }

            cancellation.Dispose();
        }
    }

    internal void Cancel()
    {
        CancellationTokenSource? cancellation;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _version++;
            cancellation = _requestCancellation;
            _requestCancellation = null;
        }

        CancelRequest(cancellation);
    }

    public void Dispose()
    {
        CancellationTokenSource? cancellation;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _version++;
            cancellation = _requestCancellation;
            _requestCancellation = null;
        }

        CancelRequest(cancellation);
    }

    private static void CancelRequest(CancellationTokenSource? cancellation)
    {
        if (cancellation is null)
        {
            return;
        }

        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The owning request completed and disposed its source after it was captured.
        }
    }

    private bool IsCurrent(long version, CancellationTokenSource cancellation)
    {
        lock (_gate)
        {
            return !_disposed
                && version == _version
                && ReferenceEquals(_requestCancellation, cancellation)
                && !cancellation.IsCancellationRequested;
        }
    }
}

internal sealed record BzsDataGridRequestOutcome<TItem>(
    bool IsCurrent,
    BzsDataGridRequest Request,
    BzsDataGridResult<TItem>? Result,
    Exception? Error)
{
    internal static BzsDataGridRequestOutcome<TItem> Succeeded(
        BzsDataGridRequest request,
        BzsDataGridResult<TItem> result) =>
        new(true, request, result, null);

    internal static BzsDataGridRequestOutcome<TItem> Failed(
        BzsDataGridRequest request,
        Exception error) =>
        new(true, request, null, error);

    internal static BzsDataGridRequestOutcome<TItem> Superseded(BzsDataGridRequest request) =>
        new(false, request, null, null);
}
