using System.Collections.Concurrent;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Bzs.Blazor.Tests;

public sealed class FeedbackAndToastTests
{
    [Fact]
    public void MessageRendersSemanticSeverityAndComposableContent()
    {
        using var context = CreateContext();

        var cut = context.Render<BzsMessage>(parameters => parameters
            .Add(component => component.Severity, BzsMessageSeverity.Error)
            .Add(component => component.Title, "Unable to save")
            .Add(component => component.ChildContent, "Correct the highlighted fields."));

        var message = cut.Find("[role=alert]");
        Assert.Equal("assertive", message.GetAttribute("aria-live"));
        Assert.Contains("Unable to save", message.TextContent, StringComparison.Ordinal);
        Assert.Contains("Correct the highlighted fields.", message.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void DeterminateProgressSupportsANonZeroMinimum()
    {
        using var context = CreateContext();

        var cut = context.Render<BzsProgress>(parameters => parameters
            .Add(component => component.Label, "Uploading files")
            .Add(component => component.Minimum, 20d)
            .Add(component => component.Maximum, 100d)
            .Add(component => component.Value, 40d));

        var progress = cut.Find("[role=progressbar]");
        Assert.Equal("Uploading files", progress.GetAttribute("aria-label"));
        Assert.Equal("20", progress.GetAttribute("aria-valuemin"));
        Assert.Equal("100", progress.GetAttribute("aria-valuemax"));
        Assert.Equal("40", progress.GetAttribute("aria-valuenow"));
        Assert.Equal("20", progress.GetAttribute("value"));
        Assert.Equal("80", progress.GetAttribute("max"));
        Assert.Contains("25%", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgressRejectsInvalidRangesAndValues()
    {
        AssertInvalidProgress(10d, 10d, 10d);
        AssertInvalidProgress(10d, 5d, 10d);
        AssertInvalidProgress(0d, double.PositiveInfinity, 0d);
        AssertInvalidProgress(0d, 100d, double.NaN);
        AssertInvalidProgress(0d, 100d, 101d);
    }

    [Fact]
    public void IndeterminateProgressDoesNotReportANumericValue()
    {
        using var context = CreateContext();

        var cut = context.Render<BzsProgress>(parameters => parameters
            .Add(component => component.Label, "Loading results"));

        var progress = cut.Find("[role=progressbar]");
        Assert.Equal("true", progress.GetAttribute("aria-busy"));
        Assert.Null(progress.GetAttribute("aria-valuenow"));
    }

    [Theory]
    [InlineData("en", "Progress", "Notification", "Dismiss notification")]
    [InlineData("zh-Hans", "进度", "通知", "关闭通知")]
    public void LibraryOwnedFeedbackLabelsFollowTheCurrentUiCulture(
        string cultureName,
        string expectedProgressLabel,
        string expectedToastStatusLabel,
        string expectedDismissLabel)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

        try
        {
            using var context = CreateContext();
            var progress = context.Render<BzsProgress>();
            var toast = context.Render<BzsToast>(parameters => parameters
                .Add(component => component.Toast, CreateToast()));

            Assert.Equal(expectedProgressLabel, progress.Find("[role=progressbar]").GetAttribute("aria-label"));
            Assert.Equal(expectedToastStatusLabel, toast.Find("[role=status]").GetAttribute("aria-label"));
            Assert.Equal(expectedDismissLabel, toast.Find("button").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void ToastUsesSemanticRolesNamesAndConsumerCallbacks()
    {
        using var context = CreateContext();
        var requestedDismissal = default(BzsToastDismissReason?);
        var pauses = new List<BzsToastPauseReason>();
        var resumes = new List<BzsToastPauseReason>();

        var cut = context.Render<BzsToast>(parameters => parameters
            .Add(component => component.Toast, CreateToast())
            .Add(component => component.DismissLabel, "Close notification")
            .Add(component => component.DismissRequested, (BzsToastDismissReason reason) => requestedDismissal = reason)
            .Add(component => component.PauseRequested, (BzsToastPauseReason reason) => pauses.Add(reason))
            .Add(component => component.ResumeRequested, (BzsToastPauseReason reason) => resumes.Add(reason)));

        var root = cut.Find("[role=status]");
        Assert.Equal("polite", root.GetAttribute("aria-live"));
        Assert.Equal("Notification", root.GetAttribute("aria-label"));
        Assert.Equal("Close notification", cut.Find("button").GetAttribute("aria-label"));

        root.MouseEnter();
        root.FocusIn();
        root.FocusOut();
        root.MouseLeave();
        cut.Find("button").Click(new MouseEventArgs());

        Assert.Equal([BzsToastPauseReason.Hover, BzsToastPauseReason.Focus], pauses);
        Assert.Equal([BzsToastPauseReason.Focus, BzsToastPauseReason.Hover], resumes);
        Assert.Equal(BzsToastDismissReason.Manual, requestedDismissal);
    }

    [Fact]
    public void ErrorToastUsesAnAssertiveNamedLiveRegion()
    {
        using var context = CreateContext();

        var cut = context.Render<BzsToast>(parameters => parameters
            .Add(component => component.Toast, CreateToast(
                severity: BzsToastSeverity.Error,
                accessibleName: "Save failure")));

        var root = cut.Find("[role=alert]");
        Assert.Equal("assertive", root.GetAttribute("aria-live"));
        Assert.Equal("Save failure", root.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task ToastServiceUsesDeterministicOverflowDuplicateAndGroupReplacementPolicies()
    {
        var timeProvider = new ManualTimeProvider();
        await using var overflowService = new BzsToastService(
            new BzsToastServiceOptions { MaximumVisibleToasts = 2 },
            timeProvider);
        var overflowDismissals = new List<BzsToastDismissedEventArgs>();
        overflowService.ToastDismissed += (_, args) => overflowDismissals.Add(args);

        var first = overflowService.Show(new BzsToastOptions { Message = "First" });
        var second = overflowService.Show(new BzsToastOptions { Message = "Second" });
        var third = overflowService.Show(new BzsToastOptions { Message = "Third" });

        Assert.Equal([second, third], overflowService.Snapshot.Select(static toast => toast.Id));
        Assert.Contains(overflowDismissals, dismissal =>
            dismissal.Toast.Id == first && dismissal.Reason == BzsToastDismissReason.Overflow);

        await using var replacementService = new BzsToastService(
            new BzsToastServiceOptions { MaximumVisibleToasts = 5 },
            timeProvider);
        var replacementDismissals = new List<BzsToastDismissedEventArgs>();
        replacementService.ToastDismissed += (_, args) => replacementDismissals.Add(args);

        var duplicate = replacementService.Show(new BzsToastOptions
        {
            Message = "First save",
            DuplicateKey = "save",
        });
        var duplicateReplacement = replacementService.Show(new BzsToastOptions
        {
            Message = "Latest save",
            DuplicateKey = "save",
        });
        var grouped = replacementService.Show(new BzsToastOptions
        {
            Message = "Old sync",
            Group = "sync",
        });
        var groupReplacement = replacementService.Show(new BzsToastOptions
        {
            Message = "New sync",
            Group = "sync",
        });

        Assert.DoesNotContain(replacementService.Snapshot, toast => toast.Id == duplicate);
        Assert.DoesNotContain(replacementService.Snapshot, toast => toast.Id == grouped);
        Assert.Contains(replacementService.Snapshot, toast => toast.Id == duplicateReplacement);
        Assert.Contains(replacementService.Snapshot, toast => toast.Id == groupReplacement);
        Assert.Contains(replacementDismissals, dismissal =>
            dismissal.Toast.Id == duplicate && dismissal.Reason == BzsToastDismissReason.Replaced);
        Assert.Contains(replacementDismissals, dismissal =>
            dismissal.Toast.Id == grouped && dismissal.Reason == BzsToastDismissReason.Replaced);
    }

    [Fact]
    public async Task ToastServiceKeepsAutomaticDismissalPausedUntilEveryReasonResumes()
    {
        var timeProvider = new ManualTimeProvider();
        await using var service = new BzsToastService(timeProvider: timeProvider);
        var dismissals = new List<BzsToastDismissedEventArgs>();
        service.ToastDismissed += (_, args) => dismissals.Add(args);

        var id = service.Show(new BzsToastOptions
        {
            Message = "Saving",
            Duration = TimeSpan.FromSeconds(1),
        });

        timeProvider.Advance(TimeSpan.FromMilliseconds(400));
        Assert.True(service.Pause(id, BzsToastPauseReason.Hover));
        Assert.True(service.Pause(id, BzsToastPauseReason.Focus));

        timeProvider.Advance(TimeSpan.FromSeconds(10));
        Assert.True(service.Resume(id, BzsToastPauseReason.Hover));
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        Assert.Single(service.Snapshot);

        Assert.True(service.Resume(id, BzsToastPauseReason.Focus));
        timeProvider.Advance(TimeSpan.FromMilliseconds(599));
        Assert.Single(service.Snapshot);
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        await WaitForAsync(() => service.Snapshot.Count == 0);

        Assert.Contains(dismissals, dismissal =>
            dismissal.Toast.Id == id && dismissal.Reason == BzsToastDismissReason.Automatic);
    }

    [Fact]
    public async Task ToastServiceIgnoresAQueuedTimerWhenPausedAndCompletesOnZeroRemainingResume()
    {
        var timeProvider = new ManualTimeProvider();
        await using var service = new BzsToastService(timeProvider: timeProvider);
        var dismissals = new List<BzsToastDismissedEventArgs>();
        service.ToastDismissed += (_, args) => dismissals.Add(args);

        var id = service.Show(new BzsToastOptions
        {
            Message = "Saving",
            Duration = TimeSpan.FromSeconds(1),
        });

        timeProvider.AdvanceWithoutInvoking(TimeSpan.FromSeconds(1));
        Assert.True(service.Pause(id, BzsToastPauseReason.Hover));
        timeProvider.InvokePendingTimers();

        Assert.Single(service.Snapshot);
        Assert.True(service.Resume(id, BzsToastPauseReason.Hover));
        await WaitForAsync(() => service.Snapshot.Count == 0);

        var dismissal = Assert.Single(dismissals);
        Assert.Equal(id, dismissal.Toast.Id);
        Assert.Equal(BzsToastDismissReason.Automatic, dismissal.Reason);
    }

    [Fact]
    public async Task ToastServiceReportsManualAutomaticAndDisposalDismissalExactlyOnce()
    {
        await using (var manualService = new BzsToastService(timeProvider: new ManualTimeProvider()))
        {
            var dismissals = new List<BzsToastDismissedEventArgs>();
            manualService.ToastDismissed += (_, args) => dismissals.Add(args);
            var id = manualService.Show(new BzsToastOptions { Message = "Manual" });

            Assert.True(manualService.Dismiss(id));
            Assert.False(manualService.Dismiss(id));
            var dismissal = Assert.Single(dismissals);
            Assert.Equal(BzsToastDismissReason.Manual, dismissal.Reason);
        }

        var timeProvider = new ManualTimeProvider();
        await using (var automaticService = new BzsToastService(timeProvider: timeProvider))
        {
            var dismissals = new List<BzsToastDismissedEventArgs>();
            automaticService.ToastDismissed += (_, args) => dismissals.Add(args);
            var id = automaticService.Show(new BzsToastOptions
            {
                Message = "Automatic",
                Duration = TimeSpan.FromSeconds(1),
            });

            timeProvider.Advance(TimeSpan.FromSeconds(1));
            await WaitForAsync(() => automaticService.Snapshot.Count == 0);
            Assert.False(automaticService.Dismiss(id));
            var dismissal = Assert.Single(dismissals);
            Assert.Equal(BzsToastDismissReason.Automatic, dismissal.Reason);
        }

        var disposalService = new BzsToastService(timeProvider: new ManualTimeProvider());
        var disposalDismissals = new List<BzsToastDismissedEventArgs>();
        disposalService.ToastDismissed += (_, args) => disposalDismissals.Add(args);
        disposalService.Show(new BzsToastOptions { Message = "Dispose" });

        await disposalService.DisposeAsync();
        await disposalService.DisposeAsync();

        var disposalDismissal = Assert.Single(disposalDismissals);
        Assert.Equal(BzsToastDismissReason.ServiceDisposed, disposalDismissal.Reason);
    }

    [Fact]
    public async Task ToastServiceContainsSubscriberExceptionsAndNotifiesRemainingSubscribers()
    {
        await using var service = new BzsToastService(timeProvider: new ManualTimeProvider());
        var changedCount = 0;
        var dismissalCount = 0;
        service.Changed += (_, _) => throw new InvalidOperationException("Changed subscriber failed.");
        service.Changed += (_, _) => changedCount++;
        service.ToastDismissed += (_, _) => throw new InvalidOperationException("Dismissal subscriber failed.");
        service.ToastDismissed += (_, _) => dismissalCount++;

        var id = service.Show(new BzsToastOptions { Message = "Published" });

        Assert.True(service.Dismiss(id));
        Assert.Equal(2, changedCount);
        Assert.Equal(1, dismissalCount);
        Assert.Empty(service.Snapshot);
    }

    [Fact]
    public async Task ToastServiceRemovesAConcurrentDismissalOrDisposalOnlyOnce()
    {
        var service = new BzsToastService(timeProvider: new ManualTimeProvider());
        var dismissals = new ConcurrentQueue<BzsToastDismissedEventArgs>();
        service.ToastDismissed += (_, args) => dismissals.Enqueue(args);
        var id = service.Show(new BzsToastOptions
        {
            Message = "Concurrent",
            Duration = Timeout.InfiniteTimeSpan,
        });

        var dismissTask = Task.Run(() =>
        {
            try
            {
                return service.Dismiss(id);
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        });
        var disposeTask = Task.Run(async () => await service.DisposeAsync());

        await Task.WhenAll(dismissTask, disposeTask);

        Assert.Empty(service.Snapshot);
        var dismissal = Assert.Single(dismissals);
        Assert.Equal(id, dismissal.Toast.Id);
        Assert.True(dismissal.Reason is BzsToastDismissReason.Manual
            or BzsToastDismissReason.ServiceDisposed);
    }

    [Fact]
    public async Task ToastSnapshotsAreStableAndImmutable()
    {
        await using var service = new BzsToastService(timeProvider: new ManualTimeProvider());
        BzsToastChangedEventArgs? firstChanged = null;
        service.Changed += (_, args) => firstChanged ??= args;

        service.Show(new BzsToastOptions { Message = "First" });
        var snapshot = service.Snapshot;
        var snapshotList = Assert.IsAssignableFrom<IList<BzsToastSnapshot>>(snapshot);

        Assert.Throws<NotSupportedException>(() => snapshotList[0] = snapshotList[0]);
        service.Show(new BzsToastOptions { Message = "Second" });

        Assert.Single(snapshot);
        Assert.NotNull(firstChanged);
        Assert.Single(firstChanged.Snapshot);
        var changedList = Assert.IsAssignableFrom<IList<BzsToastSnapshot>>(firstChanged.Snapshot);
        Assert.Throws<NotSupportedException>(() => changedList[0] = changedList[0]);
    }

    [Fact]
    public async Task ToastServiceIsScopedAndDisposesItsActiveToasts()
    {
        var timeProvider = new ManualTimeProvider();
        await using var firstScope = new BzsToastService(timeProvider: timeProvider);
        await using var secondScope = new BzsToastService(timeProvider: timeProvider);
        var firstScopeDismissals = new List<BzsToastDismissedEventArgs>();
        firstScope.ToastDismissed += (_, args) => firstScopeDismissals.Add(args);

        var firstId = firstScope.Show(new BzsToastOptions { Message = "First scope" });
        var secondId = secondScope.Show(new BzsToastOptions { Message = "Second scope" });

        Assert.Equal(firstId, Assert.Single(firstScope.Snapshot).Id);
        Assert.Equal(secondId, Assert.Single(secondScope.Snapshot).Id);

        await firstScope.DisposeAsync();

        Assert.Empty(firstScope.Snapshot);
        Assert.Equal(secondId, Assert.Single(secondScope.Snapshot).Id);
        Assert.Contains(firstScopeDismissals, dismissal =>
            dismissal.Toast.Id == firstId && dismissal.Reason == BzsToastDismissReason.ServiceDisposed);
        Assert.Throws<ObjectDisposedException>(() =>
            firstScope.Show(new BzsToastOptions { Message = "Disposed" }));
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        return context;
    }

    private static BzsToastSnapshot CreateToast(
        BzsToastSeverity severity = BzsToastSeverity.Information,
        string? accessibleName = null) =>
        new(
            new BzsToastId(Guid.NewGuid()),
            severity,
            "Changes were saved.",
            "Saved",
            null,
            null,
            TimeSpan.FromSeconds(4),
            true,
            accessibleName);

    private static void AssertInvalidProgress(double minimum, double maximum, double? value)
    {
        using var context = CreateContext();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            context.Render<BzsProgress>(parameters => parameters
                .Add(component => component.Minimum, minimum)
                .Add(component => component.Maximum, maximum)
                .Add(component => component.Value, value)));
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Yield();
        }

        Assert.True(condition());
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly object _gate = new();
        private readonly List<ManualTimer> _timers = [];
        private readonly List<ManualTimer> _pendingTimers = [];
        private DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;
        private long _timestamp;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow()
        {
            lock (_gate)
            {
                return _utcNow;
            }
        }

        public override long GetTimestamp()
        {
            lock (_gate)
            {
                return _timestamp;
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(TimeSpan elapsed) => InvokeTimers(AdvanceCore(elapsed));

        public void AdvanceWithoutInvoking(TimeSpan elapsed)
        {
            var dueTimers = AdvanceCore(elapsed);
            lock (_gate)
            {
                _pendingTimers.AddRange(dueTimers);
            }
        }

        public void InvokePendingTimers()
        {
            List<ManualTimer> pendingTimers;
            lock (_gate)
            {
                pendingTimers = [.. _pendingTimers];
                _pendingTimers.Clear();
            }

            InvokeTimers(pendingTimers);
        }

        private List<ManualTimer> AdvanceCore(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }

            lock (_gate)
            {
                _utcNow += elapsed;
                _timestamp += elapsed.Ticks;
                var dueTimers = _timers
                    .Where(timer => timer.IsDue(_timestamp))
                    .ToList();

                foreach (var timer in dueTimers)
                {
                    timer.MarkFired(_timestamp);
                }

                return dueTimers;
            }
        }

        private static void InvokeTimers(IEnumerable<ManualTimer> timers)
        {
            foreach (var timer in timers)
            {
                timer.Invoke();
            }
        }

        private void Register(ManualTimer timer)
        {
            lock (_gate)
            {
                _timers.Add(timer);
            }
        }

        private void Unregister(ManualTimer timer)
        {
            lock (_gate)
            {
                _timers.Remove(timer);
            }
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider _owner;
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private long? _dueTimestamp;
            private TimeSpan _period;
            private bool _disposed;

            public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state)
            {
                _owner = owner;
                _callback = callback;
                _state = state;
                _owner.Register(this);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (_disposed)
                {
                    return false;
                }

                _period = period;
                _dueTimestamp = dueTime == Timeout.InfiniteTimeSpan
                    ? null
                    : _owner.GetTimestamp() + dueTime.Ticks;
                return true;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.Unregister(this);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public bool IsDue(long timestamp) => !_disposed && _dueTimestamp is long due && due <= timestamp;

            public void MarkFired(long timestamp)
            {
                if (_period == Timeout.InfiniteTimeSpan)
                {
                    _dueTimestamp = null;
                    return;
                }

                _dueTimestamp = timestamp + _period.Ticks;
            }

            public void Invoke() => _callback(_state);
        }
    }
}
