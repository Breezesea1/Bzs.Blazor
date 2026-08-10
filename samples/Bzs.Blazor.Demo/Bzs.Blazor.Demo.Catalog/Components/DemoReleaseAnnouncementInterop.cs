using Microsoft.JSInterop;

namespace Bzs.Blazor.Demo.Client.Components;

internal sealed class DemoReleaseAnnouncementInterop(IJSRuntime js) : IAsyncDisposable
{
    internal const string ModulePath =
        "./_content/Bzs.Blazor.Demo.Catalog/Components/DemoReleaseAnnouncement.razor.js";
    internal const string ReadMethod = "readAcknowledgedIds";
    internal const string AcknowledgeMethod = "acknowledge";

    private Task<IJSObjectReference>? _moduleTask;
    private bool _disposed;

    public async ValueTask<IReadOnlyList<string>> ReadAcknowledgedIdsAsync(string storageKey)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<string[]>(ReadMethod, storageKey);
    }

    public async ValueTask AcknowledgeAsync(
        string storageKey,
        IReadOnlyList<string> announcementIds)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(AcknowledgeMethod, storageKey, announcementIds);
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _moduleTask ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        var module = await _moduleTask;
        ObjectDisposedException.ThrowIf(_disposed, this);
        return module;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_moduleTask is null)
        {
            return;
        }

        try
        {
            var module = await _moduleTask;
            await module.DisposeAsync();
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
