namespace Bzs.Blazor;

internal sealed class BzsDataGridProviderAdapter<TItem> : IDisposable
{
    private readonly IBzsDataGridProvider<TItem> _provider;
    private readonly BzsCurrentProviderCall<BzsDataGridResult<TItem>> _call = new();

    internal BzsDataGridProviderAdapter(IBzsDataGridProvider<TItem> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    internal Task<BzsCurrentProviderCallOutcome<BzsDataGridResult<TItem>>> LoadAsync(
        BzsDataGridRequest request) =>
        _call.RunAsync(async cancellationToken =>
        {
            var result = await _provider.GetItemsAsync(request, cancellationToken);
            return result
                ?? throw new InvalidOperationException("The DataGrid provider returned a null result.");
        });

    internal void Cancel() => _call.Cancel();

    public void Dispose() => _call.Dispose();
}
