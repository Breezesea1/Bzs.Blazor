namespace Bzs.Blazor;

internal sealed class BzsAutocompleteProviderAdapter<TValue> : IDisposable
{
    private readonly IBzsAutocompleteProvider<TValue> _provider;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly BzsCurrentProviderCall<IReadOnlyList<BzsAutocompleteOption<TValue>>> _call = new();

    internal BzsAutocompleteProviderAdapter(
        IBzsAutocompleteProvider<TValue> provider,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
        _delayAsync = delayAsync ?? Task.Delay;
    }

    internal Task<BzsCurrentProviderCallOutcome<IReadOnlyList<BzsAutocompleteOption<TValue>>>> QueryAsync(
        string query,
        TimeSpan debounceDelay,
        bool bypassDebounce = false) =>
        _call.RunAsync(token => ExecuteAsync(query, debounceDelay, bypassDebounce, token));

    internal void Cancel() => _call.Cancel();

    public void Dispose() => _call.Dispose();

    private async ValueTask<IReadOnlyList<BzsAutocompleteOption<TValue>>> ExecuteAsync(
        string query,
        TimeSpan debounceDelay,
        bool bypassDebounce,
        CancellationToken cancellationToken)
    {
        if (!bypassDebounce && debounceDelay > TimeSpan.Zero)
        {
            await _delayAsync(debounceDelay, cancellationToken);
        }

        var suggestions = await _provider.GetSuggestionsAsync(query, cancellationToken);
        return suggestions
            ?? throw new InvalidOperationException("The autocomplete provider returned a null suggestion collection.");
    }
}
