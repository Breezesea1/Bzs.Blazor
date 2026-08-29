namespace Bzs.Blazor;

/// <summary>Owns supersession, cancellation, and outcome classification for one provider operation.</summary>
internal sealed class BzsCurrentProviderCall<TResult> : IDisposable
{
    private readonly object _gate = new();
    private CancellationTokenSource? _currentCancellation;
    private long _version;
    private bool _disposed;

    internal Task<BzsCurrentProviderCallOutcome<TResult>> RunAsync(
        Func<CancellationToken, ValueTask<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        CancellationTokenSource cancellation;
        CancellationTokenSource? superseded;
        long version;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            version = ++_version;
            cancellation = new CancellationTokenSource();
            superseded = _currentCancellation;
            _currentCancellation = cancellation;
        }

        CancelRequest(superseded);
        return RunCoreAsync(operation, cancellation, version);
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
            cancellation = _currentCancellation;
            _currentCancellation = null;
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
            cancellation = _currentCancellation;
            _currentCancellation = null;
        }

        CancelRequest(cancellation);
    }

    private async Task<BzsCurrentProviderCallOutcome<TResult>> RunCoreAsync(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationTokenSource cancellation,
        long version)
    {
        try
        {
            var value = await operation(cancellation.Token);
            return IsCurrent(version, cancellation)
                ? new BzsCurrentProviderCallOutcome<TResult>.Succeeded(value)
                : new BzsCurrentProviderCallOutcome<TResult>.Superseded();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested || !IsCurrent(version, cancellation))
        {
            return new BzsCurrentProviderCallOutcome<TResult>.Superseded();
        }
        catch (Exception exception)
        {
            return IsCurrent(version, cancellation)
                ? new BzsCurrentProviderCallOutcome<TResult>.Failed(exception)
                : new BzsCurrentProviderCallOutcome<TResult>.Superseded();
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_currentCancellation, cancellation))
                {
                    _currentCancellation = null;
                }
            }

            cancellation.Dispose();
        }
    }

    private bool IsCurrent(long version, CancellationTokenSource cancellation)
    {
        lock (_gate)
        {
            return !_disposed
                && version == _version
                && ReferenceEquals(_currentCancellation, cancellation)
                && !cancellation.IsCancellationRequested;
        }
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
            // The operation completed and disposed its source after it was captured.
        }
    }
}

internal abstract record BzsCurrentProviderCallOutcome<TResult>
{
    internal sealed record Succeeded(TResult Value) : BzsCurrentProviderCallOutcome<TResult>;

    internal sealed record Failed(Exception Error) : BzsCurrentProviderCallOutcome<TResult>;

    internal sealed record Superseded : BzsCurrentProviderCallOutcome<TResult>;
}
