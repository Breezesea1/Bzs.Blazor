using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class DemoReleaseAnnouncement : ComponentBase, IAsyncDisposable
{
    internal const string StorageKey = "bzs.demo.announcements.read.v1";

    private readonly HashSet<string> _acknowledgedIds = new(StringComparer.Ordinal);
    private DemoReleaseAnnouncementInterop? _interop;
    private bool _dialogOpen;
    private bool _disposed;
    private bool _isInteractive;
    private bool _readStateLoaded;
    private int _unreadCount;

    [Inject]
    private DemoDestinationLinks Links { get; set; } = default!;

    private DemoReleaseEntry Latest => DemoReleaseCatalog.Latest;

    private bool IsChinese => DemoCulture.IsChinese(new Uri(Navigation.Uri));

    private string WhatsNewLabel => DemoText.Chrome.WhatsNew;

    private string DialogTitle => DemoText.Chrome.ReleaseDialogTitle(Latest.Version);

    private string ViewAllReleasesLabel => DemoText.Chrome.ViewAllReleases;

    private string MarkAsReadLabel => DemoText.Chrome.MarkAsRead;

    private string PublishedDate => Latest.PublishedAt.ToString(
        "D",
        DemoCulture.Resolve(IsChinese ? "zh-Hans" : "en-US"));

    private string TriggerAccessibleName => DemoText.Chrome.ReleaseAnnouncementTriggerAccessibleName(_unreadCount);

    private string UnreadBadgeAccessibleName => DemoText.Chrome.UnreadReleaseAnnouncement(_unreadCount);

    private string ReleasesUrl => Links.To(DemoCatalogDestinations.Releases);

    private string LatestReleaseUrl => Links.To(
        DemoCatalogDestinations.Releases,
        fragment: Latest.Id);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _disposed)
        {
            return;
        }

        _isInteractive = true;
        _interop = new DemoReleaseAnnouncementInterop(JS);
        try
        {
            var acknowledgedIds = await _interop.ReadAcknowledgedIdsAsync(StorageKey);
            _acknowledgedIds.UnionWith(acknowledgedIds);
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }

        if (_disposed)
        {
            return;
        }

        _readStateLoaded = true;
        UpdateUnreadCount();
        await InvokeAsync(StateHasChanged);
    }

    private Task OpenDialogAsync(MouseEventArgs _)
    {
        _dialogOpen = true;
        return Task.CompletedTask;
    }

    private Task SetDialogOpenAsync(bool open)
    {
        _dialogOpen = open;
        return Task.CompletedTask;
    }

    private Task HandleDismissedAsync(BzsDialogDismissReason _)
    {
        _dialogOpen = false;
        return Task.CompletedTask;
    }

    private Task CloseDialogForNavigationAsync(MouseEventArgs _)
    {
        _dialogOpen = false;
        return Task.CompletedTask;
    }

    private async Task AcknowledgeLatestAsync(MouseEventArgs _)
    {
        string[] announcementIds = [Latest.Id];
        _acknowledgedIds.UnionWith(announcementIds);
        UpdateUnreadCount();
        _dialogOpen = false;

        if (_interop is null)
        {
            return;
        }

        // Acknowledgement remains session-local when browser storage is unavailable.
        try
        {
            await _interop.AcknowledgeAsync(StorageKey, announcementIds);
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    internal static int GetLatestUnreadCount(
        string latestId,
        IReadOnlySet<string> acknowledgedIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(latestId);
        ArgumentNullException.ThrowIfNull(acknowledgedIds);
        return acknowledgedIds.Contains(latestId) ? 0 : 1;
    }

    private void UpdateUnreadCount() =>
        _unreadCount = GetLatestUnreadCount(Latest.Id, _acknowledgedIds);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_interop is not null)
        {
            await _interop.DisposeAsync();
        }
    }
}
