namespace Bzs.Blazor;

internal sealed class BzsAutocompleteRequestCoordinator<TValue> : IDisposable
{
    private readonly IBzsAutocompleteProvider<TValue> _provider;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly object _gate = new();
    private CancellationTokenSource? _requestCancellation;
    private long _version;
    private bool _disposed;

    internal BzsAutocompleteRequestCoordinator(
        IBzsAutocompleteProvider<TValue> provider,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        _provider = provider;
        _delayAsync = delayAsync ?? Task.Delay;
    }

    internal async Task<BzsAutocompleteRequestResult<TValue>> QueryAsync(
        string query,
        TimeSpan debounceDelay,
        bool bypassDebounce = false)
    {
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

        superseded?.Cancel();
        superseded?.Dispose();

        try
        {
            if (!bypassDebounce && debounceDelay > TimeSpan.Zero)
            {
                await _delayAsync(debounceDelay, cancellation.Token);
            }

            var suggestions = await _provider.GetSuggestionsAsync(query, cancellation.Token);
            if (suggestions is null)
            {
                throw new InvalidOperationException("The autocomplete provider returned a null suggestion collection.");
            }

            return IsCurrent(version, cancellation)
                ? BzsAutocompleteRequestResult<TValue>.Succeeded(suggestions)
                : BzsAutocompleteRequestResult<TValue>.Superseded();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return BzsAutocompleteRequestResult<TValue>.Superseded();
        }
        catch (Exception exception)
        {
            return IsCurrent(version, cancellation)
                ? BzsAutocompleteRequestResult<TValue>.Failed(exception)
                : BzsAutocompleteRequestResult<TValue>.Superseded();
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

        cancellation?.Cancel();
        cancellation?.Dispose();
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

        cancellation?.Cancel();
        cancellation?.Dispose();
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

internal sealed record BzsAutocompleteRequestResult<TValue>(
    bool IsCurrent,
    IReadOnlyList<BzsAutocompleteOption<TValue>> Suggestions,
    Exception? Error)
{
    internal static BzsAutocompleteRequestResult<TValue> Succeeded(
        IReadOnlyList<BzsAutocompleteOption<TValue>> suggestions) =>
        new(true, suggestions, null);

    internal static BzsAutocompleteRequestResult<TValue> Failed(Exception error) =>
        new(true, [], error);

    internal static BzsAutocompleteRequestResult<TValue> Superseded() =>
        new(false, [], null);
}
