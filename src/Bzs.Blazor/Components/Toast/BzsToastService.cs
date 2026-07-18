namespace Bzs.Blazor;

/// <summary>Maintains toast state within one Blazor service scope.</summary>
public sealed class BzsToastService : IBzsToastService, IDisposable
{
    private readonly object _gate = new();
    private readonly List<ToastEntry> _entries = [];
    private readonly BzsToastServiceOptions _options;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    /// <summary>Initializes a scoped toast service.</summary>
    public BzsToastService(
        BzsToastServiceOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _options = options ?? new BzsToastServiceOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
        ValidateServiceOptions(_options);
    }

    /// <inheritdoc />
    public event EventHandler<BzsToastChangedEventArgs>? Changed;

    /// <inheritdoc />
    public event EventHandler<BzsToastDismissedEventArgs>? ToastDismissed;

    /// <inheritdoc />
    public IReadOnlyList<BzsToastSnapshot> Snapshot
    {
        get
        {
            lock (_gate)
            {
                return Array.AsReadOnly(_entries.Select(static entry => entry.Snapshot).ToArray());
            }
        }
    }

    /// <inheritdoc />
    public BzsToastId Show(BzsToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateToastOptions(options);

        List<BzsToastDismissedEventArgs> dismissals = [];
        BzsToastChangedEventArgs changed;
        BzsToastId id;
        lock (_gate)
        {
            ThrowIfDisposed();
            RemoveReplacements(options, dismissals);
            while (_entries.Count >= _options.MaximumVisibleToasts)
            {
                RemoveEntry(_entries[0], BzsToastDismissReason.Overflow, dismissals);
            }

            id = new BzsToastId(Guid.NewGuid());
            var duration = ResolveDuration(options);
            var snapshot = new BzsToastSnapshot(
                id,
                options.Severity,
                options.Message.Trim(),
                Normalize(options.Title),
                Normalize(options.Group),
                Normalize(options.DuplicateKey),
                duration,
                options.Dismissible,
                Normalize(options.AccessibleName));
            var entry = new ToastEntry(snapshot, duration);
            _entries.Add(entry);
            StartTimer(entry);
            changed = CreateChangedArgs();
        }

        PublishDismissals(dismissals);
        PublishChanged(changed);
        return id;
    }

    /// <inheritdoc />
    public bool Dismiss(BzsToastId id, BzsToastDismissReason reason = BzsToastDismissReason.Manual)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "The toast dismissal reason is not supported.");
        }

        BzsToastDismissedEventArgs? dismissal = null;
        BzsToastChangedEventArgs? changed = null;
        lock (_gate)
        {
            ThrowIfDisposed();
            var entry = _entries.FirstOrDefault(candidate => candidate.Snapshot.Id == id);
            if (entry is null)
            {
                return false;
            }

            List<BzsToastDismissedEventArgs> dismissals = [];
            RemoveEntry(entry, reason, dismissals);
            dismissal = dismissals[0];
            changed = CreateChangedArgs();
        }

        PublishDismissed(dismissal);
        PublishChanged(changed);
        return true;
    }

    /// <inheritdoc />
    public bool Pause(BzsToastId id, BzsToastPauseReason reason)
    {
        ValidatePauseReason(reason);
        lock (_gate)
        {
            ThrowIfDisposed();
            var entry = _entries.FirstOrDefault(candidate => candidate.Snapshot.Id == id);
            if (entry is null || !entry.PauseReasons.Add(reason))
            {
                return false;
            }

            if (entry.PauseReasons.Count == 1 && entry.Remaining is not null)
            {
                entry.Remaining = Max(
                    TimeSpan.Zero,
                    entry.Remaining.Value - _timeProvider.GetElapsedTime(entry.StartTimestamp));
                entry.Timer?.Dispose();
                entry.Timer = null;
                entry.TimerVersion++;
            }

            return true;
        }
    }

    /// <inheritdoc />
    public bool Resume(BzsToastId id, BzsToastPauseReason reason)
    {
        ValidatePauseReason(reason);
        BzsToastDismissedEventArgs? dismissal = null;
        BzsToastChangedEventArgs? changed = null;
        lock (_gate)
        {
            ThrowIfDisposed();
            var entry = _entries.FirstOrDefault(candidate => candidate.Snapshot.Id == id);
            if (entry is null || !entry.PauseReasons.Remove(reason))
            {
                return false;
            }

            if (entry.PauseReasons.Count == 0)
            {
                if (entry.Remaining <= TimeSpan.Zero)
                {
                    List<BzsToastDismissedEventArgs> dismissals = [];
                    RemoveEntry(entry, BzsToastDismissReason.Automatic, dismissals);
                    dismissal = dismissals[0];
                    changed = CreateChangedArgs();
                }
                else
                {
                    StartTimer(entry);
                }
            }
        }

        if (dismissal is not null)
        {
            PublishDismissed(dismissal);
            PublishChanged(changed!);
        }

        return true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        List<BzsToastDismissedEventArgs> dismissals = [];
        BzsToastChangedEventArgs? changed = null;
        lock (_gate)
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            foreach (var entry in _entries.ToArray())
            {
                RemoveEntry(entry, BzsToastDismissReason.ServiceDisposed, dismissals);
            }
            changed = CreateChangedArgs();
        }

        PublishDismissals(dismissals);
        PublishChanged(changed);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

    private void StartTimer(ToastEntry entry)
    {
        entry.Timer?.Dispose();
        entry.Timer = null;
        entry.TimerVersion++;
        if (entry.Remaining is null || entry.PauseReasons.Count != 0)
        {
            return;
        }

        entry.StartTimestamp = _timeProvider.GetTimestamp();
        var dueTime = entry.Remaining <= TimeSpan.Zero ? TimeSpan.Zero : entry.Remaining.Value;
        var timerVersion = entry.TimerVersion;
        entry.Timer = _timeProvider.CreateTimer(
            static state =>
            {
                var (service, id, version) = ((BzsToastService, BzsToastId, long))state!;
                service.DismissFromTimer(id, version);
            },
            (this, entry.Snapshot.Id, timerVersion),
            dueTime,
            Timeout.InfiniteTimeSpan);
    }

    private void DismissFromTimer(BzsToastId id, long timerVersion)
    {
        BzsToastDismissedEventArgs? dismissal = null;
        BzsToastChangedEventArgs? changed = null;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            var entry = _entries.FirstOrDefault(candidate => candidate.Snapshot.Id == id);
            if (entry is null || entry.TimerVersion != timerVersion || entry.PauseReasons.Count != 0)
            {
                return;
            }

            List<BzsToastDismissedEventArgs> dismissals = [];
            RemoveEntry(entry, BzsToastDismissReason.Automatic, dismissals);
            dismissal = dismissals[0];
            changed = CreateChangedArgs();
        }

        PublishDismissed(dismissal);
        PublishChanged(changed);
    }

    private void RemoveReplacements(
        BzsToastOptions options,
        List<BzsToastDismissedEventArgs> dismissals)
    {
        var group = Normalize(options.Group);
        var duplicateKey = Normalize(options.DuplicateKey);
        foreach (var entry in _entries.ToArray())
        {
            if (group is not null && string.Equals(entry.Snapshot.Group, group, StringComparison.Ordinal)
                || duplicateKey is not null
                    && string.Equals(entry.Snapshot.DuplicateKey, duplicateKey, StringComparison.Ordinal))
            {
                RemoveEntry(entry, BzsToastDismissReason.Replaced, dismissals);
            }
        }
    }

    private void RemoveEntry(
        ToastEntry entry,
        BzsToastDismissReason reason,
        List<BzsToastDismissedEventArgs> dismissals)
    {
        if (!_entries.Remove(entry))
        {
            return;
        }

        entry.Timer?.Dispose();
        entry.Timer = null;
        entry.TimerVersion++;
        dismissals.Add(new BzsToastDismissedEventArgs(entry.Snapshot, reason));
    }

    private BzsToastChangedEventArgs CreateChangedArgs() =>
        new(_entries.Select(static entry => entry.Snapshot).ToArray());

    private void PublishDismissals(IEnumerable<BzsToastDismissedEventArgs> dismissals)
    {
        foreach (var dismissal in dismissals)
        {
            PublishDismissed(dismissal);
        }
    }

    private void PublishChanged(BzsToastChangedEventArgs changed) =>
        InvokeSafely(Changed, changed);

    private void PublishDismissed(BzsToastDismissedEventArgs dismissal) =>
        InvokeSafely(ToastDismissed, dismissal);

    private void InvokeSafely<TEventArgs>(EventHandler<TEventArgs>? handlers, TEventArgs args)
        where TEventArgs : EventArgs
    {
        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler<TEventArgs> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(this, args);
            }
            catch
            {
                // One consumer must not prevent host state or timer cleanup notifications.
            }
        }
    }

    private TimeSpan? ResolveDuration(BzsToastOptions options)
    {
        var duration = options.Duration ?? options.Severity switch
        {
            BzsToastSeverity.Information => _options.InformationDuration,
            BzsToastSeverity.Success => _options.SuccessDuration,
            BzsToastSeverity.Warning => _options.WarningDuration,
            BzsToastSeverity.Error => _options.ErrorDuration,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Severity, "The toast severity is not supported."),
        };

        return duration == Timeout.InfiniteTimeSpan ? null : duration;
    }

    private static void ValidateServiceOptions(BzsToastServiceOptions options)
    {
        if (options.MaximumVisibleToasts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaximumVisibleToasts must be greater than zero.");
        }

        ValidateDefaultDuration(options.InformationDuration, nameof(options.InformationDuration));
        ValidateDefaultDuration(options.SuccessDuration, nameof(options.SuccessDuration));
        ValidateDefaultDuration(options.WarningDuration, nameof(options.WarningDuration));
        ValidateDefaultDuration(options.ErrorDuration, nameof(options.ErrorDuration));
    }

    private static void ValidateDefaultDuration(TimeSpan duration, string name)
    {
        if (duration <= TimeSpan.Zero && duration != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(name, "Toast durations must be positive or infinite.");
        }
    }

    private static void ValidateToastOptions(BzsToastOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Message))
        {
            throw new ArgumentException("A toast message is required.", nameof(options));
        }

        if (!Enum.IsDefined(options.Severity))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Severity, "The toast severity is not supported.");
        }

        if (options.Duration is TimeSpan duration
            && duration <= TimeSpan.Zero
            && duration != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(options), duration, "Toast duration must be positive or infinite.");
        }
    }

    private static void ValidatePauseReason(BzsToastPauseReason reason)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "The toast pause reason is not supported.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left >= right ? left : right;

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private sealed class ToastEntry(BzsToastSnapshot snapshot, TimeSpan? duration)
    {
        public BzsToastSnapshot Snapshot { get; } = snapshot;
        public HashSet<BzsToastPauseReason> PauseReasons { get; } = [];
        public TimeSpan? Remaining { get; set; } = duration;
        public long StartTimestamp { get; set; }
        public ITimer? Timer { get; set; }
        public long TimerVersion { get; set; }
    }
}
